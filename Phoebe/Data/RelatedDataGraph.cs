using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Toggl.Phoebe.Data.DataObjects;

namespace Toggl.Phoebe.Data
{
    /// <summary>
    /// Utility class used by SyncManager.
    /// </summary>
    public class RelatedDataGraph
    {
        private class Node
        {
            public Node (CommonData data)
            {
                Data = data;
            }

            public readonly CommonData Data;
            public readonly List<Node> Parents = new List<Node> ();
            public readonly List<Node> Children = new List<Node> ();
        }

        private readonly List<Node> nodes = new List<Node> ();

        private Node GetOrCreateNode (CommonData dataObject)
        {
            var node = GetNode (dataObject);
            if (node == null) {
                nodes.Add (node = new Node (dataObject));
            }
            return node;
        }

        private Node GetNode (CommonData dataObject)
        {
            return nodes.FirstOrDefault (n => n.Data.Matches (dataObject));
        }

        public void Add (CommonData dataObject, CommonData parent = null)
        {
            if (parent != null) {
                Add (parent);
            }

            var node = GetOrCreateNode (dataObject);

            if (parent != null) {
                var parentNode = GetOrCreateNode (parent);

                node.Parents.Add (parentNode);
                parentNode.Children.Add (node);
            }
        }

        public IEnumerable<CommonData> Remove (CommonData dataObject)
        {
            var node = GetNode (dataObject);
            if (node == null) {
                return Enumerable.Empty<CommonData> ();
            }

            var removedNodes = new List<Node> ();
            Remove (node, removedNodes);
            return removedNodes.Select (n => n.Data);
        }

        private void Remove (Node node, List<Node> deleted)
        {
            if (node == null) {
                return;
            }

            // Remove children
            foreach (var child in node.Children.ToList ()) {
                Remove (child, deleted);
            }

            // Detach from parents
            foreach (var parent in node.Parents.ToList ()) {
                parent.Children.Remove (node);
            }

            nodes.Remove (node);
            deleted.Add (node);
        }

        public int NodeCount
        {
            get { return nodes.Count; }
        }

        public IEnumerable<CommonData> Nodes
        {
            get { return nodes.Select (n => n.Data); }
        }

        public IEnumerable<CommonData> EndNodes
        {
            get {
                return nodes.Where (n => n.Children.Count == 0)
                       .Select (n => n.Data);
            }
        }

        public IEnumerable<CommonData> RemoveBranch (CommonData dataObject)
        {
            var node = GetNode (dataObject);
            if (node == null) {
                return Enumerable.Empty<CommonData> ();
            }

            var removedNodes = new List<Node> ();

            // Find elders (highest parents):
            var elders = new HashSet<Node> ();
            var parentStack = new Stack<Node> ();
            parentStack.Push (node);

            while (parentStack.Count > 0) {
                var parentNode = parentStack.Pop ();

                if (parentNode.Parents.Count == 0) {
                    elders.Add (parentNode);
                } else {
                    foreach (var grandparent in parentNode.Parents) {
                        parentStack.Push (grandparent);
                    }
                }
            }

            // Remove elders from graph:
            foreach (var elder in elders) {
                Remove (elder, removedNodes);
            }

            return removedNodes.Select (n => n.Data);
        }

        public static async Task<RelatedDataGraph> FromDirty (IEnumerable<CommonData> objects)
        {
            var graph = new RelatedDataGraph ();

            var dataCache = new List<CommonData> (objects);
            var parentStack = new Stack<CommonData> ();
            var objectsStack = new Stack<IEnumerable<CommonData>> ();

            parentStack.Push (null);
            objectsStack.Push (dataCache.ToList ());

            CommonData parent;
            while (parentStack.Count > 0) {
                parent = parentStack.Pop ();
                objects = objectsStack.Pop ()
                          .Where ((m) => m != null && (m.IsDirty || m.RemoteId == null || m.DeletedAt != null));

                foreach (var dataObject in objects) {
                    parentStack.Push (dataObject);
                    objectsStack.Push (await GetRelatedData (dataObject, dataCache).ConfigureAwait (false));
                    graph.Add (dataObject, parent);
                }
            }

            return graph;
        }

        private static async Task<IEnumerable<CommonData>> GetRelatedData (CommonData data, List<CommonData> cache)
        {
            var dataObjects = new List<CommonData> ();

            foreach (var relation in data.GetRelations ()) {
                if (relation.Id == null) {
                    continue;
                }

                // Query data in a synchronous manner to guarantee exclusive access to cache (without locking)
                // and give other code chance to have intermediate queries.
                var relatedObject = cache.FirstOrDefault (d => d.Id == relation.Id && d.GetType () == relation.Type);
                if (relatedObject == null) {
                    relatedObject = await relation.ToListAsync ().ConfigureAwait (false);
                    if (relatedObject != null) {
                        cache.Add (relatedObject);
                    }
                }

                if (relatedObject != null) {
                    dataObjects.Add (relatedObject);
                }
            }

            return dataObjects;
        }
    }
}
