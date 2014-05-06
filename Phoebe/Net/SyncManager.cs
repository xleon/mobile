using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Toggl.Phoebe.Data;
using Toggl.Phoebe.Data.Models;
using XPlatUtils;

namespace Toggl.Phoebe.Net
{
    public class SyncManager
    {
        private static readonly string Tag = "SyncManager";
        #pragma warning disable 0414
        private readonly Subscription<AuthChangedMessage> subscriptionAuthChanged;
        #pragma warning restore 0414
        private Subscription<ModelsCommittedMessage> subscriptionModelsCommited;

        public SyncManager ()
        {
            var bus = ServiceContainer.Resolve<MessageBus> ();
            subscriptionAuthChanged = bus.Subscribe<AuthChangedMessage> (OnAuthChanged);
            subscriptionModelsCommited = bus.Subscribe<ModelsCommittedMessage> (OnModelsCommited);
        }

        private void OnModelsCommited (ModelsCommittedMessage msg)
        {
            Run (SyncMode.Auto);
        }

        private void OnAuthChanged (AuthChangedMessage msg)
        {
            if (msg.AuthManager.IsAuthenticated)
                return;

            // Reset last run on logout
            LastRun = null;
        }

        public async void Run (SyncMode mode = SyncMode.Full)
        {
            if (!ServiceContainer.Resolve<AuthManager> ().IsAuthenticated)
                return;
            if (IsRunning)
                return;

            var bus = ServiceContainer.Resolve<MessageBus> ();
            IsRunning = true;

            // Unsubscribe from models commited messages (our actions trigger them as well,
            // so need to ignore them to prevent infinite recursion.
            if (subscriptionModelsCommited != null) {
                bus.Unsubscribe (subscriptionModelsCommited);
                subscriptionModelsCommited = null;
            }

            try {
                await RunInBackground (mode);
            } finally {
                IsRunning = false;
                subscriptionModelsCommited = bus.Subscribe<ModelsCommittedMessage> (OnModelsCommited);
            }
        }

        private async Task RunInBackground (SyncMode mode)
        {
            var bus = ServiceContainer.Resolve<MessageBus> ();
            var client = ServiceContainer.Resolve<ITogglClient> ();
            var log = ServiceContainer.Resolve<Logger> ();
            var modelStore = ServiceContainer.Resolve<IModelStore> ();

            // Resolve automatic sync mode to actual mode
            if (mode == SyncMode.Auto) {
                if (LastRun != null && LastRun > Time.UtcNow - TimeSpan.FromHours (1)) {
                    mode = SyncMode.Push;
                } else {
                    mode = SyncMode.Full;
                }
            }

            bus.Send (new SyncStartedMessage (this, mode));

            bool hasErrors = false;
            Exception ex = null;
            try {
                if (mode == SyncMode.Full) {
                    // TODO: Purge data which isn't related to us

                    // Purge excess time entries. Do it 200 items at a time, to avoid allocating too much memory to the
                    // models to be deleted. If there are more than 200 entries, they will be removed in the next purge.
                    var q = Model.Query<TimeEntryModel> (
                                (te) => (te.IsDirty != true && te.RemoteId != null)
                                || (te.RemoteId == null && te.DeletedAt != null))
                        .OrderBy ((te) => te.StartTime, false).Skip (1000).Take (200);
                    foreach (var entry in q) {
                        entry.IsPersisted = false;
                    }
                }

                if (mode.HasFlag (SyncMode.Pull)) {
                    var changes = await client.GetChanges (LastRun)
                        .ConfigureAwait (continueOnCapturedContext: false);

                    changes.User.IsPersisted = true;
                    foreach (var m in changes.Workspaces) {
                        if (m.RemoteDeletedAt == null) {
                            m.IsPersisted = true;
                            m.Users.Add (changes.User);
                        }
                    }
                    foreach (var m in changes.Tags) {
                        if (m.RemoteDeletedAt == null) {
                            m.IsPersisted = true;
                        }
                    }
                    foreach (var m in changes.Clients) {
                        if (m.RemoteDeletedAt == null) {
                            m.IsPersisted = true;
                        }
                    }
                    foreach (var m in changes.Projects) {
                        if (m.RemoteDeletedAt == null) {
                            m.IsPersisted = true;
                            m.Users.Add (changes.User);
                        }
                    }
                    foreach (var m in changes.Tasks) {
                        if (m.RemoteDeletedAt == null) {
                            m.IsPersisted = true;
                        }
                    }
                    foreach (var m in changes.TimeEntries) {
                        if (m.RemoteDeletedAt == null) {
                            m.IsPersisted = true;
                        }
                    }

                    if (modelStore.TryCommit ()) {
                        // Update LastRun incase the data was persisted successfully
                        LastRun = changes.Timestamp;
                    }
                }

                if (mode.HasFlag (SyncMode.Push)) {
                    // Construct dependency graph:
                    var graph = ModelGraph.FromDirty (Enumerable.Empty<Model> ()
                        .Union (QueryDirtyModels<WorkspaceModel> ())
                        .Union (QueryDirtyModels<WorkspaceUserModel> ())
                        .Union (QueryDirtyModels<TagModel> ())
                        .Union (QueryDirtyModels<ClientModel> ())
                        .Union (QueryDirtyModels<ProjectModel> ())
                        .Union (QueryDirtyModels<ProjectUserModel> ())
                        .Union (QueryDirtyModels<TaskModel> ())
                        .Union (QueryDirtyModels<TimeEntryModel> ().ForCurrentUser ().Where ((m) => m.State != TimeEntryState.New)));

                    // Start pushing the dependencies from the end nodes up
                    var tasks = new List<Task<Exception>> ();
                    while (true) {
                        tasks.Clear ();

                        var models = graph.EndNodes.ToList ();
                        if (models.Count == 0)
                            break;

                        foreach (var model in models) {
                            tasks.Add (PushModel (model));
                        }

                        await Task.WhenAll (tasks)
                            .ConfigureAwait (continueOnCapturedContext: false);

                        for (var i = 0; i < tasks.Count; i++) {
                            var model = models [i];
                            var error = tasks [i].Result;

                            if (error != null) {
                                if (model.RemoteId == null) {
                                    // When creation fails, remove branch as there are models that depend on this
                                    // one, so there is no point in continuing with the branch.
                                    graph.RemoveBranch (model);
                                } else {
                                    graph.Remove (model);
                                }
                                hasErrors = true;

                                // Log error
                                var id = model.RemoteId.HasValue ? model.RemoteId.ToString () : model.Id.ToString ();
                                if (error is System.Net.Http.HttpRequestException) {
                                    log.Info (Tag, error, "Failed to sync {0}#{1}.", model.GetType ().Name, id);
                                } else {
                                    log.Warning (Tag, error, "Failed to sync {0}#{1}.", model.GetType ().Name, id);
                                }
                            } else {
                                graph.Remove (model);
                            }
                        }
                    }

                    // Attempt to persist changes
                    modelStore.TryCommit ();
                }
            } catch (Exception e) {
                if (e.IsNetworkFailure () || e is TaskCanceledException) {
                    log.Info (Tag, e, "Sync ({0}) failed.", mode);
                } else {
                    log.Warning (Tag, e, "Sync ({0}) failed.", mode);
                }

                hasErrors = true;
                ex = e;
            } finally {
                bus.Send (new SyncFinishedMessage (this, mode, hasErrors, ex));
            }
        }

        private static IModelQuery<T> QueryDirtyModels<T> ()
            where T : Model, new()
        {
            // Workaround to exclude intermediate models which we've created from assumptions (for current user
            // and without remote id) from returned models.
            if (typeof(T) == typeof(WorkspaceUserModel)) {
                var userId = ServiceContainer.Resolve<AuthManager> ().UserId;
                return (IModelQuery<T>)Model.Query<WorkspaceUserModel> (
                    (m) => (m.ToId != userId || m.RemoteId != null)
                    && (m.IsDirty || m.RemoteId == null || m.DeletedAt != null));
            } else if (typeof(T) == typeof(ProjectUserModel)) {
                var userId = ServiceContainer.Resolve<AuthManager> ().UserId;
                return (IModelQuery<T>)Model.Query<ProjectUserModel> (
                    (m) => (m.ToId != userId || m.RemoteId != null)
                    && (m.IsDirty || m.RemoteId == null || m.DeletedAt != null));
            }

            return Model.Query<T> ((m) => m.IsDirty || m.RemoteId == null || m.DeletedAt != null);
        }

        private async Task<Exception> PushModel (Model model)
        {
            var client = ServiceContainer.Resolve<ITogglClient> ();
            try {
                if (model.DeletedAt != null) {
                    if (model.RemoteId != null) {
                        // Delete model
                        await client.Delete (model)
                            .ConfigureAwait (continueOnCapturedContext: false);
                        model.IsPersisted = false;
                    } else {
                        // Some weird combination where the DeletedAt exists and remote Id doesn't:
                        model.IsPersisted = false;
                    }
                } else if (model.RemoteId != null) {
                    await client.Update (model)
                        .ConfigureAwait (continueOnCapturedContext: false);
                } else {
                    await client.Create (model)
                        .ConfigureAwait (continueOnCapturedContext: false);
                }
            } catch (System.Net.Http.HttpRequestException ex) {
                return ex;
            }

            return null;
        }

        public bool IsRunning { get; private set; }

        private DateTime? LastRun {
            get { return ServiceContainer.Resolve<ISettingsStore> ().SyncLastRun; }
            set { ServiceContainer.Resolve<ISettingsStore> ().SyncLastRun = value; }
        }
    }
}
