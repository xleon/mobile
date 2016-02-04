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
            var info = new TimeEntryInfo ();

            // TODO: Check if dictionaries contain ids
            info.ProjectData = teData.ProjectId != Guid.Empty
                ? state.Projects[teData.ProjectId]
                : new ProjectData ();
            info.ClientData = info.ProjectData.ClientId != Guid.Empty
                ? state.Clients[info.ProjectData.ClientId]
                : new ClientData ();
            info.TaskData = teData.TaskId != Guid.Empty
                ? state.Tasks[teData.TaskId]
                : new TaskData ();
            info.Description = teData.Description;
            info.Color = (info.ProjectData.Id != Guid.Empty) ? info.ProjectData.Color : -1;
            info.IsBillable = teData.IsBillable;

            // TODO: Tags
            //info.NumberOfTags

            return info;
        }

        public void TestUpdater(TimerState state, IDataMsg dataMsg)
        {
            var teMsg = dataMsg.GetDataOrDefault<TimeEntryMsg> ();
            if (teMsg == null) {
                return;
            }

            foreach (var msg in teMsg) {

                // TODO: Check this condition
                if (msg.Item2.StartTime < state.LowerLimit) {
                    continue;
                }

                RichTimeEntry oldTe;
                if (state.TimeEntries.TryGetValue (msg.Item2.Id, out oldTe)) {
                    // Remove old time entry
                    state.TimeEntries.Remove (msg.Item2.Id);
                }

                if (msg.Item1 == DataAction.Put) {
                    // Load info and insert new entry
                    var newTe = new TimeEntryData (msg.Item2); // Protect the reference
					var newInfo = LoadTimeEntryInfo (state, newTe);
                    state.TimeEntries.Add (newTe.Id, new RichTimeEntry (newTe, newInfo));
                }
            }
        }

        public TimeEntryData CreateTimeEntryData (DateTime startTime)
        {
            return new TimeEntryData {
                Id = Guid.NewGuid (),
                StartTime = startTime,
                StopTime = startTime.AddMinutes (1),
                UserId = userId,
                WorkspaceId = workspaceId,
                Description = "Test Entry",
                State = TimeEntryState.Finished,
                Tags = new List<string> ()
            };
        }

        public IAppState GetState ()
        {
            return appState;
        }

        public void SendMessage (DataAction action, TimeEntryData data)
        {
            var teMsg = new TimeEntryMsg (DataDir.Outcoming, action, data);
            updater.Update (appState, DataMsg.Success(DataTag.TimeEntryLoad, teMsg));
        }

        [Test]
        public void TestAddEntry ()
        {
            var oldCount = GetState ().TimerState.TimeEntries.Count;
            var te = CreateTimeEntryData (DateTime.Now);
            SendMessage (DataAction.Put, te);

            var newCount = GetState ().TimerState.TimeEntries.Count;
            Assert.AreEqual (oldCount + 1, newCount);
        }

        [Test]
        public void TestTryModifyEntry ()
        {
            var oldDescription = "OLD";
            var te = CreateTimeEntryData (DateTime.Now);
            te.Description = oldDescription;
            SendMessage (DataAction.Put, te);

            // Modifying the entry now shouldn't affect the state
            te.Description = "NEW";
            var description = GetState ().TimerState.TimeEntries[te.Id].Data.Description;
            Assert.AreEqual (oldDescription, description);
        }
    }
}

