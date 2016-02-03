using System;
using System.Linq;
using System.Collections.Generic;
using Toggl.Phoebe._Data;
using Toggl.Phoebe._Data.Models;

namespace Toggl.Phoebe._Reactive
{
    // TODO: Expand this interface if needed
    public interface IAction
    {
        IDataMsg Message { get; }
    }

    public interface IReducer
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
        public Dictionary<Type, IReducer> Actions { get; private set; }

        public TreeVisitor (Dictionary<Type, IReducer> actions)
        {
            Actions = actions;
        }

        public ITreeNode VisitNode (IAction action, ITreeNode node, Type nodeType)
        {
            var typ = node.ChildrenType;
            var newChildren = typ is ITreeNode
                ? node.Children.Select (x => VisitNode (action, node, typ))
                : VisitLeafs (action, node.Children, typ);

            IReducer reducer;
            if (Actions.TryGetValue (nodeType, out reducer)) {
                return reducer.ReduceNode (action, node, newChildren);
            }
            else {
                return node.CreateNew (newChildren);
            }
        }

        public IEnumerable<object> VisitLeafs (IAction action, IEnumerable<object> leafs, Type leafType)
        {
            IReducer reducer;
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
        public static WorkspaceNode EmptyWorkspace = new WorkspaceNode ();

        public UserData Data { get; private set; }
        public Dictionary<Guid, WorkspaceNode> Workspaces { get; private set; }

        // TODO: Other AppState values: IsLoading, etc...

        public AppState ()
        {
            Workspaces = new Dictionary<Guid, WorkspaceNode> { { Guid.Empty, EmptyWorkspace } };
        }

        public AppState (UserData data) : this()
        {
            Data = data;
        }

        public IEnumerable<object> Children {
            get {
                return Workspaces.Values;
            }
        }

        public Type ChildrenType {
            get {
                return typeof(WorkspaceNode);
            }
        }

        public ITreeNode CreateNew (IEnumerable<object> newChildren)
        {
            // TODO: Copy other AppState values
            var newNode = new AppState (Data);

            foreach (var child in newChildren.Cast<WorkspaceNode> ()) {
                newNode.Workspaces.Add (child.Data.Id, child);
            }
            return newNode;
        }
    }

    public class WorkspaceNode : ITreeNode
    {
        public static ProjectNode EmptyProject = new ProjectNode ();

        public WorkspaceData Data { get; private set; }
        public Dictionary<Guid, ClientData> Clients { get; private set; }
        public Dictionary<Guid, ProjectNode> Projects { get; private set; }

        public WorkspaceNode ()
        {
            Clients = new Dictionary<Guid, ClientData> ();
            Projects = new Dictionary<Guid, ProjectNode> { { Guid.Empty, EmptyProject } };
        }

        public WorkspaceNode (WorkspaceData data) : this()
        {
            Data = data;
        }

        public IEnumerable<object> Children {
            get {
                return Projects.Values;
            }
        }

        public Type ChildrenType {
            get {
                return typeof(ProjectNode);
            }
        }

        public ITreeNode CreateNew (IEnumerable<object> newChildren)
        {
            var newNode = new WorkspaceNode (Data);
            foreach (var client in Clients) {
                newNode.Clients.Add (client.Key, client.Value);
            }
            foreach (var child in newChildren.Cast<ProjectNode> ()) {
                newNode.Projects.Add (child.Data.Id, child);
            }
            return newNode;
        }
    }

    public class ProjectNode : ITreeNode
    {
        public static TaskNode EmptyTask = new TaskNode ();

        public ProjectData Data { get; private set; }
        public Dictionary<Guid, TaskNode> Tasks { get; private set; }

        public ProjectNode ()
        {
            Tasks = new Dictionary<Guid, TaskNode> { { Guid.Empty, EmptyTask } };
        }

        public ProjectNode (ProjectData data) : this()
        {
            Data = data;
        }

        public IEnumerable<object> Children {
            get {
                return Tasks.Values;
            }
        }

        public Type ChildrenType {
            get {
                return typeof(TaskNode);
            }
        }

        public ITreeNode CreateNew (IEnumerable<object> newChildren)
        {
            var newNode = new ProjectNode (Data);
            foreach (var child in newChildren.Cast<TaskNode> ()) {
                newNode.Tasks.Add (child.Data.Id, child);
            }
            return newNode;
        }
    }

    public class TaskNode : ITreeNode
    {
        public TaskData Data { get; private set; }
        public Dictionary<Guid, TimeEntryData> TimeEntries { get; private set; }

        public TaskNode ()
        {
            TimeEntries = new Dictionary<Guid, TimeEntryData> ();
        }

        public TaskNode (TaskData data) : this()
        {
            Data = data;
        }

        public IEnumerable<object> Children {
            get {
                return TimeEntries.Values;
            }
        }

        public Type ChildrenType {
            get {
                return typeof(TimeEntryData);
            }
        }

        public ITreeNode CreateNew (IEnumerable<object> newChildren)
        {
            var newNode = new TaskNode (Data);
            foreach (var child in newChildren.Cast<TimeEntryData> ()) {
                newNode.TimeEntries.Add (child.Id, child);
            }
            return newNode;
        }
    }
}

