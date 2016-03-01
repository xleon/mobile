using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using Toggl.Phoebe.Tests;
using Toggl.Phoebe._Data;
using Toggl.Phoebe._Data.Json;
using Toggl.Phoebe._Data.Models;
using Toggl.Phoebe._Net;
using Toggl.Phoebe._Reactive;
using Toggl.Phoebe._ViewModels;
using XPlatUtils;
using Toggl.Phoebe.Analytics;

namespace Toggl.Phoebe.Tests.Reactive
{
    [TestFixture]
    public class NewProjectVMTest : Test
    {
        NewProjectVM viewModel;
        SyncSqliteDataStore dataStore;
        readonly ToggleClientMock togglClient = new ToggleClientMock ();

        public override void Init ()
        {
            base.Init ();

            var initState = Util.GetInitAppState ();
            var platformUtils = new PlatformUtils ();
            ServiceContainer.RegisterScoped<IPlatformUtils> (platformUtils);
            ServiceContainer.RegisterScoped<ITogglClient> (togglClient);
            ServiceContainer.RegisterScoped<ITracker> (new TrackerMock());

            RxChain.Init (initState);
            viewModel = new NewProjectVM (initState.TimerState, Util.WorkspaceId);
            dataStore = new SyncSqliteDataStore (databasePath, platformUtils.SQLiteInfo);
        }

        public override void Cleanup ()
        {
            base.Cleanup ();
            RxChain.Cleanup ();
        }

        [Test]
        public void TestSaveProject ()
        {
			var pcolor = 2;
            var pname = "MyProject";
            var tcs = Util.CreateTask<bool> ();

            RunAsync (async () => {
                viewModel.SaveProject (pname, pcolor, new SyncTestOptions (false, (state, sent, queued) => {
                    try {
                        ProjectData project = null;
                        Assert.NotNull (project = state.TimerState.Projects.Values.SingleOrDefault (
                            x => x.WorkspaceId == Util.WorkspaceId && x.Name == pname && x.Color == pcolor));

                        // Check project has been correctly saved in database
                        Assert.NotNull (dataStore.Table<ProjectData> ().SingleOrDefault (
                            x => x.WorkspaceId == Util.WorkspaceId && x.Name == pname && x.Color == pcolor && x.Id == project.Id));

                        // ProjectUserData
                        Assert.NotNull (state.TimerState.ProjectUsers.Values.SingleOrDefault (x => x.ProjectId == project.Id));
                        Assert.NotNull (dataStore.Table<ProjectUserData> ().SingleOrDefault (x => x.ProjectId == project.Id));

                        tcs.SetResult (true);
                    }
                    catch (Exception ex) {
                        tcs.SetException (ex);
                    }                        
                }));
                await tcs.Task;
            });
        }


        [Test]
        public void TestSetClient ()
        {
            var pcolor = 5;
            var pname = "MyProject2";
            var client = new ClientData {
                Id = Guid.NewGuid (),
                Name = "MyClient"
            };
            var tcs = Util.CreateTask<bool> ();

            RunAsync (async () => {
                viewModel.SetClient (client);
                viewModel.SaveProject (pname, pcolor, new SyncTestOptions (false, (state, sent, queued) => {
                    try {
                        Assert.NotNull (state.TimerState.Projects.Values.SingleOrDefault (
                            x => x.Name == pname && x.ClientId == client.Id));

                        tcs.SetResult (true);
                    }
                    catch (Exception ex) {
                        tcs.SetException (ex);
                    }                        
                }));                
                await tcs.Task;
            });
        }
    }
}

