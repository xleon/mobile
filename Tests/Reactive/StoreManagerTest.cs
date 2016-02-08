using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using Toggl.Phoebe._Data;
using Toggl.Phoebe._Reactive;
using XPlatUtils;
using Toggl.Phoebe._Data.Models;
using Toggl.Phoebe._ViewModels.Timer;

namespace Toggl.Phoebe.Tests.Reactive
{
    [TestFixture]
    public class StoreManagerTest : Test
    {
        Type sender;
        ISyncDataStore store;

        public override void SetUp ()
        {
            base.SetUp ();

            sender = this.GetType ();
            var platformUtils = new PlatformUtils ();
            store = new SyncSqliteDataStore (Path.GetTempFileName (), platformUtils.SQLiteInfo);

            ServiceContainer.Register<IPlatformUtils> (platformUtils);
            ServiceContainer.Register<ISchedulerProvider> (new TestSchedulerProvider ());
            ServiceContainer.Register<ISyncDataStore> (store);
            
            RxChain.Init (RxChain.InitMode.TestStoreManager);
        }

        TimeEntryMsg msg(TimeEntryData teData)
        {
            var action = teData.DeletedAt != null
                ? DataAction.Delete : DataAction.Put;
            return new TimeEntryMsg (DataDir.Outcoming, action, teData);
        }

        [Test]
        public void TestAddEntry()
        {
            var oldCount = store.Table<TimeEntryData> ().Count ();
            var te = Util.CreateTimeEntryData (DateTime.Now);
            te.State = TimeEntryState.Running;

            RxChain.Send (sender, DataTag.TimeEntryAdd, msg(te));

            var newCount = store.Table<TimeEntryData> ().Count ();
            Assert.AreEqual (oldCount + 1, newCount);
        }

        [Test]
        public void TestRemoveEntry()
        {
            var oldCount = store.Table<TimeEntryData> ().Count ();
            var te = Util.CreateTimeEntryData (DateTime.Now);
            te.State = TimeEntryState.Running;

            RxChain.Send (sender, DataTag.TimeEntryAdd, msg(te));

            var newCount = store.Table<TimeEntryData> ().Count ();
            Assert.AreEqual (oldCount + 1, newCount);

            RxChain.Send (sender, DataTag.TimeEntryRemove, te);

            var newCount2 = store.Table<TimeEntryData> ().Count ();
            Assert.AreEqual (oldCount, newCount2);
        }

        [Test]
        public void TestStopEntry()
        {
            var oldCount = store.Table<TimeEntryData> ().Count ();
            var te = Util.CreateTimeEntryData (DateTime.Now);
            te.State = TimeEntryState.Running;

            RxChain.Send (sender, DataTag.TimeEntryAdd, te);
            var newCount = store.Table<TimeEntryData> ().Count ();
            Assert.AreEqual (oldCount + 1, newCount);

            RxChain.Send (sender, DataTag.TimeEntryStop, te);
            var newTe = store
                .Table<TimeEntryData> ()
                .Single (x => x.Id == te.Id);
            Assert.AreEqual (TimeEntryState.Finished, newTe.State);
        }

    }
}

