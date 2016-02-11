using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
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
        public override void SetUp ()
        {
            base.SetUp ();

            var platformUtils = new PlatformUtils ();
            var store = new SyncSqliteDataStore (Path.GetTempFileName (), platformUtils.SQLiteInfo);

            ServiceContainer.Register<IPlatformUtils> (platformUtils);
            ServiceContainer.Register<ISyncDataStore> (store);

            RxChain.Init (Util.GetInitAppState (), RxChain.InitMode.TestStoreManager);
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

            RxChain.Send (new DataMsg.TimeEntryAdd (te));
        }

        [Test]
        public void TestRemoveEntry ()
        {
            int step = 0;
            IDisposable subscription = null;
            var te = Util.CreateTimeEntryData (DateTime.Now);

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
                    subscription.Dispose ();
                    break;
                }
            });

            RxChain.Send (new DataMsg.TimeEntryAdd (te));

            RxChain.Send (new DataMsg.TimeEntriesRemovePermanently (
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


            RxChain.Send (new DataMsg.TimeEntryAdd (te));

            // Modifying the entry now shouldn't affect the state
            te.Description = "NEW";
            var description = receivedState.TimeEntries[te.Id].Data.Description;
            Assert.AreEqual (oldDescription, description);
        }
    }
}

