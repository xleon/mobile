using NUnit.Framework;
using Toggl.Phoebe.Data.DataObjects;
using Toggl.Phoebe.Data.Reports;

namespace Toggl.Phoebe.Tests.Views
{
    [TestFixture]
    public class SummaryReportViewTest : Test
    {
        private UserData user;
        private WorkspaceData workspace;

        public override void SetUp ()
        {
            base.SetUp ();

            RunAsync (async delegate {
                workspace = await DataStore.PutAsync (new WorkspaceData () {
                    Name = "Test",
                });

                user = await DataStore.PutAsync (new UserData () {
                    Name = "John Doe",
                    TrackingMode = TrackingMode.StartNew,
                    DefaultWorkspaceId = workspace.Id,
                });

                await SetUpFakeUser (user.Id);
            });
        }

        [Test]
        public void TestCreateWeekReport ()
        {
            RunAsync (async delegate {
                var view = new SummaryReportView ();
                view.Period = Toggl.Phoebe.Data.ZoomLevel.Week;
                await view.Load (0);
                Assert.AreEqual (false, view.IsLoading);
                Assert.AreEqual (true, view.ActivityCount == 7);
                Assert.AreEqual (true, view.Projects != null);
            });
        }

        [Test]
        public void TestCreateMonthReport ()
        {
            RunAsync (async delegate {
                var view = new SummaryReportView ();
                view.Period = Toggl.Phoebe.Data.ZoomLevel.Month;
                await view.Load (0);
                Assert.AreEqual (false, view.IsLoading);
                Assert.AreEqual (true, view.ActivityCount == 30);
                Assert.AreEqual (true, view.Projects != null);
            });
        }

        [Test]
        public void TestCreateYearReport ()
        {
            RunAsync (async delegate {
                var view = new SummaryReportView ();
                view.Period = Toggl.Phoebe.Data.ZoomLevel.Year;
                await view.Load (0);
                Assert.AreEqual (false, view.IsLoading);
                Assert.AreEqual (true, view.ActivityCount == 12);
                Assert.AreEqual (true, view.Projects != null);
            });
        }
    }
}

