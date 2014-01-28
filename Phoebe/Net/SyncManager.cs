using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Net.Http;
using System.Threading.Tasks;
using Toggl.Phoebe.Data;
using Toggl.Phoebe.Data.Models;
using XPlatUtils;

namespace Toggl.Phoebe.Net
{
    public class SyncManager
    {
        #pragma warning disable 0414
        private readonly object subscriptionModelsCommited;
        #pragma warning restore 0414

        public SyncManager ()
        {
            var bus = ServiceContainer.Resolve<MessageBus> ();
            subscriptionModelsCommited = bus.Subscribe<ModelsCommittedMessage> (OnModelsCommited);
        }

        private void OnModelsCommited (ModelsCommittedMessage msg)
        {
            Run (SyncMode.Auto);
        }

        public async void Run (SyncMode mode = SyncMode.Full)
        {
            if (!ServiceContainer.Resolve<AuthManager> ().IsAuthenticated)
                return;
            if (IsRunning)
                return;

            var bus = ServiceContainer.Resolve<MessageBus> ();
            var client = ServiceContainer.Resolve<ITogglClient> ();

            // Resolve automatic sync mode to actual mode
            if (mode == SyncMode.Auto) {
                if (LastRun != null && LastRun > DateTime.UtcNow - TimeSpan.FromHours (1)) {
                    mode = SyncMode.Push;
                } else {
                    mode = SyncMode.Full;
                }
            }

            IsRunning = true;
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
                    var changes = await client.GetChanges (LastRun);

                    changes.User.IsPersisted = true;
                    foreach (var m in changes.Workspaces) {
                        if (m.RemoteDeletedAt == null) {
                            m.IsPersisted = true;
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
                    LastRun = changes.Timestamp;
                }

                if (mode.HasFlag (SyncMode.Push)) {
                    // Construct dependency graph:
                    var graph = Graph.FromDirty (Enumerable.Empty<Model> ()
                        .Union (QueryDirtyModels<WorkspaceModel> ())
                        .Union (QueryDirtyModels<TagModel> ())
                        .Union (QueryDirtyModels<ClientModel> ())
                        .Union (QueryDirtyModels<ProjectModel> ())
                        .Union (QueryDirtyModels<TaskModel> ())
                        .Union (QueryDirtyModels<TimeEntryModel> ().ForCurrentUser ()));

                    // Purge invalid nodes:
                    var models = graph.Nodes.Where ((m) => !m.IsValid && m.RemoteId == null).ToList ();
                    foreach (var model in models) {
                        graph.RemoveBranch (model);
                    }
                    models = graph.Nodes.Where ((m) => !m.IsValid && m.RemoteId != null).ToList ();
                    foreach (var model in models) {
                        graph.Remove (model);
                    }

                    // Start pushing the dependencies from the end nodes up
                    var tasks = new List<Task<Exception>> ();
                    while (true) {
                        tasks.Clear ();

                        models = graph.EndNodes.ToList ();
                        if (models.Count == 0)
                            break;

                        foreach (var model in models) {
                            tasks.Add (PushModel (model));
                        }

                        await Task.WhenAll (tasks);

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
                                // TODO: Log error?
                            } else {
                                graph.Remove (model);
                            }
                        }
                    }
                }
            } catch (Exception e) {
                hasErrors = true;
                ex = e;
            } finally {
                IsRunning = false;
                bus.Send (new SyncFinishedMessage (this, mode, hasErrors, ex));
            }
        }

        private static IModelQuery<T> QueryDirtyModels<T> ()
            where T : Model, new()
        {
            return Model.Query<T> ((m) => m.IsDirty || m.RemoteId == null || m.DeletedAt != null);
        }

        private async Task<Exception> PushModel (Model model)
        {
            var client = ServiceContainer.Resolve<ITogglClient> ();
            try {
                if (model.DeletedAt != null) {
                    if (model.RemoteId != null) {
                        // Delete model
                        await client.Delete (model);
                        model.IsPersisted = false;
                    } else {
                        // Some weird combination where the DeletedAt exists and remote Id doesn't:
                        model.IsPersisted = false;
                    }
                } else if (model.RemoteId != null) {
                    await client.Update (model);
                } else {
                    await client.Create (model);
                }
            } catch (HttpRequestException ex) {
                return ex;
            }

            return null;
        }

        public bool IsRunning { get; private set; }

        private DateTime? LastRun {
            get { return ServiceContainer.Resolve<ISettingsStore> ().SyncLastRun; }
            set { ServiceContainer.Resolve<ISettingsStore> ().SyncLastRun = value; }
        }

        private class Graph
        {
            private class Node
            {
                public readonly HashSet<Model> Parents = new HashSet<Model> ();
                public readonly HashSet<Model> Children = new HashSet<Model> ();
            }

            private readonly Dictionary<Model, Node> nodes = new Dictionary<Model, Node> ();

            public void Add (Model model, Model parent = null)
            {
                if (parent != null)
                    Add (parent);

                Node node;
                if (!nodes.TryGetValue (model, out node)) {
                    nodes [model] = node = new Node ();
                }

                if (parent != null) {
                    node.Parents.Add (parent);
                    nodes [parent].Children.Add (model);
                }
            }

            public IEnumerable<Model> Remove (Model model)
            {
                Node node;
                if (!nodes.TryGetValue (model, out node))
                    return Enumerable.Empty<Model> ();

                var removedModels = new List<Model> ();
                Remove (model, removedModels);
                return removedModels;
            }

            private void Remove (Model model, List<Model> deleted)
            {
                Node node;
                if (!nodes.TryGetValue (model, out node))
                    return;

                // Remove children
                foreach (var child in node.Children) {
                    Remove (child, deleted);
                }

                // Detach from parents
                foreach (var parent in node.Parents) {
                    nodes [parent].Children.Remove (model);
                }

                nodes.Remove (model);
                deleted.Add (model);
            }

            public int NodeCount {
                get { return nodes.Count; }
            }

            public IEnumerable<Model> Nodes {
                get { return nodes.Keys; }
            }

            public IEnumerable<Model> EndNodes {
                get {
                    return nodes.Where ((kvp) => kvp.Value.Children.Count == 0)
                            .Select ((kvp) => kvp.Key);
                }
            }

            public IEnumerable<Model> RemoveBranch (Model model)
            {
                Node node;
                if (!nodes.TryGetValue (model, out node))
                    return Enumerable.Empty<Model> ();

                var removedModels = new List<Model> ();

                // Find elders (highest parents):
                var elders = new HashSet<Model> ();
                var parentStack = new Stack<Model> ();
                parentStack.Push (model);
                while (parentStack.Count > 0) {
                    var parent = parentStack.Pop ();

                    var parentNode = nodes [parent];
                    if (parentNode.Parents.Count == 0) {
                        elders.Add (parent);
                    } else {
                        foreach (var grandparent in parentNode.Parents) {
                            parentStack.Push (grandparent);
                        }
                    }
                }

                // Remove elders from graph:
                foreach (var elder in elders) {
                    Remove (elder, removedModels);
                }

                return removedModels;
            }

            public static Graph FromDirty (IEnumerable<Model> models)
            {
                var graph = new Graph ();

                Stack<Model> parentStack = new Stack<Model> ();
                Stack<IEnumerable<Model>> modelsStack = new Stack<IEnumerable<Model>> ();

                parentStack.Push (null);
                modelsStack.Push (models);

                Model parent;
                while (parentStack.Count > 0) {
                    parent = parentStack.Pop ();
                    models = modelsStack.Pop ()
                        .Where ((m) => m != null && (m.IsDirty || m.RemoteId == null || m.DeletedAt != null));

                    foreach (var model in models) {
                        parentStack.Push (model);
                        modelsStack.Push (model.GetAllForeignModels ().Values);
                        graph.Add (model, parent);
                    }
                }

                return graph;
            }
        }
    }
}
