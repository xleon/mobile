using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using Toggl.Phoebe.Tests;
using Toggl.Phoebe._Data;
using Toggl.Phoebe._Data.Json;
using Toggl.Phoebe._Data.Models;
using Toggl.Phoebe._Helpers;
using Toggl.Phoebe._Net;
using Toggl.Phoebe._Reactive;
using Toggl.Phoebe._ViewModels.Timer;
using XPlatUtils;

namespace Toggl.Phoebe.Tests.Reactive
{
    [TestFixture]
    public class AppStateTest : Test
    {
        public class TestReducer : IReducer
        {
            public object ReduceLeaf (IAction action, object oldLeaf)
            {
                // We're not dealing with leafs here
                return oldLeaf;
            }

            // TODO: Make all dic additions and replacements totally immutable?
            Dictionary<Guid, TaskNode> createTask (
                Dictionary<Guid, TaskNode> oldDic, TimeEntryData teData)
            {
                throw new NotImplementedException ();
            }

            Dictionary<Guid, TaskNode> createOrReplaceTask (
                Dictionary<Guid, TaskNode> oldDic, DataAction action, TimeEntryData teData)
            {
                if (!teData.TaskId.HasValue)
                    throw new Exception ("TimeEntryData must have TaskId at this point");

                TaskNode task;
                if (oldDic.TryGetValue (teData.TaskId.Value, out task)) {
                    TimeEntryData te;
                    if (task.TimeEntries.TryGetValue (teData.Id, out te)) {
                        var newEntries = action == DataAction.Put
                            ? task.TimeEntries.ReplaceInPlace (te.Id, teData)   // Replace
                            : task.TimeEntries.RemoveInPlace (te.Id);           // Delete

                        return oldDic.ReplaceInPlace (task.Data.Id, (TaskNode)task.CreateNew (newEntries.Values));
                    }
                    else {
                        if (action == DataAction.Put) {                         // Insert
                            var newEntries = task.TimeEntries.AddInPlace (teData.Id, teData);
                            return oldDic.ReplaceInPlace (task.Data.Id, (TaskNode)task.CreateNew (newEntries.Values));
                        }
                        else {                                                  // Do nothing
                            return oldDic;
                        }
                    }
                }
                else {
                    return action == DataAction.Put
                        ? createTask (oldDic, teData)    // Insert new task
                        : oldDic;                        // Do nothing
                }

                return oldDic;
            }

            Dictionary<Guid, ProjectNode> createProject (
                Dictionary<Guid, ProjectNode> oldDic, TimeEntryData teData)
            {
                throw new NotImplementedException ();
            }

            Dictionary<Guid, ProjectNode> createOrReplaceProject (
                Dictionary<Guid, ProjectNode> oldDic, DataAction action, TimeEntryData teData)
            {
                if (!teData.ProjectId.HasValue)
                    throw new Exception ("TimeEntryData must have ProjectId at this point");

                ProjectNode project;
                if (oldDic.TryGetValue (teData.ProjectId.Value, out project)) {
                    var newTasks = createOrReplaceTask (project.Tasks, action, teData);
                    return oldDic.ReplaceInPlace (project.Data.Id, (ProjectNode)project.CreateNew (newTasks.Values));
                }
                else {
                    return action == DataAction.Put
                        ? createProject (oldDic, teData) // Insert new project
                        : oldDic;                        // Do nothing 
                }
            }

            Dictionary<Guid, WorkspaceNode> createWorkspace (
                Dictionary<Guid, WorkspaceNode> oldDic, TimeEntryData teData)
            {
                throw new NotImplementedException ();
            }

            Dictionary<Guid, WorkspaceNode> createOrReplaceWorkspace (
                Dictionary<Guid, WorkspaceNode> oldDic, DataAction action, TimeEntryData teData)
            {
                WorkspaceNode ws;
                if (oldDic.TryGetValue (teData.WorkspaceId, out ws)) {
                    var newProjects = createOrReplaceProject (ws.Projects, action, teData);
                    return oldDic.ReplaceInPlace (ws.Data.Id, (WorkspaceNode)ws.CreateNew (newProjects.Values));
                }
                else {
                    return action == DataAction.Put
                            ? createWorkspace (oldDic, teData)  // Insert new project
                            : oldDic;                           // Do nothing 
                }
            }

            public ITreeNode ReduceNode (IAction action, ITreeNode oldNode, IEnumerable<object> newChildren)
            {
                var app = oldNode as AppState;
                var messsages = action.Message.GetDataOrDefault<TimeEntryMsg> ();

                if (app == null || messsages == null) {
                    return oldNode;
                }

                var seed = newChildren.Cast <WorkspaceNode> ()
                    .ToDictionary (x => x.Data.Id); 

                var newChildren2 = messsages.Aggregate (seed,
                    (workspaces, msg) => createOrReplaceWorkspace (workspaces, msg.Item1, msg.Item2));

                return app.CreateNew (newChildren2.Values);
            }
        }

        public override void SetUp ()
        {
        }
    }
}

