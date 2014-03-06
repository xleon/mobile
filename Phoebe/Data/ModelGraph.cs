using System.Collections.Generic;
using System.Linq;

namespace Toggl.Phoebe.Data
{
    /// <summary>
    /// Utility class used by SyncManager.
    /// </summary>
    public class ModelGraph
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

        public static ModelGraph FromDirty (IEnumerable<Model> models)
        {
            var graph = new ModelGraph ();

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
