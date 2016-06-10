using System;
using System.Linq;
using NUnit.Framework;
using Toggl.Phoebe.Data;
using Toggl.Phoebe.Data.Models;
using Toggl.Phoebe.Net;
using Toggl.Phoebe.Reactive;
using Toggl.Phoebe.ViewModels;
using XPlatUtils;
using Toggl.Phoebe.Analytics;
using System.Threading.Tasks;

namespace Toggl.Phoebe.Tests.Reactive
{
    [TestFixture]
    public class NewProjectVMTest : Test
    {
        NewProjectVM viewModel;
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
            viewModel = new NewProjectVM(initState, Util.WorkspaceId);
            dataStore = new SyncSqliteDataStore(databasePath, platformUtils.SQLiteInfo);
        }

        public override void Cleanup()
        {
            base.Cleanup();
            RxChain.Cleanup();
        }

        [Test]
        public void TestSaveProject()
        {
            var pcolor = 2;
            var pname = "MyProject";
            networkSwitcher.SetNetworkConnection(false);

            IProjectData project  = viewModel.SaveProjectAsync(pname, pcolor).Result;

            Assert.That(project = StoreManager.Singleton.AppState.Projects.Values.SingleOrDefault(
                                      x => x.WorkspaceId == Util.WorkspaceId && x.Name == pname && x.Color == pcolor), Is.Not.Null);

            // Check project has been correctly saved in database
            Assert.That(dataStore.Table<ProjectData> ().SingleOrDefault(
                            x => x.WorkspaceId == Util.WorkspaceId && x.Name == pname && x.Color == pcolor && x.Id == project.Id), Is.Not.Null);
        }


        [Test]
        public void TestSetClient()
        {
            var pcolor = 5;
            var pname = "MyProject2";
            var client = ClientData.Create(x => x.Name = "MyClient");
            networkSwitcher.SetNetworkConnection(false);

            viewModel.SetClient(client);

            var projectData = viewModel.SaveProjectAsync(pname, pcolor).Result;

            Assert.That(StoreManager.Singleton.AppState.Projects.Values.SingleOrDefault(
                            x => x.Name == pname && x.ClientId == client.Id), Is.Not.Null);

        }
    }
}

