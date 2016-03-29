using System;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using Toggl.Phoebe._Data;
using Toggl.Phoebe._Data.Json;
using Toggl.Phoebe._Data.Models;
using Toggl.Phoebe._Net;
using Toggl.Phoebe._Reactive;
using XPlatUtils;

namespace Toggl.Phoebe.Tests.Reactive
{
    [TestFixture]
    public class SyncManagerTest : Test
    {
        NetworkSwitcher networkSwitcher;
        readonly ToggleClientMock togglClient = new ToggleClientMock ();

        public override void Init ()
        {
            base.Init ();
            var platformUtils = new PlatformUtils ();
            ServiceContainer.RegisterScoped<IPlatformUtils> (platformUtils);
            ServiceContainer.RegisterScoped<ITogglClient> (togglClient);
            networkSwitcher = new NetworkSwitcher ();
            ServiceContainer.RegisterScoped<INetworkPresence> (networkSwitcher);
            RxChain.Init (Util.GetInitAppState ());
        }

        public override void Cleanup ()
        {
            base.Cleanup ();
            RxChain.Cleanup ();
        }

        [SetUp]
        public override void SetUp()
        {
            base.SetUp();
            togglClient.ReceivedItems.Clear ();
        }

        [Test]
        public async Task TestSendMessageWithoutConnection ()
        {
            var tcs = Util.CreateTask<bool> ();
            var te = Util.CreateTimeEntryData (DateTime.Now);

            RxChain.Send (
            new DataMsg.TimeEntryPut (te), new SyncTestOptions (false, (_, sent, queued) => {
                try {
                    // As there's no connection, message should have been enqueued
                    Assert.That (queued.Any (x => x.Data.Id == te.Id), Is.True);
                    Assert.That (0, Is.EqualTo (sent.Count));
                    tcs.SetResult (true);
                } catch (Exception ex) {
                    tcs.SetException (ex);
                }
            }));

            await tcs.Task;
        }

        [Test]
        public async Task TestSendMessageWithConnection ()
        {
            var tcs = Util.CreateTask<bool> ();
            var te = Util.CreateTimeEntryData (DateTime.Now);

            RxChain.Send (
            new DataMsg.TimeEntryPut (te), new SyncTestOptions (true, (_, sent, queued) => {
                try {
                    // As there's connection, message should have been sent
                    Assert.That (queued.Any (x => x.Data.Id == te.Id), Is.False);
                    Assert.That (1, Is.EqualTo (sent.Count));
                    tcs.SetResult (true);
                } catch (Exception ex) {
                    tcs.SetException (ex);
                }
            }));

            await tcs.Task;
        }

        [Test]
        public async Task TestTrySendMessageAndReconnect ()
        {
            var tcs = Util.CreateTask<bool> ();
            var te = Util.CreateTimeEntryData (DateTime.Now);
            var te2 = Util.CreateTimeEntryData (DateTime.Now + TimeSpan.FromMinutes (5));

            RxChain.Send (
            new DataMsg.TimeEntryPut (te), new SyncTestOptions (false, (_, sent, queued) => {
                try {
                    // As there's no connection, message should have been enqueued
                    Assert.That (queued.Any (x => x.Data.Id == te.Id), Is.True);
                    Assert.That (0, Is.EqualTo (sent.Count));
                } catch (Exception ex) {
                    tcs.SetException (ex);
                }
            }));

            RxChain.Send (
            new DataMsg.TimeEntryPut (te2), new SyncTestOptions (true, (_, sent, queued) => {
                try {
                    // As there's connection, messages should have been sent
                    Assert.That (queued.Any (x => x.Data.Id == te.Id || x.Data.Id == te2.Id), Is.False);
                    Assert.That (sent.Count > 0, Is.True);
                    tcs.SetResult (true);
                } catch (Exception ex) {
                    tcs.SetException (ex);
                }
            }));

            await tcs.Task;
        }

        [Test]
        public async Task TestCreateNewCommonData ()
        {
            // Set network as connected.
            networkSwitcher.SetNetworkConnection (true);
            var mapper = new JsonMapper ();
            var tcs = Util.CreateTask<bool> ();
            var te = Util.CreateTimeEntryData (DateTime.Now);

            RxChain.Send (new DataMsg.TimeEntryPut (te), new SyncTestOptions (true, (_, sent, queued) => {
                try {
                    var commonData = togglClient.ReceivedItems.FirstOrDefault ();
                    var remoteTe = mapper.Map<TimeEntryData> (commonData);

                    Assert.That (remoteTe.Description, Is.EqualTo (te.Description));
                    Assert.That (remoteTe.RemoteId, Is.Not.Null);

                    var dataStore = ServiceContainer.Resolve<ISyncDataStore> ();
                    // Check item has been correctly saved in database
                    Assert.That (dataStore.Table<TimeEntryData> ().SingleOrDefault (
                                     x => x.Id == te.Id), Is.Not.Null);
                    tcs.SetResult (true);
                } catch (Exception ex) {
                    tcs.SetException (ex);
                }
            }));
            await tcs.Task;
        }
    }
}
