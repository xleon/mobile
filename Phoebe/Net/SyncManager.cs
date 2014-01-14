using System;
using XPlatUtils;
using Toggl.Phoebe.Data;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Net.Http;
using System.Linq.Expressions;

namespace Toggl.Phoebe.Net
{
    public class SyncManager
    {
        public async Task Run (SyncMode mode = SyncMode.Auto)
        {
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
                    var q = Model.Query<TimeEntryModel> ().OrderBy ((te) => te.StartTime, false).Skip (1000).Take (200);
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
                        .Union (Model.Query<WorkspaceModel> ((m) => m.IsDirty || m.RemoteId == null || m.DeletedAt != null))
                        .Union (Model.Query<ClientModel> ((m) => m.IsDirty || m.RemoteId == null || m.DeletedAt != null))
                        .Union (Model.Query<ProjectModel> ((m) => m.IsDirty || m.RemoteId == null || m.DeletedAt != null))
                        .Union (Model.Query<TaskModel> ((m) => m.IsDirty || m.RemoteId == null || m.DeletedAt != null))
                        .Union (Model.Query<TimeEntryModel> ((m) => m.IsDirty || m.RemoteId == null || m.DeletedAt != null).ForCurrentUser ()));

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

                        await Task.WhenAll (tasks);

                        for (var i = 0; i < tasks.Count; i++) {
                            var model = models [i];
                            var error = tasks [i].Result;

                            if (error != null) {
                                graph.RemoveBranch (model);
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
            // TODO:
            get { return null; }
            set { }
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
