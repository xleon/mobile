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
                        var project = state.TimerState.Projects.Values.Single (
                            x => x.WorkspaceId == Util.WorkspaceId && x.Name == pname && x.Color == pcolor);
                        Assert.NotNull (project);
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

