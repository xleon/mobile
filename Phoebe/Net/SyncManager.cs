using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Toggl.Phoebe.Data;
using Toggl.Phoebe.Data.DataObjects;
using Toggl.Phoebe.Data.Json.Converters;
using XPlatUtils;

namespace Toggl.Phoebe.Net
{
    public class SyncManager : ISyncManager
    {
        private static readonly string Tag = "SyncManager";
        #pragma warning disable 0414
        private readonly Subscription<AuthChangedMessage> subscriptionAuthChanged;
        #pragma warning restore 0414
        private Subscription<DataChangeMessage> subscriptionDataChange;

        public SyncManager ()
        {
            var bus = ServiceContainer.Resolve<MessageBus> ();
            subscriptionAuthChanged = bus.Subscribe<AuthChangedMessage> (OnAuthChanged);
            subscriptionDataChange = bus.Subscribe<DataChangeMessage> (OnDataChange);
        }

        private void OnDataChange (DataChangeMessage msg)
        {
            var dataObject = msg.Data as CommonData;
            if (dataObject != null) {
                if (!dataObject.IsDirty || dataObject.RemoteRejected) {
                    return;
                }
            }

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

            var network = ServiceContainer.Resolve<INetworkPresence> ();

            if (!network.IsNetworkPresent) {
                network.RegisterSyncWhenNetworkPresent ();
                return;
            } else {
                network.UnregisterSyncWhenNetworkPresent ();
            }

            var bus = ServiceContainer.Resolve<MessageBus> ();
            IsRunning = true;

            // Unsubscribe from models commited messages (our actions trigger them as well,
            // so need to ignore them to prevent infinite recursion.
            if (subscriptionDataChange != null) {
                bus.Unsubscribe (subscriptionDataChange);
                subscriptionDataChange = null;
            }

            try {
                // Make sure that the RunInBackground is actually started on a background thread
                LastRun = await await Task.Factory.StartNew (() => RunInBackground (mode, LastRun));
            } finally {
                IsRunning = false;
                subscriptionDataChange = bus.Subscribe<DataChangeMessage> (OnDataChange);
            }
        }

        private async Task<DateTime?> RunInBackground (SyncMode mode, DateTime? lastRun)
        {
            var bus = ServiceContainer.Resolve<MessageBus> ();
            var log = ServiceContainer.Resolve<Logger> ();

            // Resolve automatic sync mode to actual mode
            if (mode == SyncMode.Auto) {
                if (lastRun != null && lastRun > Time.UtcNow - TimeSpan.FromMinutes (5)) {
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
                    await CollectGarbage ().ConfigureAwait (false);
                }

                if (mode.HasFlag (SyncMode.Pull)) {
                    lastRun = await PullChanges (lastRun).ConfigureAwait (false);
                }

                if (mode.HasFlag (SyncMode.Push)) {
                    hasErrors = await PushChanges ().ConfigureAwait (false);
                }
            } catch (Exception e) {
                if (e.IsNetworkFailure () || e is TaskCanceledException) {
                    log.Info (Tag, e, "Sync ({0}) failed.", mode);
                    if (e.IsNetworkFailure ())
                        ServiceContainer.Resolve<INetworkPresence> ().RegisterSyncWhenNetworkPresent ();
                } else {
                    log.Warning (Tag, e, "Sync ({0}) failed.", mode);
                }

                hasErrors = true;
                ex = e;
            } finally {
                bus.Send (new SyncFinishedMessage (this, mode, hasErrors, ex));
            }

            return lastRun;
        }

        private static async Task CollectGarbage ()
        {
            var store = ServiceContainer.Resolve<IDataStore> ();

            // TODO: Purge data which isn't related to us

            // Purge excess time entries. Do it 200 items at a time, to avoid allocating too much memory to the
            // models to be deleted. If there are more than 200 entries, they will be removed in the next purge.
            var timeEntryRows = await store.Table<TimeEntryData> ()
                .Skip (1000).Take (200)
                .OrderBy (r => r.StartTime, false)
                .QueryAsync (r => (r.IsDirty != true && r.RemoteId != null)
                                || (r.RemoteId == null && r.DeletedAt != null))
                .ConfigureAwait (false);

            await Task.WhenAll (timeEntryRows.Select (store.DeleteAsync)).ConfigureAwait (false);
        }

        private static async Task<DateTime> PullChanges (DateTime? lastRun)
        {
            var client = ServiceContainer.Resolve<ITogglClient> ();
            var store = ServiceContainer.Resolve<IDataStore> ();
            var changes = await client.GetChanges (lastRun).ConfigureAwait (false);

            // Import data (in parallel batches)
            var userData = await changes.User.Import ();
            await Task.WhenAll (changes.Workspaces.Select (async (json) => {
                var workspaceData = await json.Import ().ConfigureAwait (false);

                // Make sure that the user relation exists and is up to date
                if (workspaceData != null) {
                    var workspaceUserRows = await store.Table<WorkspaceUserData> ()
                        .QueryAsync (r => r.WorkspaceId == workspaceData.Id && r.UserId == userData.Id && r.DeletedAt == null)
                        .ConfigureAwait (false);

                    var workspaceUserData = workspaceUserRows.FirstOrDefault ();
                    if (workspaceUserData == null) {
                        workspaceUserData = new WorkspaceUserData () {
                            WorkspaceId = workspaceData.Id,
                            UserId = userData.Id,
                            IsAdmin = json.IsAdmin,
                        };
                    } else {
                        workspaceUserData.IsAdmin = json.IsAdmin;
                    }

                    await store.PutAsync (workspaceUserData).ConfigureAwait (false);
                }
            }));
            await Task.WhenAll (changes.Tags.Select (json => json.Import ()));
            await Task.WhenAll (changes.Clients.Select (json => json.Import ()));
            await Task.WhenAll (changes.Projects.Select (async (json) => {
                var projectData = await json.Import ().ConfigureAwait (false);

                // Make sure that the user relation exists
                if (projectData != null) {
                    var projectUserRows = await store.Table<ProjectUserData> ()
                        .QueryAsync (r => r.ProjectId == projectData.Id && r.UserId == userData.Id && r.DeletedAt == null)
                        .ConfigureAwait (false);

                    var projectUserData = projectUserRows.FirstOrDefault ();
                    if (projectUserData == null) {
                        projectUserData = new ProjectUserData () {
                            ProjectId = projectData.Id,
                            UserId = userData.Id,
                        };

                        await store.PutAsync (projectUserData).ConfigureAwait (false);
                    }
                }
            }));
            await Task.WhenAll (changes.Tasks.Select (json => json.Import ()));
            await Task.WhenAll (changes.TimeEntries.Select (json => json.Import ()));

            return changes.Timestamp;
        }

        private static async Task<bool> PushChanges ()
        {
            var log = ServiceContainer.Resolve<Logger> ();
            var hasErrors = false;

            // Construct dependency graph:
            var allDirtyData = await GetAllDirtyData ().ConfigureAwait (false);
            var graph = await RelatedDataGraph.FromDirty (allDirtyData).ConfigureAwait (false);

            // Start pushing the dependencies from the end nodes up
            var tasks = new List<Task<Exception>> ();
            while (true) {
                tasks.Clear ();

                var dirtyDataObjects = graph.EndNodes.ToList ();
                if (dirtyDataObjects.Count == 0)
                    break;

                foreach (var dataObject in dirtyDataObjects) {
                    if (dataObject.RemoteRejected) {
                        if (dataObject.RemoteId == null) {
                            // Creation has failed, so remove the whole branch.
                            graph.RemoveBranch (dataObject);
                        } else {
                            graph.Remove (dataObject);
                        }
                    } else {
                        tasks.Add (PushDataObject (dataObject));
                    }
                }

                // Nothing was pushed this round
                if (tasks.Count < 1)
                    continue;

                await Task.WhenAll (tasks).ConfigureAwait (false);

                for (var i = 0; i < tasks.Count; i++) {
                    var dataObject = dirtyDataObjects [i];
                    var error = tasks [i].Result;

                    if (error != null) {
                        if (dataObject.RemoteId == null) {
                            // When creation fails, remove branch as there are models that depend on this
                            // one, so there is no point in continuing with the branch.
                            graph.RemoveBranch (dataObject);
                        } else {
                            graph.Remove (dataObject);
                        }
                        hasErrors = true;

                        // Log error
                        var id = dataObject.RemoteId.HasValue ? dataObject.RemoteId.ToString () : dataObject.Id.ToString ();
                        if (error is ServerValidationException) {
                            log.Info (Tag, error, "Server rejected {0}#{1}.", dataObject.GetType ().Name, id);
                        } else if (error is System.Net.Http.HttpRequestException) {
                            log.Info (Tag, error, "Failed to sync {0}#{1}.", dataObject.GetType ().Name, id);
                        } else {
                            log.Warning (Tag, error, "Failed to sync {0}#{1}.", dataObject.GetType ().Name, id);
                        }
                    } else {
                        graph.Remove (dataObject);
                    }
                }
            }

            return hasErrors;
        }

        private static async Task<IEnumerable<CommonData>> GetAllDirtyData ()
        {
            return Enumerable.Empty<CommonData> ()
                .Concat (await GetDirtyData<WorkspaceData> ().ConfigureAwait (false))
                .Concat (await GetDirtyData<WorkspaceUserData> ().ConfigureAwait (false))
                .Concat (await GetDirtyData<TagData> ().ConfigureAwait (false))
                .Concat (await GetDirtyData<ClientData> ().ConfigureAwait (false))
                .Concat (await GetDirtyData<ProjectData> ().ConfigureAwait (false))
                .Concat (await GetDirtyData<ProjectUserData> ().ConfigureAwait (false))
                .Concat (await GetDirtyData<TaskData> ().ConfigureAwait (false))
                .Concat (await GetDirtyData<TimeEntryData> ().ConfigureAwait (false));
        }

        private static Task<List<T>> GetDirtyData<T> ()
            where T : CommonData, new()
        {
            var store = ServiceContainer.Resolve<IDataStore> ();
            IDataQuery<T> query;

            if (typeof(T) == typeof(WorkspaceUserData)) {
                // Exclude intermediate models which we've created from assumptions (for current user
                // and without remote id) from returned models.
                var userId = ServiceContainer.Resolve<AuthManager> ().GetUserId ();
                query = (IDataQuery<T>)store.Table<WorkspaceUserData> ()
                    .Where (r => r.UserId != userId || r.RemoteId != null);
            } else if (typeof(T) == typeof(ProjectUserData)) {
                // Exclude intermediate models which we've created from assumptions (for current user
                // and without remote id) from returned models.
                var userId = ServiceContainer.Resolve<AuthManager> ().GetUserId ();
                query = (IDataQuery<T>)store.Table<ProjectUserData> ()
                    .Where (r => r.UserId != userId || r.RemoteId != null);
            } else if (typeof(T) == typeof(TimeEntryData)) {
                // Only sync non-draft time entries for current user:
                var userId = ServiceContainer.Resolve<AuthManager> ().GetUserId ();
                query = (IDataQuery<T>)store.Table<TimeEntryData> ()
                    .Where (r => r.UserId == userId && r.State != TimeEntryState.New);
            } else {
                query = store.Table<T> ();
            }

            query = query.Where (r => r.IsDirty || r.RemoteId == null || r.DeletedAt != null);

            return query.QueryAsync ();
        }

        private static async Task<Exception> PushDataObject (CommonData dataObject)
        {
            var client = ServiceContainer.Resolve<ITogglClient> ();
            var store = ServiceContainer.Resolve<IDataStore> ();

            Exception error = null;

            try {
                if (dataObject.DeletedAt != null) {
                    if (dataObject.RemoteId != null) {
                        // Delete model
                        var json = await dataObject.Export ().ConfigureAwait (false);
                        await client.Delete (json).ConfigureAwait (false);
                        await store.DeleteAsync (dataObject).ConfigureAwait (false);
                    } else {
                        // Some weird combination where the DeletedAt exists and remote Id doesn't:
                        await store.DeleteAsync (dataObject).ConfigureAwait (false);
                    }
                } else if (dataObject.RemoteId != null) {
                    var json = await dataObject.Export ().ConfigureAwait (false);
                    json = await client.Update (json).ConfigureAwait (false);
                    await json.Import (
                        forceUpdate: true
                    ).ConfigureAwait (false);
                } else {
                    var json = await dataObject.Export ().ConfigureAwait (false);
                    json = await client.Create (json).ConfigureAwait (false);
                    await json.Import (
                        localIdHint: dataObject.Id,
                        forceUpdate: true
                    ).ConfigureAwait (false);
                }
            } catch (ServerValidationException ex) {
                error = ex;
            } catch (System.Net.Http.HttpRequestException ex) {
                error = ex;
            }

            if (error is ServerValidationException) {
                dataObject.RemoteRejected = true;
                await store.PutDataAsync (dataObject);
            }

            return error;
        }

        public bool IsRunning { get; private set; }

        private DateTime? LastRun {
            get { return ServiceContainer.Resolve<ISettingsStore> ().SyncLastRun; }
            set { ServiceContainer.Resolve<ISettingsStore> ().SyncLastRun = value; }
        }
    }
}
