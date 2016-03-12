using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Toggl.Phoebe._Data;
using Toggl.Phoebe._Data.Models;
using Toggl.Phoebe._Reactive;
using XPlatUtils;

namespace Toggl.Phoebe.Tests.Reactive
{
    [TestFixture]
    public class AppStateTest : Test
    {
        public override void Init ()
        {
            base.Init ();

            var platformUtils = new PlatformUtils ();
            ServiceContainer.RegisterScoped<IPlatformUtils> (platformUtils);

            RxChain.Init (Util.GetInitAppState (), RxChain.InitMode.TestStoreManager);
        }

        public override void Cleanup ()
        {
            base.Cleanup ();
            RxChain.Cleanup ();
        }

        [Test]
        public void TestAddEntry ()
        {
            IDisposable subscription = null;
            var te = Util.CreateTimeEntryData (DateTime.Now);

            subscription =
                StoreManager
                .Singleton
                .Observe (state => state.TimerState)
            .Subscribe (state => {
                Assert.True (state.TimeEntries.ContainsKey (te.Id));
                subscription.Dispose ();
            });

            RxChain.Send (new DataMsg.TimeEntryPut (te));
        }

        [Test]
        public void TestStopEntry ()
        {
            int step = 0;
            IDisposable subscription = null;
            var te = Util.CreateTimeEntryData (DateTime.Now);
            te.State = TimeEntryState.Running;

            subscription =
                StoreManager
                .Singleton
                .Observe (state => state.TimerState)
            .Subscribe (state => {
                switch (step) {
                case 0:
                    var te2 = state.TimeEntries[te.Id];
                    Assert.True (te2.Data.State == TimeEntryState.Running);
                    step++;
                    break;
                case 1:
                    var te3 = state.TimeEntries[te.Id];
                    Assert.True (te3.Data.State == TimeEntryState.Finished);
                    subscription.Dispose ();
                    break;
                }
            });

            RxChain.Send (new DataMsg.TimeEntryPut (te));
            RxChain.Send (new DataMsg.TimeEntryStop (te));
        }

        [Test]
        public void TestRemoveEntry ()
        {
            int step = 0;
            IDisposable subscription = null;
            var te = Util.CreateTimeEntryData (DateTime.Now);
            var db = ServiceContainer.Resolve<ISyncDataStore> ();

            subscription =
                StoreManager
                .Singleton
                .Observe (state => state.TimerState)
            .Subscribe (state => {
                switch (step) {
                case 0:
                    Assert.True (state.TimeEntries.ContainsKey (te.Id));
                    step++;
                    break;
                case 1:
                    Assert.False (state.TimeEntries.ContainsKey (te.Id));
                    // The entry should have also been deleted from the db
                    Assert.False (db.Table<TimeEntryData> ().Any (x => x.Id == te.Id));
                    subscription.Dispose ();
                    break;
                }
            });

            RxChain.Send (new DataMsg.TimeEntryPut (te));

            RxChain.Send (new DataMsg.TimeEntriesRemovePermanently (
                              new List<ITimeEntryData> { te }));
        }

        [Test]
        public void TestRemoveEntryWithUndo ()
        {
            int step = 0;
            IDisposable subscription = null;
            var te = Util.CreateTimeEntryData (DateTime.Now);
            var db = ServiceContainer.Resolve<ISyncDataStore> ();

            subscription =
                StoreManager
                .Singleton
                .Observe (state => state.TimerState)
            .Subscribe (state => {
                switch (step) {
                // Add
                case 0:
                    Assert.True (state.TimeEntries.ContainsKey (te.Id));
                    step++;
                    break;
                // Remove with undo
                case 1:
                    Assert.False (state.TimeEntries.ContainsKey (te.Id));
                    // The entry shouldn't actually be deleted from the db
                    Assert.True (db.Table<TimeEntryData> ().Any (x => x.Id == te.Id));
                    step++;
                    break;
                // Restore from undo
                case 2:
                    Assert.True (state.TimeEntries.ContainsKey (te.Id));
                    subscription.Dispose ();
                    break;
                }
            });

            RxChain.Send (new DataMsg.TimeEntryPut (te));

            RxChain.Send (new DataMsg.TimeEntriesRemoveWithUndo (
                              new List<ITimeEntryData> { te }));

            RxChain.Send (new DataMsg.TimeEntriesRestoreFromUndo (
                              new List<ITimeEntryData> { te }));
        }

        [Test]
        public void TestTryModifyEntry ()
        {
            const string oldDescription = "OLD";
            var te = Util.CreateTimeEntryData (DateTime.Now);
            te.Description = oldDescription;
            IDisposable subscription = null;
            TimerState receivedState = null;

            subscription =
                StoreManager
                .Singleton
                .Observe (state => state.TimerState)
            .Subscribe (state => {
                receivedState = state;
                subscription.Dispose ();
            });


            RxChain.Send (new DataMsg.TimeEntryPut (te));

            // Modifying the entry now shouldn't affect the state
            te.Description = "NEW";
            var description = receivedState.TimeEntries[te.Id].Data.Description;
            Assert.AreEqual (oldDescription, description);
        }

        [Test]
        public void TestSeveralSuscriptors ()
        {
            int step = 0;
            IDisposable subscription1 = null, subscription2 = null;
            var te = Util.CreateTimeEntryData (DateTime.Now);
            var db = ServiceContainer.Resolve<ISyncDataStore> ();

            subscription1 =
                StoreManager
                .Singleton
                .Observe (state => state.TimerState)
            .Subscribe (state => {
                Assert.True (state.TimeEntries.ContainsKey (te.Id));
                subscription1.Dispose ();
            });

            subscription2 =
                StoreManager
                .Singleton
                .Observe (state => state.TimerState)
            .Subscribe (state => {
                switch (step) {
                case 0:
                    Assert.True (state.TimeEntries.ContainsKey (te.Id));
                    step++;
                    break;
                case 1:
                    Assert.False (state.TimeEntries.ContainsKey (te.Id));
                    // The entry should have also been deleted from the db
                    Assert.False (db.Table<TimeEntryData> ().Any (x => x.Id == te.Id));
                    subscription2.Dispose ();
                    break;
                }
            });

            RxChain.Send (new DataMsg.TimeEntryPut (te));

            RxChain.Send (new DataMsg.TimeEntriesRemovePermanently (
                              new List<ITimeEntryData> { te }));
        }

    }
}

