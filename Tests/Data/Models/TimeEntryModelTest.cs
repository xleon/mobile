using System;
using Moq;
using NUnit.Framework;
using Toggl.Phoebe.Data;
using Toggl.Phoebe.Data.NewModels;
using Toggl.Phoebe.Net;
using XPlatUtils;
using Toggl.Phoebe.Data.DataObjects;

namespace Toggl.Phoebe.Tests.Data.Models
{
    [TestFixture]
    public class TimeEntryModelTest : ModelTest<TimeEntryModel>
    {
        private UserModel user;

        public override void SetUp ()
        {
            base.SetUp ();

            user = new UserModel () {
                Name = "User",
                Email = "user@test.net",
                DefaultWorkspace = new WorkspaceModel () {
                    Name = "Workspace",
                },
            };

            RunAsync (async delegate {
                await user.DefaultWorkspace.SaveAsync ();
                await user.SaveAsync ();
            });

            // ServiceContainer.Register<ISettingsStore> (Mock.Of<ISettingsStore> (
            //     (store) => store.ApiToken == "test" &&
            //     store.UserId == user.Id));
            // ServiceContainer.Register<AuthManager> (new AuthManager ());
        }

        [Test]
        public void TestStart ()
        {
            RunAsync (async delegate {
                var entry = new TimeEntryModel () {
                    User = user,
                };

                await entry.StartAsync ();
                Assert.IsNull (entry.Task);
                Assert.IsNull (entry.Project);
                Assert.AreEqual (user.DefaultWorkspace.Id, entry.Workspace.Id);
                Assert.AreEqual (TimeEntryState.Running, entry.State);
                Assert.AreNotEqual (new DateTime (), entry.StartTime);
                Assert.IsNull (entry.StopTime);
            });
        }

        [Test]
        public void TestStore ()
        {
            RunAsync (async delegate {
                var entry = new TimeEntryModel () {
                    User = user,
                    StartTime = new DateTime (2013, 1, 2, 3, 4, 5, DateTimeKind.Utc),
                    StopTime = new DateTime (2013, 1, 2, 5, 4, 3, DateTimeKind.Utc),
                };

                await entry.StoreAsync ();
                Assert.IsNull (entry.Task);
                Assert.IsNull (entry.Project);
                Assert.AreEqual (user.DefaultWorkspace.Id, entry.Workspace.Id);
                Assert.AreEqual (TimeEntryState.Finished, entry.State);
                Assert.AreEqual (new DateTime (2013, 1, 2, 3, 4, 5, DateTimeKind.Utc), entry.StartTime);
                Assert.AreEqual (new DateTime (2013, 1, 2, 5, 4, 3, DateTimeKind.Utc), entry.StopTime);
            });
        }

        [Test]
        public void TestContinue ()
        {
            throw new NotImplementedException ();
        }

        [Test]
        public void TestGetDraft ()
        {
            throw new NotImplementedException ();
        }

        [Test]
        public void TestCreateFinished ()
        {
            throw new NotImplementedException ();
        }

        [Test]
        public void TestIsBillableFlag ()
        {
            var entry = new TimeEntryModel () {
                User = user,
                IsBillable = false,
            };

            Assert.IsFalse (entry.IsBillable);

            entry.Project = new ProjectModel (new ProjectData () {
                Id = Guid.NewGuid (),
                IsBillable = true,
            });
            Assert.IsTrue (entry.IsBillable);

            entry.Project = null;
            Assert.IsTrue (entry.IsBillable);

            entry.Project = new ProjectModel (new ProjectData () {
                Id = Guid.NewGuid (),
                IsBillable = false,
            });
            Assert.IsFalse (entry.IsBillable);
        }

        [Test]
        public void TestNewStartChange ()
        {
            var entry = new TimeEntryModel (new TimeEntryData () {
                State = TimeEntryState.New,
                UserId = user.Id,
                WorkspaceId = user.DefaultWorkspace.Id,
                StartTime = new DateTime (2013, 10, 1, 10, 12, 30, DateTimeKind.Utc),
                StopTime = new DateTime (2013, 10, 1, 13, 12, 30, DateTimeKind.Utc),
            });

            entry.StartTime = new DateTime (2013, 10, 1, 11, 12, 30, DateTimeKind.Utc);
            Assert.AreEqual (new DateTime (2013, 10, 1, 14, 12, 30, DateTimeKind.Utc), entry.StopTime);
        }

        [Test]
        public void TestNewStartChangeNoStop ()
        {
            var entry = new TimeEntryModel (new TimeEntryData () {
                State = TimeEntryState.New,
                UserId = user.Id,
                WorkspaceId = user.DefaultWorkspace.Id,
            });

            entry.StartTime = Time.UtcNow.AddHours (-1);
            Assert.AreEqual (entry.StartTime.AddHours (1), entry.StopTime);
        }

        [Test]
        public void TestNewStartChange3DaysAgoNoStop ()
        {
            var entry = new TimeEntryModel (new TimeEntryData () {
                State = TimeEntryState.New,
                UserId = user.Id,
                WorkspaceId = user.DefaultWorkspace.Id,
            });

            entry.StartTime = Time.UtcNow.AddDays (-3).AddHours (-1);
            Assert.AreEqual (entry.StartTime.AddHours (1), entry.StopTime);
        }

        [Test]
        public void TestNewFutureStartChangeNoStop ()
        {
            var entry = new TimeEntryModel (new TimeEntryData () {
                State = TimeEntryState.New,
                UserId = user.Id,
                WorkspaceId = user.DefaultWorkspace.Id,
            });

            entry.StartTime = Time.UtcNow.AddHours (1);
            Assert.AreEqual (entry.StartTime, entry.StopTime);
        }

        [Test]
        public void TestNewStopChange ()
        {
            var entry = new TimeEntryModel (new TimeEntryData () {
                State = TimeEntryState.New,
                UserId = user.Id,
                WorkspaceId = user.DefaultWorkspace.Id,
                StartTime = new DateTime (2013, 10, 1, 10, 12, 30, DateTimeKind.Utc),
                StopTime = new DateTime (2013, 10, 1, 13, 12, 30, DateTimeKind.Utc),
            });

            entry.StopTime = new DateTime (2013, 10, 1, 11, 12, 30, DateTimeKind.Utc);
            Assert.AreEqual (new DateTime (2013, 10, 1, 10, 12, 30, DateTimeKind.Utc), entry.StartTime);
        }

        [Test]
        public void TestNewDurationChange ()
        {
            var entry = new TimeEntryModel (new TimeEntryData () {
                State = TimeEntryState.New,
                UserId = user.Id,
                WorkspaceId = user.DefaultWorkspace.Id,
                StartTime = new DateTime (2013, 10, 1, 10, 12, 30, DateTimeKind.Utc),
                StopTime = new DateTime (2013, 10, 1, 13, 12, 30, DateTimeKind.Utc),
            });

            entry.SetDuration (TimeSpan.FromHours (1));
            Assert.AreEqual (new DateTime (2013, 10, 1, 12, 12, 30, DateTimeKind.Utc), entry.StartTime);
            Assert.AreEqual (new DateTime (2013, 10, 1, 13, 12, 30, DateTimeKind.Utc), entry.StopTime);
        }

        [Test]
        public void TestRunningStartChange ()
        {
            var entry = new TimeEntryModel (new TimeEntryData () {
                State = TimeEntryState.Running,
                UserId = user.Id,
                WorkspaceId = user.DefaultWorkspace.Id,
                StartTime = new DateTime (2013, 10, 1, 10, 12, 30, DateTimeKind.Utc),
            });
            var oldDuration = (long)entry.GetDuration ().TotalSeconds;
            Assert.AreNotEqual (0, oldDuration);

            entry.StartTime = new DateTime (2013, 10, 1, 12, 12, 30, DateTimeKind.Utc);
            var newDuration = (long)entry.GetDuration ().TotalSeconds;
            Assert.AreEqual ((long)TimeSpan.FromHours (2).TotalSeconds, oldDuration - newDuration);
            Assert.AreEqual (null, entry.StopTime);
        }

        [Test]
        public void TestRunningDurationChange ()
        {
            var entry = new TimeEntryModel (new TimeEntryData () {
                State = TimeEntryState.Running,
                UserId = user.Id,
                WorkspaceId = user.DefaultWorkspace.Id,
                StartTime = new DateTime (2013, 10, 1, 10, 12, 30, DateTimeKind.Utc),
            });
            var oldDuration = entry.GetDuration ();
            Assert.AreNotEqual (0, oldDuration);

            entry.SetDuration (oldDuration + TimeSpan.FromHours (1));
            Assert.AreEqual (new DateTime (2013, 10, 1, 9, 12, 30, DateTimeKind.Utc), entry.StartTime.Truncate (TimeSpan.TicksPerSecond));
            Assert.AreEqual (null, entry.StopTime);
        }

        [Test]
        public void TestStoppedStartChange ()
        {
            var entry = new TimeEntryModel (new TimeEntryData () {
                State = TimeEntryState.Finished,
                UserId = user.Id,
                WorkspaceId = user.DefaultWorkspace.Id,
                StartTime = new DateTime (2013, 10, 1, 10, 12, 30, DateTimeKind.Utc),
                StopTime = new DateTime (2013, 10, 1, 13, 12, 30, DateTimeKind.Utc),
            });

            // Changing start time should keep the duration constant and adjust stop time
            entry.StartTime = new DateTime (2013, 10, 1, 11, 12, 30, DateTimeKind.Utc);
            Assert.AreEqual (new DateTime (2013, 10, 1, 14, 12, 30, DateTimeKind.Utc), entry.StopTime);
        }

        [Test]
        public void TestStoppedStopChange ()
        {
            var entry = new TimeEntryModel (new TimeEntryData () {
                State = TimeEntryState.Finished,
                UserId = user.Id,
                WorkspaceId = user.DefaultWorkspace.Id,
                StartTime = new DateTime (2013, 10, 1, 10, 12, 30, DateTimeKind.Utc),
                StopTime = new DateTime (2013, 10, 1, 13, 12, 30, DateTimeKind.Utc),
            });

            entry.StopTime = new DateTime (2013, 10, 1, 11, 12, 30, DateTimeKind.Utc);
            Assert.AreEqual (new DateTime (2013, 10, 1, 10, 12, 30, DateTimeKind.Utc), entry.StartTime);
        }

        [Test]
        public void TestStoppedDurationChange ()
        {
            var entry = new TimeEntryModel (new TimeEntryData () {
                State = TimeEntryState.Finished,
                UserId = user.Id,
                WorkspaceId = user.DefaultWorkspace.Id,
                StartTime = new DateTime (2013, 10, 1, 10, 12, 30, DateTimeKind.Utc),
                StopTime = new DateTime (2013, 10, 1, 13, 12, 30, DateTimeKind.Utc),
            });

            entry.SetDuration (TimeSpan.FromHours (1));
            Assert.AreEqual (new DateTime (2013, 10, 1, 10, 12, 30, DateTimeKind.Utc), entry.StartTime);
            Assert.AreEqual (new DateTime (2013, 10, 1, 11, 12, 30, DateTimeKind.Utc), entry.StopTime);
        }

        [Test]
        public void TestDuronlyNewDurationChange ()
        {
            var entry = new TimeEntryModel (new TimeEntryData () {
                State = TimeEntryState.New,
                UserId = user.Id,
                WorkspaceId = user.DefaultWorkspace.Id,
                StartTime = new DateTime (2013, 10, 1).ToUtc (),
                DurationOnly = true,
            });
            Assert.IsNull (entry.StopTime);

            entry.SetDuration (TimeSpan.FromHours (1));
            Assert.IsNotNull (entry.StopTime);
            Assert.AreNotEqual (new DateTime (2013, 10, 1).ToUtc (), entry.StartTime);
        }

        [Test]
        public void TestDuronlyRunningDurationChange ()
        {
            var entry = new TimeEntryModel (new TimeEntryData () {
                State = TimeEntryState.Running,
                UserId = user.Id,
                WorkspaceId = user.DefaultWorkspace.Id,
                StartTime = new DateTime (2013, 10, 1).ToUtc (),
                DurationOnly = true,
            });
            Assert.IsNull (entry.StopTime);

            entry.SetDuration (TimeSpan.FromHours (1));
            Assert.IsNull (entry.StopTime);
            Assert.AreNotEqual (new DateTime (2013, 10, 1).ToUtc (), entry.StartTime);
        }

        [Test]
        public void TestDuronlyStoppedDurationChange ()
        {
            var entry = new TimeEntryModel (new TimeEntryData () {
                State = TimeEntryState.Finished,
                UserId = user.Id,
                WorkspaceId = user.DefaultWorkspace.Id,
                StartTime = new DateTime (2013, 10, 1, 10, 12, 0, DateTimeKind.Utc),
                StopTime = new DateTime (2013, 10, 1, 12, 12, 0, DateTimeKind.Utc),
                DurationOnly = true,
            });
            Assert.AreEqual (TimeSpan.FromHours (2), entry.GetDuration ());

            entry.SetDuration (TimeSpan.FromHours (1));
            Assert.AreEqual (new DateTime (2013, 10, 1, 10, 12, 0, DateTimeKind.Utc), entry.StartTime);
            Assert.AreEqual (new DateTime (2013, 10, 1, 11, 12, 0, DateTimeKind.Utc), entry.StopTime);
        }
    }
}
