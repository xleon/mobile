using System;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using Toggl.Phoebe.Data;
using Toggl.Phoebe.Data.Models;
using Toggl.Phoebe.Net;
using Toggl.Phoebe.Reactive;
using Toggl.Phoebe.ViewModels;
using XPlatUtils;
using Toggl.Phoebe.Analytics;

namespace Toggl.Phoebe.Tests.Reactive
{
    [TestFixture]
    public class NewClientVMTest : Test
    {
        NewClientVM viewModel;
        SyncSqliteDataStore dataStore;
        readonly ToggleClientMock togglClient = new ToggleClientMock();
        readonly NetworkSwitcher networkSwitcher = new NetworkSwitcher();

        public override void Init()
        {
            base.Init();

            var initState = Util.GetInitAppState();
            var platformUtils = new PlatformUtils();
            ServiceContainer.RegisterScoped<IPlatformUtils> (platformUtils);
            ServiceContainer.RegisterScoped<ITogglClient> (togglClient);
            ServiceContainer.RegisterScoped<ITracker> (new TrackerMock());
            ServiceContainer.RegisterScoped<INetworkPresence>(networkSwitcher);

            RxChain.Init(initState);
            viewModel = new NewClientVM(initState, Util.WorkspaceId);
            dataStore = new SyncSqliteDataStore(databasePath, platformUtils.SQLiteInfo);
        }

        public override void Cleanup()
        {
            base.Cleanup();
            RxChain.Cleanup();
        }

        [Test]
        public void TestSaveClient()
        {
            var name = "MyClient";
            networkSwitcher.SetNetworkConnection(false);

            IClientData client = viewModel.SaveClientAsync(name).Result;

            Assert.That(client = StoreManager.Singleton.AppState.Clients.Values.SingleOrDefault(
                                     x => x.WorkspaceId == Util.WorkspaceId && x.Name == name), Is.Not.Null);

            // Check item has been correctly saved in database
            Assert.That(dataStore.Table<ClientData> ().SingleOrDefault(
                            x => x.WorkspaceId == Util.WorkspaceId && x.Name == name && x.Id == client.Id), Is.Not.Null);
        }
    }
}

