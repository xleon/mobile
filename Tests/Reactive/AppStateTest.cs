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
        Guid userId = Guid.NewGuid ();
        Guid workspaceId = Guid.NewGuid ();
        AppState appState;
        CompositeUpdater<AppState> updater;

        public override void SetUp ()
        {
            base.SetUp ();

            // TODO: Get initial state of application
            appState = new AppState ();
            updater = new CompositeUpdater<AppState> ()
                .Add (x => x.TimerState, TestUpdater);
        }

        public static TimeEntryInfo LoadTimeEntryInfo (TimerState state, TimeEntryData teData)
        {
            // TODO: Check if dictionaries contain ids
            var projectData = teData.ProjectId != Guid.Empty
                ? state.Projects[teData.ProjectId]
                : new ProjectData ();
            var clientData = projectData.ClientId != Guid.Empty
                ? state.Clients[projectData.ClientId]
                : new ClientData ();
            var taskData = teData.TaskId != Guid.Empty
                ? state.Tasks[teData.TaskId]
                : new TaskData ();
            var color = (projectData.Id != Guid.Empty) ? projectData.Color : -1;

            return new TimeEntryInfo (
                projectData,
                clientData,
                taskData,
                color);
        }

        public void TestUpdater(TimerState state, IDataMsg dataMsg)
        {
            var teMsg = dataMsg.GetDataOrDefault<TimeEntryMsg> ();
            if (teMsg == null) {
                return;
            }

//            foreach (var msg in teMsg) {
//
//                // TODO: Check this condition
//                if (msg.Item2.StartTime < state.LowerLimit) {
//                    continue;
//                }
//
//                RichTimeEntry oldTe;
//                if (state.TimeEntries.TryGetValue (msg.Item2.Id, out oldTe)) {
//                    // Remove old time entry
//                    state.TimeEntries.Remove (msg.Item2.Id);
//                }
//
//                if (msg.Item1 == DataAction.Put) {
//                    // Load info and insert new entry
//                    var newTe = new TimeEntryData (msg.Item2); // Protect the reference
//					var newInfo = LoadTimeEntryInfo (state, newTe);
//                    state.TimeEntries.Add (newTe.Id, new RichTimeEntry (newTe, newInfo));
//                }
//            }
        }

        public IAppState GetState ()
        {
            return appState;
        }

        public void SendMessage (TimeEntryData data)
        {
            var teMsg = new TimeEntryMsg (DataDir.Outcoming, data);
            updater.Update (appState, DataMsg.Success(DataTag.TimeEntryLoad, teMsg));
        }

//        [Test]
        public void TestAddEntry ()
        {
            var oldCount = GetState ().TimerState.TimeEntries.Count;
            var te = Util.CreateTimeEntryData (DateTime.Now);
            SendMessage (te);

            var newCount = GetState ().TimerState.TimeEntries.Count;
            Assert.AreEqual (oldCount + 1, newCount);
        }

//        [Test]
        public void TestTryModifyEntry ()
        {
            const string oldDescription = "OLD";
            var te = Util.CreateTimeEntryData (DateTime.Now);
            te.Description = oldDescription;
            SendMessage (te);

            // Modifying the entry now shouldn't affect the state
            te.Description = "NEW";
            var description = GetState ().TimerState.TimeEntries[te.Id].Data.Description;
            Assert.AreEqual (oldDescription, description);
        }
    }
}

