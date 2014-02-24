using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using Toggl.Phoebe.Data;
using Toggl.Phoebe.Data.Models;
using XPlatUtils;
using Toggl.Phoebe.Net;
using Moq;

namespace Toggl.Phoebe.Tests.Data
{
    [TestFixture]
    public class TimeEntryModelTest: Test
    {
        private string tmpDb;

        private IModelStore ModelStore {
            get { return ServiceContainer.Resolve<IModelStore> (); }
        }

        private AuthManager AuthManager {
            get { return ServiceContainer.Resolve<AuthManager> (); }
        }

        public override void SetUp ()
        {
            base.SetUp ();

            tmpDb = Path.GetTempFileName ();
            ServiceContainer.Register<IModelStore> (new TestSqliteStore (tmpDb));

            var user = Model.Update (new UserModel () {
                Name = "User",
                Email = "user@test.net",
                DefaultWorkspace = Model.Update (new WorkspaceModel () {
                    Name = "Workspace",
                }),
            });
            ServiceContainer.Register<ISettingsStore> (Mock.Of<ISettingsStore> (
                (store) => store.ApiToken == "test" &&
                store.UserId == user.Id));
            ServiceContainer.Register<AuthManager> (new AuthManager ());
        }

        public override void TearDown ()
        {
            ModelStore.Commit ();

            base.TearDown ();

            File.Delete (tmpDb);
            tmpDb = null;
        }

        [Test]
        public void TestStopOthersWhenStartNew ()
        {
            var oldTimeEntry = TimeEntryModel.StartNew ();
            Assert.AreEqual (TimeEntryState.Running, oldTimeEntry.State);
            Assert.IsNull (oldTimeEntry.StopTime);

            var newTimeEntry = TimeEntryModel.StartNew ();
            Assert.AreEqual (TimeEntryState.Running, newTimeEntry.State);
            Assert.IsNull (newTimeEntry.StopTime);
            Assert.AreEqual (TimeEntryState.Finished, oldTimeEntry.State, "Old entry hasn't been finished.");
            Assert.IsNotNull (oldTimeEntry.StopTime, "Stop time not set for old entry.");
        }

        [Test]
        public void TestStopOthersWhenMerging ()
        {
            var oldTimeEntry = Model.Update (new TimeEntryModel () {
                User = AuthManager.User,
                State = TimeEntryState.Finished,
                StartTime = DateTime.UtcNow,
                StopTime = DateTime.UtcNow + TimeSpan.FromSeconds (60),
                DurationOnly = false,
                IsPersisted = true,
                ModifiedAt = new DateTime (),
            });

            var newTimeEntry = TimeEntryModel.StartNew ();
            Assert.AreEqual (TimeEntryState.Running, newTimeEntry.State);
            Assert.IsNull (newTimeEntry.StopTime);

            // Simulate merge from server
            Model.Update (new TimeEntryModel () {
                Id = oldTimeEntry.Id,
                Description = "Old time entry",
                User = oldTimeEntry.User,
                State = TimeEntryState.Running,
                StartTime = oldTimeEntry.StartTime,
                DurationOnly = oldTimeEntry.DurationOnly,
                IsPersisted = oldTimeEntry.IsPersisted,
            });

            Assert.AreEqual (TimeEntryState.Running, oldTimeEntry.State);
            Assert.IsNull (oldTimeEntry.StopTime);
            Assert.AreEqual (TimeEntryState.Finished, newTimeEntry.State, "New entry not finished.");
            Assert.IsNotNull (newTimeEntry.StopTime, "Stop time not set for new entry.");
        }

        [Test]
        public void TestStopOthersWhenContinueNew ()
        {
            var oldTimeEntry = Model.Update (new TimeEntryModel () {
                Description = "Old time entry",
                User = AuthManager.User,
                State = TimeEntryState.Finished,
                StartTime = DateTime.UtcNow,
                StopTime = DateTime.UtcNow + TimeSpan.FromSeconds (60),
                DurationOnly = false,
                IsPersisted = true,
            });
            var runningTimeEntry = TimeEntryModel.StartNew ();
            Assert.AreEqual (TimeEntryState.Running, runningTimeEntry.State);
            Assert.IsNull (runningTimeEntry.StopTime);

            var newTimeEntry = oldTimeEntry.Continue ();
            Assert.AreNotSame (oldTimeEntry, newTimeEntry);
            Assert.AreEqual (TimeEntryState.Running, newTimeEntry.State);
            Assert.IsNull (newTimeEntry.StopTime);
            Assert.AreEqual (TimeEntryState.Finished, runningTimeEntry.State, "Old entry wasn't stopped.");
            Assert.IsNotNull (runningTimeEntry.StopTime, "Stop time not set for old entry.");
        }

        [Test]
        public void TestStopOthersWhenContinueSame ()
        {
            var oldTimeEntry = Model.Update (new TimeEntryModel () {
                Description = "Old time entry",
                User = AuthManager.User,
                State = TimeEntryState.Finished,
                StartTime = DateTime.UtcNow,
                StopTime = DateTime.UtcNow + TimeSpan.FromSeconds (60),
                DurationOnly = true,
                IsPersisted = true,
            });
            var runningTimeEntry = TimeEntryModel.StartNew ();
            Assert.AreEqual (TimeEntryState.Running, runningTimeEntry.State);
            Assert.IsNull (runningTimeEntry.StopTime);

            var newTimeEntry = oldTimeEntry.Continue ();
            Assert.AreSame (oldTimeEntry, newTimeEntry);
            Assert.AreEqual (TimeEntryState.Running, newTimeEntry.State);
            Assert.IsNull (newTimeEntry.StopTime);
            Assert.AreEqual (TimeEntryState.Finished, runningTimeEntry.State, "Old entry not finished.");
            Assert.IsNotNull (runningTimeEntry.StopTime, "Stop time not set for old entry.");
        }

        [Test]
        public void TestDeserialisationPropertyOrder ()
        {
            var type = typeof(TimeEntryModel);
            var properties = type.GetProperties ()
                .Where ((prop) => prop.CanRead && prop.CanWrite
                             && prop.GetMethod.IsPublic && prop.SetMethod.IsPublic
                             && !prop.CustomAttributes.Any ((attr) => attr.AttributeType == typeof(SQLite.IgnoreAttribute)));

            // Various orders how to restore the properties
            var combinations = new List<List<System.Reflection.PropertyInfo>> () {
                properties.ToList (),
                properties.Reverse ().ToList (),
                properties.OrderBy ((prop) => prop.PropertyType.Name).ToList (),
            };

            // Test several configurations of models to "deserialise"
            var models = new TimeEntryModel[] {
                new TimeEntryModel () {
                    Id = Guid.NewGuid (),
                    State = TimeEntryState.New,
                    StartTime = new DateTime (2013, 10, 1, 10, 12, 30, DateTimeKind.Utc),
                    StopTime = new DateTime (2013, 10, 1, 13, 12, 30, DateTimeKind.Utc),
                    Description = "Test #2",
                },
                new TimeEntryModel () {
                    Id = Guid.NewGuid (),
                    RemoteId = 123,
                    State = TimeEntryState.Running,
                    StartTime = new DateTime (2013, 10, 1, 10, 12, 30, DateTimeKind.Utc),
                    Description = "Test #1",
                },
                new TimeEntryModel () {
                    Id = Guid.NewGuid (),
                    State = TimeEntryState.Finished,
                    StartTime = new DateTime (2013, 10, 1, 10, 12, 30, DateTimeKind.Utc),
                    StopTime = new DateTime (2013, 10, 1, 13, 12, 30, DateTimeKind.Utc),
                    Description = "Test #3",
                },
            };

            foreach (var model in models) {
                foreach (var props in combinations) {
                    // Simulate deserializing
                    var newModel = new TimeEntryModel ();
                    foreach (var propInfo in props) {
                        propInfo.SetValue (newModel, propInfo.GetValue (model));
                    }

                    // Test if all values are exactly the same as before
                    foreach (var propInfo in props) {
                        Assert.AreEqual (propInfo.GetValue (model), propInfo.GetValue (newModel), String.Format ("Property {0} is invalid.", propInfo.Name));
                    }
                }
            }
        }

        [Test]
        public void TestNewStartChange ()
        {
            var entry = Model.Update (new TimeEntryModel () {
                State = TimeEntryState.New,
                StartTime = new DateTime (2013, 10, 1, 10, 12, 30, DateTimeKind.Utc),
                StopTime = new DateTime (2013, 10, 1, 13, 12, 30, DateTimeKind.Utc),
            });

            entry.StartTime = new DateTime (2013, 10, 1, 11, 12, 30, DateTimeKind.Utc);
            Assert.AreEqual (new DateTime (2013, 10, 1, 13, 12, 30, DateTimeKind.Utc), entry.StopTime);
        }

        [Test]
        public void TestNewStopChange ()
        {
            var entry = Model.Update (new TimeEntryModel () {
                State = TimeEntryState.New,
                StartTime = new DateTime (2013, 10, 1, 10, 12, 30, DateTimeKind.Utc),
                StopTime = new DateTime (2013, 10, 1, 13, 12, 30, DateTimeKind.Utc),
            });

            entry.StopTime = new DateTime (2013, 10, 1, 11, 12, 30, DateTimeKind.Utc);
            Assert.AreEqual (new DateTime (2013, 10, 1, 10, 12, 30, DateTimeKind.Utc), entry.StartTime);
        }

        [Test]
        public void TestNewDurationChange ()
        {
            var entry = Model.Update (new TimeEntryModel () {
                State = TimeEntryState.New,
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
            var entry = Model.Update (new TimeEntryModel () {
                State = TimeEntryState.Running,
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
            var entry = Model.Update (new TimeEntryModel () {
                State = TimeEntryState.Running,
                StartTime = new DateTime (2013, 10, 1, 10, 12, 30, DateTimeKind.Utc),
            });
            var oldDuration = entry.GetDuration ();
            Assert.AreNotEqual (0, oldDuration);

            entry.SetDuration (oldDuration + TimeSpan.FromHours (1));
            Assert.AreEqual (new DateTime (2013, 10, 1, 9, 12, 30, DateTimeKind.Utc), entry.StartTime);
            Assert.AreEqual (null, entry.StopTime);
        }

        [Test]
        public void TestStoppedStartChange ()
        {
            var entry = Model.Update (new TimeEntryModel () {
                State = TimeEntryState.Finished,
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
            var entry = Model.Update (new TimeEntryModel () {
                State = TimeEntryState.Finished,
                StartTime = new DateTime (2013, 10, 1, 10, 12, 30, DateTimeKind.Utc),
                StopTime = new DateTime (2013, 10, 1, 13, 12, 30, DateTimeKind.Utc),
            });

            entry.StopTime = new DateTime (2013, 10, 1, 11, 12, 30, DateTimeKind.Utc);
            Assert.AreEqual (new DateTime (2013, 10, 1, 10, 12, 30, DateTimeKind.Utc), entry.StartTime);
        }

        [Test]
        public void TestStoppedDurationChange ()
        {
            var entry = Model.Update (new TimeEntryModel () {
                State = TimeEntryState.Finished,
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
            var entry = Model.Update (new TimeEntryModel () {
                State = TimeEntryState.New,
                StartTime = new DateTime (2013, 10, 1).ToUtc (),
                DurationOnly = true,
            });
            Assert.IsNull (entry.StopTime);

            entry.SetDuration (TimeSpan.FromHours (1));
            Assert.IsNull (entry.StopTime);
            Assert.AreNotEqual (new DateTime (2013, 10, 1).ToUtc (), entry.StartTime);
        }

        [Test]
        public void TestDuronlyRunningDurationChange ()
        {
            var entry = Model.Update (new TimeEntryModel () {
                State = TimeEntryState.Running,
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
            var entry = Model.Update (new TimeEntryModel () {
                State = TimeEntryState.Finished,
                StartTime = new DateTime (2013, 10, 1, 10, 12, 0, DateTimeKind.Utc),
                StopTime = new DateTime (2013, 10, 1, 12, 12, 0, DateTimeKind.Utc),
                DurationOnly = true,
            });
            Assert.AreEqual (TimeSpan.FromHours (2), entry.GetDuration ());

            entry.SetDuration (TimeSpan.FromHours (1));
            Assert.AreEqual (new DateTime (2013, 10, 1, 10, 12, 0, DateTimeKind.Utc), entry.StartTime);
            Assert.AreEqual (new DateTime (2013, 10, 1, 11, 12, 0, DateTimeKind.Utc), entry.StopTime);
        }

        private class TestSqliteStore : SQLiteModelStore
        {
            public TestSqliteStore (string path) : base (path)
            {
            }

            protected override void ScheduleCommit ()
            {
                // Only manual commits
            }
        }
    }
}
