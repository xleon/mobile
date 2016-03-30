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

            subscription = StoreManager
                           .Singleton
                           .Observe (x => x.State)
            .Subscribe (state => {
                Assert.That (state.TimeEntries.ContainsKey (te.Id), Is.True);
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

            subscription = StoreManager
                           .Singleton
                           .Observe (x => x.State)
            .Subscribe (state => {
                switch (step) {
                case 0:
                    var te2 = state.TimeEntries[te.Id];
                    Assert.That (te2.Data.State == TimeEntryState.Running, Is.True);
                    step++;
                    break;
                case 1:
                    var te3 = state.TimeEntries[te.Id];
                    Assert.That (te3.Data.State == TimeEntryState.Finished, Is.True);
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

            subscription = StoreManager
                           .Singleton
                           .Observe (x => x.State)
            .Subscribe (state => {
                switch (step) {
                case 0:
                    Assert.That (state.TimeEntries.ContainsKey (te.Id), Is.True);
                    step++;
                    break;
                case 1:
                    Assert.That (state.TimeEntries.ContainsKey (te.Id), Is.False);
                    // The entry should have also been deleted from the db
                    Assert.That (db.Table<TimeEntryData> ().Any (x => x.Id == te.Id), Is.False);
                    subscription.Dispose ();
                    break;
                }
            });

            RxChain.Send (new DataMsg.TimeEntryPut (te));
            RxChain.Send (new DataMsg.TimeEntriesRemove (te));
        }


        // TODO RX: Clone all the objects added to AppState to make this test work?
        //[Test]
        //public void TestTryModifyEntry ()
        //{
        //    const string oldDescription = "OLD";
        //    var te = Util.CreateTimeEntryData (DateTime.Now);
        //    te.Description = oldDescription;
        //    IDisposable subscription = null;

        //    subscription =
        //        StoreManager.Singleton
        //                    .Observe (x => x.State)
        //                    .Subscribe (state => {
        //                        subscription.Dispose ();
        //                        // Modifying the entry now shouldn't affect the state
        //                        te.Description = "NEW";
        //                        var description = state.TimeEntries[te.Id].Data.Description;
        //                        Assert.AreEqual (oldDescription, description);
        //                    });

        //    RxChain.Send (new DataMsg.TimeEntryPut (te));
        //}

        [Test]
        public void TestSeveralSuscriptors ()
        {
            int step = 0;
            IDisposable subscription1 = null, subscription2 = null;
            var te = Util.CreateTimeEntryData (DateTime.Now);
            var db = ServiceContainer.Resolve<ISyncDataStore> ();

            subscription1 = StoreManager
                            .Singleton
                            .Observe (x => x.State)
            .Subscribe (state => {
                Assert.That (state.TimeEntries.ContainsKey (te.Id), Is.True);
                subscription1.Dispose ();
            });

            subscription2 = StoreManager
                            .Singleton
                            .Observe (x => x.State)
            .Subscribe (state => {
                switch (step) {
                case 0:
                    Assert.That (state.TimeEntries.ContainsKey (te.Id), Is.True);
                    step++;
                    break;
                case 1:
                    Assert.That (state.TimeEntries.ContainsKey (te.Id), Is.False);
                    // The entry should have also been deleted from the db
                    Assert.That (db.Table<TimeEntryData> ().Any (x => x.Id == te.Id), Is.False);
                    subscription2.Dispose ();
                    break;
                }
            });

            RxChain.Send (new DataMsg.TimeEntryPut (te));
            RxChain.Send (new DataMsg.TimeEntriesRemove ( te ));
        }

    }
}

