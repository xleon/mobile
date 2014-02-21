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
            Assert.IsTrue (oldTimeEntry.IsRunning);
            Assert.IsNull (oldTimeEntry.StopTime);

            var newTimeEntry = TimeEntryModel.StartNew ();
            Assert.IsTrue (newTimeEntry.IsRunning);
            Assert.IsNull (newTimeEntry.StopTime);
            Assert.IsFalse (oldTimeEntry.IsRunning, "Old entry still running.");
            Assert.IsNotNull (oldTimeEntry.StopTime, "Stop time not set for old entry.");
        }

        [Test]
        public void TestStopOthersWhenMerging ()
        {
            var oldTimeEntry = Model.Update (new TimeEntryModel () {
                User = AuthManager.User,
                StartTime = DateTime.UtcNow,
                Duration = 60,
                DurationOnly = false,
                IsPersisted = true,
                ModifiedAt = new DateTime (),
            });

            var newTimeEntry = TimeEntryModel.StartNew ();
            Assert.IsTrue (newTimeEntry.IsRunning);
            Assert.IsNull (newTimeEntry.StopTime);

            // Simulate merge from server
            Model.Update (new TimeEntryModel () {
                Id = oldTimeEntry.Id,
                Description = "Old time entry",
                User = oldTimeEntry.User,
                StartTime = oldTimeEntry.StartTime,
                Duration = oldTimeEntry.Duration,
                DurationOnly = oldTimeEntry.DurationOnly,
                IsRunning = true,
                IsPersisted = oldTimeEntry.IsPersisted,
            });

            Assert.IsTrue (oldTimeEntry.IsRunning);
            Assert.IsNull (oldTimeEntry.StopTime);
            Assert.IsFalse (newTimeEntry.IsRunning, "New entry still running.");
            Assert.IsNotNull (newTimeEntry.StopTime, "Stop time not set for new entry.");
        }

        [Test]
        public void TestStopOthersWhenContinueNew ()
        {
            var oldTimeEntry = Model.Update (new TimeEntryModel () {
                Description = "Old time entry",
                User = AuthManager.User,
                StartTime = DateTime.UtcNow,
                Duration = 60,
                DurationOnly = false,
                IsPersisted = true,
            });
            var runningTimeEntry = TimeEntryModel.StartNew ();
            Assert.IsTrue (runningTimeEntry.IsRunning);
            Assert.IsNull (runningTimeEntry.StopTime);

            var newTimeEntry = oldTimeEntry.Continue ();
            Assert.AreNotSame (oldTimeEntry, newTimeEntry);
            Assert.IsTrue (newTimeEntry.IsRunning);
            Assert.IsNull (newTimeEntry.StopTime);
            Assert.IsFalse (runningTimeEntry.IsRunning, "Old entry still running.");
            Assert.IsNotNull (runningTimeEntry.StopTime, "Stop time not set for old entry.");
        }

        [Test]
        public void TestStopOthersWhenContinueSame ()
        {
            var oldTimeEntry = Model.Update (new TimeEntryModel () {
                Description = "Old time entry",
                User = AuthManager.User,
                StartTime = DateTime.UtcNow,
                Duration = 60,
                DurationOnly = true,
                IsPersisted = true,
            });
            var runningTimeEntry = TimeEntryModel.StartNew ();
            Assert.IsTrue (runningTimeEntry.IsRunning);
            Assert.IsNull (runningTimeEntry.StopTime);

            var newTimeEntry = oldTimeEntry.Continue ();
            Assert.AreSame (oldTimeEntry, newTimeEntry);
            Assert.IsTrue (newTimeEntry.IsRunning);
            Assert.IsNull (newTimeEntry.StopTime);
            Assert.IsFalse (runningTimeEntry.IsRunning, "Old entry still running.");
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
                    RemoteId = 123,
                    StartTime = new DateTime (2013, 10, 1, 10, 12, 30, DateTimeKind.Utc),
                    IsRunning = true,
                    Duration = 1000,
                    Description = "Test #1",
                },
                new TimeEntryModel () {
                    Id = Guid.NewGuid (),
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
                        Assert.AreEqual (propInfo.GetValue (model), propInfo.GetValue (newModel));
                    }
                }
            }
        }

        [Test]
        public void TestNewStartChange ()
        {
            var entry = Model.Update (new TimeEntryModel () {
                StartTime = new DateTime (2013, 10, 1, 10, 12, 30, DateTimeKind.Utc),
                StopTime = new DateTime (2013, 10, 1, 13, 12, 30, DateTimeKind.Utc),
            });
            Assert.AreEqual (TimeSpan.FromHours (3), TimeSpan.FromSeconds (entry.Duration));

            entry.StartTime = new DateTime (2013, 10, 1, 11, 12, 30, DateTimeKind.Utc);
            Assert.AreEqual (TimeSpan.FromHours (2), TimeSpan.FromSeconds (entry.Duration));
            Assert.AreEqual (new DateTime (2013, 10, 1, 13, 12, 30, DateTimeKind.Utc), entry.StopTime);
        }

        [Test]
        public void TestNewStopChange ()
        {
            var entry = Model.Update (new TimeEntryModel () {
                StartTime = new DateTime (2013, 10, 1, 10, 12, 30, DateTimeKind.Utc),
                StopTime = new DateTime (2013, 10, 1, 13, 12, 30, DateTimeKind.Utc),
            });
            Assert.AreEqual (TimeSpan.FromHours (3), TimeSpan.FromSeconds (entry.Duration));

            entry.StopTime = new DateTime (2013, 10, 1, 11, 12, 30, DateTimeKind.Utc);
            Assert.AreEqual (TimeSpan.FromHours (1), TimeSpan.FromSeconds (entry.Duration));
            Assert.AreEqual (new DateTime (2013, 10, 1, 10, 12, 30, DateTimeKind.Utc), entry.StartTime);
        }

        [Test]
        public void TestNewDurationChange ()
        {
            var entry = Model.Update (new TimeEntryModel () {
                StartTime = new DateTime (2013, 10, 1, 10, 12, 30, DateTimeKind.Utc),
                StopTime = new DateTime (2013, 10, 1, 13, 12, 30, DateTimeKind.Utc),
            });
            Assert.AreEqual (TimeSpan.FromHours (3), TimeSpan.FromSeconds (entry.Duration));

            entry.Duration = (long)TimeSpan.FromHours (1).TotalSeconds;
            Assert.AreEqual (new DateTime (2013, 10, 1, 12, 12, 30, DateTimeKind.Utc), entry.StartTime);
            Assert.AreEqual (new DateTime (2013, 10, 1, 13, 12, 30, DateTimeKind.Utc), entry.StopTime);
        }

        [Test]
        public void TestRunningStartChange ()
        {
            var entry = Model.Update (new TimeEntryModel () {
                StartTime = new DateTime (2013, 10, 1, 10, 12, 30, DateTimeKind.Utc),
                IsRunning = true,
                IsPersisted = true,
            });
            var oldDuration = entry.Duration;
            Assert.AreNotEqual (0, oldDuration);

            entry.StartTime = new DateTime (2013, 10, 1, 11, 12, 30, DateTimeKind.Utc);
            Assert.AreEqual (TimeSpan.FromHours (2), TimeSpan.FromSeconds (oldDuration - entry.Duration));
            Assert.AreEqual (null, entry.StopTime);
        }

        [Test]
        public void TestRunningDurationChange ()
        {
            var entry = Model.Update (new TimeEntryModel () {
                StartTime = new DateTime (2013, 10, 1, 10, 12, 30, DateTimeKind.Utc),
                IsRunning = true,
                IsPersisted = true,
            });
            var oldDuration = entry.Duration;
            Assert.AreNotEqual (0, oldDuration);

            entry.Duration += (long)TimeSpan.FromHours (1).TotalSeconds;
            Assert.AreEqual (new DateTime (2013, 10, 1, 9, 12, 30, DateTimeKind.Utc), entry.StartTime);
            Assert.AreEqual (null, entry.StopTime);
        }

        [Test]
        public void TestStoppedStartChange ()
        {
            var entry = Model.Update (new TimeEntryModel () {
                StartTime = new DateTime (2013, 10, 1, 10, 12, 30, DateTimeKind.Utc),
                StopTime = new DateTime (2013, 10, 1, 13, 12, 30, DateTimeKind.Utc),
                IsPersisted = true,
            });
            Assert.AreEqual (TimeSpan.FromHours (3), TimeSpan.FromSeconds (entry.Duration));

            // Changing start time should keep the duration constant and adjust stop time
            entry.StartTime = new DateTime (2013, 10, 1, 11, 12, 30, DateTimeKind.Utc);
            Assert.AreEqual (TimeSpan.FromHours (3), TimeSpan.FromSeconds (entry.Duration));
            Assert.AreEqual (new DateTime (2013, 10, 1, 14, 12, 30, DateTimeKind.Utc), entry.StopTime);
        }

        [Test]
        public void TestStoppedStopChange ()
        {
            var entry = Model.Update (new TimeEntryModel () {
                StartTime = new DateTime (2013, 10, 1, 10, 12, 30, DateTimeKind.Utc),
                StopTime = new DateTime (2013, 10, 1, 13, 12, 30, DateTimeKind.Utc),
                IsPersisted = true,
            });
            Assert.AreEqual (TimeSpan.FromHours (3), TimeSpan.FromSeconds (entry.Duration));

            entry.StopTime = new DateTime (2013, 10, 1, 11, 12, 30, DateTimeKind.Utc);
            Assert.AreEqual (new DateTime (2013, 10, 1, 10, 12, 30, DateTimeKind.Utc), entry.StartTime);
            Assert.AreEqual (TimeSpan.FromHours (1), TimeSpan.FromSeconds (entry.Duration));
        }

        [Test]
        public void TestStoppedDurationChange ()
        {
            var entry = Model.Update (new TimeEntryModel () {
                StartTime = new DateTime (2013, 10, 1, 10, 12, 30, DateTimeKind.Utc),
                StopTime = new DateTime (2013, 10, 1, 13, 12, 30, DateTimeKind.Utc),
                IsPersisted = true,
            });
            Assert.AreEqual (TimeSpan.FromHours (3), TimeSpan.FromSeconds (entry.Duration));

            entry.Duration = (long)TimeSpan.FromHours (1).TotalSeconds;
            Assert.AreEqual (new DateTime (2013, 10, 1, 10, 12, 30, DateTimeKind.Utc), entry.StartTime);
            Assert.AreEqual (new DateTime (2013, 10, 1, 11, 12, 30, DateTimeKind.Utc), entry.StopTime);
        }

        [Test]
        public void TestDuronlyNewDurationChange ()
        {
            var entry = Model.Update (new TimeEntryModel () {
                StartTime = new DateTime (2013, 10, 1).ToUtc (),
                DurationOnly = true,
            });
            Assert.IsNull (entry.StopTime);
            Assert.AreEqual (0, entry.Duration);

            entry.Duration = (long)TimeSpan.FromHours (1).TotalSeconds;
            Assert.IsNull (entry.StopTime);
            Assert.AreEqual (new DateTime (2013, 10, 1).ToUtc (), entry.StartTime);
        }

        [Test]
        public void TestDuronlyRunningDurationChange ()
        {
            var entry = Model.Update (new TimeEntryModel () {
                StartTime = new DateTime (2013, 10, 1).ToUtc (),
                DurationOnly = true,
                IsPersisted = true,
                IsRunning = true,
            });
            Assert.IsNull (entry.StopTime);
            Assert.AreEqual (0, entry.Duration);

            entry.Duration = (long)TimeSpan.FromHours (1).TotalSeconds;
            Assert.IsNull (entry.StopTime);
            Assert.AreEqual (new DateTime (2013, 10, 1).ToUtc (), entry.StartTime);
        }

        [Test]
        public void TestDuronlyStoppedDurationChange ()
        {
            var entry = Model.Update (new TimeEntryModel () {
                StartTime = new DateTime (2013, 10, 1).ToUtc (),
                DurationOnly = true,
                IsPersisted = true,
                IsRunning = false,
            });
            Assert.IsNull (entry.StopTime);
            Assert.AreEqual (0, entry.Duration);

            entry.Duration = (long)TimeSpan.FromHours (1).TotalSeconds;
            Assert.IsNull (entry.StopTime);
            Assert.AreEqual (new DateTime (2013, 10, 1).ToUtc (), entry.StartTime);
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
