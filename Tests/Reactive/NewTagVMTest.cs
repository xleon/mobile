using System;
using System.Linq;
using NUnit.Framework;
using Toggl.Phoebe._Data;
using Toggl.Phoebe._Data.Models;
using Toggl.Phoebe._Net;
using Toggl.Phoebe._Reactive;
using Toggl.Phoebe._ViewModels;
using XPlatUtils;
using Toggl.Phoebe.Analytics;
using System.Threading.Tasks;

namespace Toggl.Phoebe.Tests.Reactive
{
    [TestFixture]
    public class NewTagVMTest : Test
    {
        NewTagVM viewModel;
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
            viewModel = new NewTagVM (initState, Util.WorkspaceId);
            dataStore = new SyncSqliteDataStore (databasePath, platformUtils.SQLiteInfo);
        }

        public override void Cleanup ()
        {
            base.Cleanup ();
            RxChain.Cleanup ();
        }

        [Test]
        public async Task TestSaveTag ()
        {
            var name = "MyTag";
            var tcs = Util.CreateTask<bool> ();

            viewModel.SaveTag (name, new SyncTestOptions (false, (state, sent, queued) => {
                try {
                    TagData tag = null;
                    Assert.That (tag = state.Tags.Values.SingleOrDefault (
                                           x => x.WorkspaceId == Util.WorkspaceId && x.Name == name), Is.Not.Null);

                    // Check item has been correctly saved in database
                    Assert.That (dataStore.Table<TagData> ().SingleOrDefault (
                                     x => x.WorkspaceId == Util.WorkspaceId && x.Name == name && x.Id == tag.Id), Is.Not.Null);

                    tcs.SetResult (true);
                } catch (Exception ex) {
                    tcs.SetException (ex);
                }
            }));

            await tcs.Task;
        }
    }
}

