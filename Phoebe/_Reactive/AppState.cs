using System;
using System.Linq;
using System.Collections.Generic;
using Toggl.Phoebe._Data.Models;

namespace Toggl.Phoebe._Reactive
{
    // TODO: Dummy interface
    public interface IAction
    {
    }

    public interface Reducer
    {
        object ReduceLeaf (IAction action, object oldLeaf);
        ITreeNode ReduceNode (IAction action, ITreeNode oldNode, IEnumerable<object> newChildren);
    }

    public interface ITreeNode
    {
        Type ChildrenType { get; }
        IEnumerable<object> Children { get; }
        ITreeNode CreateNew (IEnumerable<object> newChildren);
    }

    public class TreeVisitor
    {
        public IDictionary<Type, Reducer> Actions { get; private set; }

        public TreeVisitor (IDictionary<Type, Reducer> actions)
        {
            Actions = actions;
        }

        public ITreeNode VisitNode (IAction action, ITreeNode node, Type nodeType)
        {
            var typ = node.ChildrenType;
            var newChildren = typ is ITreeNode
                ? node.Children.Select (x => VisitNode (action, node, typ))
                : VisitLeafs (action, node.Children, typ);

            Reducer reducer;
            if (Actions.TryGetValue (nodeType, out reducer)) {
                return reducer.ReduceNode (action, node, newChildren);
            }
            else {
                return node.CreateNew (newChildren);
            }
        }

        public IEnumerable<object> VisitLeafs (IAction action, IEnumerable<object> leafs, Type leafType)
        {
            Reducer reducer;
            if (Actions.TryGetValue (leafType, out reducer)) {
                return leafs.Select (x => reducer.ReduceLeaf (action, x));
            }
            else {
                return leafs;
            }
        }
    }

    public class AppState : ITreeNode
    {
        public UserNode User { get; private set; }

        public AppState ()
        {
        }

        public IEnumerable<object> Children {
            get {
                return new[] { User };
            }
        }

        public Type ChildrenType {
            get {
                return typeof(UserNode);
            }
        }

        public ITreeNode CreateNew (IEnumerable<object> newChildren)
        {
            throw new NotImplementedException ();
        }
    }

    public class UserNode : ITreeNode
    {
        public static WorkspaceNode EmptyWorkspace = new WorkspaceNode ();

        public UserData Data { get; private set; }
        public IList<WorkspaceNode> Workspaces { get; private set; }

        public UserNode ()
        {
            Workspaces = new List<WorkspaceNode> () { EmptyWorkspace };
        }

        public UserNode (UserData data) : this()
        {
            Data = data;
        }

        public IEnumerable<object> Children {
            get {
                return Workspaces;
            }
        }

        public Type ChildrenType {
            get {
                return typeof(WorkspaceNode);
            }
        }

        public ITreeNode CreateNew (IEnumerable<object> newChildren)
        {
            throw new NotImplementedException ();
        }
    }

    public class WorkspaceNode : ITreeNode
    {
        public static ClientNode EmptyClient = new ClientNode ();

        public WorkspaceData Data { get; private set; }
        public IList<ClientNode> Clients { get; private set; }

        public WorkspaceNode ()
        {
            Clients = new List<ClientNode> () { EmptyClient };
        }

        public WorkspaceNode (WorkspaceData data) : this()
        {
            Data = data;
        }

        public IEnumerable<object> Children {
            get {
                return Clients;
            }
        }

        public Type ChildrenType {
            get {
                return typeof(ClientNode);
            }
        }

        public ITreeNode CreateNew (IEnumerable<object> newChildren)
        {
            throw new NotImplementedException ();
        }
    }

    public class ClientNode : ITreeNode
    {
        public static ProjectNode EmptyProject = new ProjectNode ();

        public ClientData Data { get; private set; }
        public IList<ProjectNode> Projects { get; private set; }

        public ClientNode ()
        {
            Projects = new List<ProjectNode> () { EmptyProject };
        }

        public ClientNode (ClientData data) : this()
        {
            Data = data;
        }

        public IEnumerable<object> Children {
            get {
                return Projects;
            }
        }

        public Type ChildrenType {
            get {
                return typeof(ProjectNode);
            }
        }

        public ITreeNode CreateNew (IEnumerable<object> newChildren)
        {
            throw new NotImplementedException ();
        }
    }

    public class ProjectNode : ITreeNode
    {
        public static TaskNode EmptyTask = new TaskNode ();

        public ProjectData Data { get; private set; }
        public IList<TaskNode> Tasks { get; private set; }

        public ProjectNode ()
        {
            Tasks = new List<TaskNode> () { EmptyTask };
        }

        public ProjectNode (ProjectData data) : this()
        {
            Data = data;
        }

        public IEnumerable<object> Children {
            get {
                return Tasks;
            }
        }

        public Type ChildrenType {
            get {
                return typeof(TaskNode);
            }
        }

        public ITreeNode CreateNew (IEnumerable<object> newChildren)
        {
            throw new NotImplementedException ();
        }
    }

    public class TaskNode : ITreeNode
    {
        public TaskData Data { get; private set; }
        public IList<TimeEntryData> TimeEntries { get; private set; }

        public TaskNode ()
        {
            TimeEntries = new List<TimeEntryData> ();
        }

        public TaskNode (TaskData data) : this()
        {
            Data = data;
        }

        public IEnumerable<object> Children {
            get {
                return TimeEntries;
            }
        }

        public Type ChildrenType {
            get {
                return typeof(TimeEntryData);
            }
        }

        public ITreeNode CreateNew (IEnumerable<object> newChildren)
        {
            throw new NotImplementedException ();
        }
    }
}

