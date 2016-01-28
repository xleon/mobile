using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Toggl.Phoebe.Data;
using Toggl.Phoebe.Data.DataObjects;
using Toggl.Phoebe.Data.Utils;
using Toggl.Phoebe.Data.Views;
using XPlatUtils;

namespace Toggl.Phoebe.Tests.Data
{
    [TestFixture]
    public class TimeEntriesCollectionTest : Test
    {
        private const int Timeout = 500;

        class TestFeed : IObservable<TimeEntryMessage>
        {
            class Disposable : IDisposable
            {
                Action action;

                public Disposable (Action action)
                {
                    this.action = action;
                }

                public void Dispose ()
                {
                    if (action != null) {
                        action ();
                        action = null;
                    }
                }
            }

            IObserver<TimeEntryMessage> observer;

            public IDisposable Subscribe (IObserver<TimeEntryMessage> observer)
            {
                this.observer = observer;
                return new Disposable (() => this.observer = null);
            }

            public void Push (TimeEntryData data, DataAction action)
            {
                if (observer != null) {
                    observer.OnNext (new TimeEntryMessage (data, action));
                }
            }
        }

        class EventInfo
        {
            public NotifyCollectionChangedAction Action { get; private set; }
            public IHolder Item { get; private set; }
            public int NewIndex { get; private set; }
            public int OldIndex { get; private set; }

            public EventInfo (NotifyCollectionChangedEventArgs ev)
            {
                Action = ev.Action;
                if (ev.NewItems != null && ev.NewItems.Count > 0) {
                    Item = ev.NewItems [0] as IHolder;
                } else if (ev.OldItems != null && ev.OldItems.Count > 0) {
                    Item = ev.OldItems [0] as IHolder;
                }
                NewIndex = ev.NewStartingIndex;
                OldIndex = ev.OldStartingIndex;
            }

            public override string ToString ()
            {
                return string.Format ("[{0}, {1}, Index={2}]", Action, Item, NewIndex);
            }
        }

        Guid userId;
        Guid workspaceId;

        public override void SetUp ()
        {
            base.SetUp ();

            userId = Guid.NewGuid ();
            workspaceId = Guid.NewGuid ();
            ServiceContainer.Register<IPlatformUtils> (new UpgradeManagerTest.PlatformUtils ());
        }

        // TODO: Extract these methods to a Test.Util class
        public TimeEntryData CreateTimeEntry (DateTime startTime, Guid taskId = default (Guid), Guid projId = default (Guid))
        {
            return new TimeEntryData {
                Id = Guid.NewGuid (),
                StartTime = startTime,
                StopTime = startTime.AddMinutes (1),
                UserId = userId,
                WorkspaceId = workspaceId,
                TaskId = taskId == Guid.Empty ? Guid.NewGuid () : taskId,
                ProjectId = projId == Guid.Empty ? Guid.NewGuid () : projId,
                Description = "Test Entry",
                State = TimeEntryState.Finished,
            };
        }

        public TimeEntryData CreateTimeEntry (TimeEntryData prev, int daysOffset = 0, int minutesOffset = 0, Guid task = default (Guid), Guid proj = default (Guid))
        {
            var startTime = prev.StartTime.AddDays (daysOffset).AddMinutes (minutesOffset);
            return new TimeEntryData {
                Id = prev.Id,
                StartTime = startTime,
                StopTime = startTime.AddMinutes (1),
                UserId = userId,
                WorkspaceId = workspaceId,
                TaskId = task == Guid.Empty ? prev.TaskId : task,
                ProjectId = proj == Guid.Empty ? prev.ProjectId : proj,
                Description = prev.Description,
                State = TimeEntryState.Finished,
            };
        }

        public bool CastEquals<T1, T2> (object a, object b, Func<T1, T2, bool> equals)
        {
            try {
                return equals ((T1)a, (T2)b);
            } catch {
                return false;
            }
        }

        private static TimeEntriesCollection<T> CreateTimeEntriesCollection<T> (
            TestFeed feed, int bufferMilliseconds, params TimeEntryData[] timeEntries)
        where T : ITimeEntryHolder
        {
            // First create a collection with no time buffer to add the firs items
            var col1 = new TimeEntriesCollection<T> (feed, 0, false, false);

            foreach (var entry in timeEntries) {
                // Create a new entry to protect the reference;
                // This will run synchronous as we're not querying data from the database
                feed.Push (new TimeEntryData (entry), DataAction.Put);
            }

            // Create a new collection with the desired buffer
            if (bufferMilliseconds > 0) {
                var col2 = new TimeEntriesCollection<T> (feed, bufferMilliseconds, false, false);
                col2.Reset (col1.Data);
                return col2;
            } else {
                return col1;
            }
        }


        private Task<IList<EventInfo>> GetEvents (
            int eventCount, INotifyCollectionChanged collection, TestFeed feed, Action raiseEvents)
        {
            var i = 0;
            var li = new List<EventInfo> ();
            var tcs = new TaskCompletionSource<IList<EventInfo>> ();

            collection.CollectionChanged += (s, e) => {
                li.Add (new EventInfo (e));
                if (++i == eventCount) {
                    tcs.SetResult (li);
                }
            };
            raiseEvents ();

            var timer = new System.Timers.Timer (Timeout);
            timer.Elapsed += (s, e) => {
                timer.Stop();
                if (!tcs.Task.IsCompleted) {
                    tcs.SetException (new Exception ("Timeout"));
                }
            };
            timer.Start ();

            return tcs.Task;
        }

        private void AssertEvent (EventInfo ev, string evType, string itemType)
        {
            NotifyCollectionChangedAction evAction;

            if (evType == "add") {
                evAction = NotifyCollectionChangedAction.Add;
            } else if (evType == "move") {
                evAction = NotifyCollectionChangedAction.Move;
            } else if (evType == "replace") {
                evAction = NotifyCollectionChangedAction.Replace;
            } else if (evType == "remove") {
                evAction = NotifyCollectionChangedAction.Remove;
            } else {
                throw new NotSupportedException ();
            }

            if (ev.Action != evAction) {
                Assert.Fail ("Expected {0} but was {1}", evType, ev.Action);
            }

            bool isExpectedType;
            if (itemType == "date header") {
                isExpectedType = ev.Item is DateHolder;
            } else if (itemType == "time entry") {
                isExpectedType = ev.Item is ITimeEntryHolder;
            } else {
                throw new NotSupportedException ();
            }

            if (!isExpectedType) {
                Assert.Fail ("Expected {0} but was {1}", itemType, ev.Item.GetType().Name);
            }
        }

        private void AssertList (
            IEnumerable<object> collection, params object[] items)
        {
            var i = -1;
            foreach (var itemA in collection) {
                i++;
                if (items.Length <= i) {
                    Assert.Fail ("Collection has more items than expected");
                }

                if (itemA is DateHolder) {
                    if (CastEquals<DateHolder, DateTime> (itemA, items [i], (a, b) => a.Date == b.Date)) {
                        continue;
                    }
                } else if (itemA is TimeEntryHolder) {
                    if (CastEquals<TimeEntryHolder, TimeEntryData> (itemA, items [i], (a, b) => a.Data.Id == b.Id)) {
                        continue;
                    }
                } else if (itemA is TimeEntryGroup) {
                    if (CastEquals<TimeEntryGroup, TimeEntryData> (itemA, items [i], (a, b) => a.DataCollection.Single ().Id == b.Id) ||
                            CastEquals<TimeEntryGroup, TimeEntryData[]> (itemA, items [i], (a, b) => a.DataCollection.SequenceEqual (b, (x, y) => x.Id == y.Id))) {
                        continue;
                    }
                }

                Assert.Fail ("Collection has an unexpected item at index {0}", i);
            }

            if (++i < items.Length) {
                Assert.Fail ("Collection has less items than expected");
            }
        }

        [Test]
        public async void TestSendTwoPutsToEmptyList ()
        {
            var feed = new TestFeed ();
            var singleView = CreateTimeEntriesCollection<TimeEntryHolder> (feed, 0);

            var dt = new DateTime (2015, 12, 14, 10, 0, 0, 0);
            var entry1 = CreateTimeEntry (dt);
            var entry2 = CreateTimeEntry (dt.AddHours (1));

            var evs = await GetEvents (4, singleView, feed, () => {
                feed.Push (entry1, DataAction.Put);
                feed.Push (entry2, DataAction.Put);
            });

            Assert.AreEqual (4, evs.Count);
            AssertList (singleView, dt, entry2, entry1);

            // Events after first push
            AssertEvent (evs[0], "add", "date header");
            AssertEvent (evs[1], "add", "time entry");

            // Events after second push
            AssertEvent (evs[2], "replace", "date header");
            AssertEvent (evs[3], "add", "time entry");
        }

        [Test]
        public async void TestMoveForwardWithDelete ()
        {
            var dt = new DateTime (2015, 12, 14, 19, 0, 0);
            var entries = new [] {
                CreateTimeEntry (dt),
                CreateTimeEntry (dt.AddMinutes (-10)),
                CreateTimeEntry (dt.AddMinutes (-20)),
                CreateTimeEntry (dt.AddDays (-1)),
                CreateTimeEntry (dt.AddDays (-1).AddMinutes (-20)),
            };

            // Allow some buffer so pushes are handled at the same time
            var feed = new TestFeed ();
            var singleView = CreateTimeEntriesCollection<TimeEntryHolder> (feed, 100, entries);

            var evs = await GetEvents (5, singleView, feed, () => {
                feed.Push (CreateTimeEntry (entries[1], -1), DataAction.Put); // Move entry to previous day
                feed.Push (entries[2], DataAction.Delete);  // Delete entry
                feed.Push (entries[4], DataAction.Delete);  // Delete entry
            });

            Assert.AreEqual (5, evs.Count);
            AssertList (singleView, dt, entries[0], dt.AddDays (-1), entries[3], entries[1]);

            AssertEvent (evs[0], "replace", "date header"); // Update today's header
            AssertEvent (evs[1], "remove", "time entry");   // Remove time entry
            AssertEvent (evs[2], "remove", "time entry");   // Remove time entry
            AssertEvent (evs[3], "move", "time entry");     // Move time entry
            AssertEvent (evs[4], "replace", "time entry");
        }

        [Test]
        public async void TestMoveBackwardWithDelete ()
        {
            var dt = new DateTime (2015, 12, 14, 19, 0, 0);
            var entries = new [] {
                CreateTimeEntry (dt.AddDays (1)),
                CreateTimeEntry (dt.AddDays (1).AddMinutes (-20)),
                CreateTimeEntry (dt),
                CreateTimeEntry (dt.AddMinutes (-10)),
                CreateTimeEntry (dt.AddMinutes (-20)),
            };

            // Allow some buffer so pushes are handled at the same time
            var feed = new TestFeed ();
            var singleView = CreateTimeEntriesCollection<TimeEntryHolder> (feed, 100, entries);

            var evs = await GetEvents (6, singleView, feed, () => {
                feed.Push (CreateTimeEntry (entries[3], 1), DataAction.Put); // Move entry to next day
                feed.Push (entries[0], DataAction.Delete); // Delete entry
                feed.Push (entries[1], DataAction.Delete); // Delete entry
            });

            Assert.AreEqual (6, evs.Count);
            AssertList (singleView, dt.AddDays (1), entries[3], dt, entries[2], entries[4]);

            AssertEvent (evs[0], "replace", "date header"); // Update today's header
            AssertEvent (evs[1], "remove", "time entry");   // Remove time entry
            AssertEvent (evs[2], "remove", "time entry");     // Move time entry
            AssertEvent (evs[3], "move", "time entry");     // Move time entry
            AssertEvent (evs[4], "replace", "time entry");
            AssertEvent (evs[5], "replace", "date header"); // Update yesterday's header
        }

        [Test]
        public async void TestMoveForwardWithAdd ()
        {
            var dt = new DateTime (2015, 12, 15, 10, 0, 0);
            var entry1 = CreateTimeEntry (dt);
            var entry2 = CreateTimeEntry (dt.AddMinutes (-10));
            var entry3 = CreateTimeEntry (dt.AddMinutes (-20));
            var entry4 = CreateTimeEntry (dt.AddDays (-1));
            var entry5 = CreateTimeEntry (dt.AddDays (-1).AddMinutes (-20));

            // Allow some buffer so pushes are handled at the same time
            var feed = new TestFeed ();
            var singleView = CreateTimeEntriesCollection<TimeEntryHolder> (feed, 100, entry1, entry2, entry4);

            var evs = await GetEvents (5, singleView, feed, () => {
                feed.Push (CreateTimeEntry (entry2, -1), DataAction.Put); // Move entry to previous day
                feed.Push (entry3, DataAction.Put); // Add entry
                feed.Push (entry5, DataAction.Put); // Add entry
            });

            Assert.AreEqual (5, evs.Count);
            AssertList (singleView, dt, entry1, entry3, dt.AddDays (-1), entry4, entry2, entry5);

            AssertEvent (evs[0], "add", "time entry");
            AssertEvent (evs[1], "replace", "date header");
            AssertEvent (evs[2], "move", "time entry");
            AssertEvent (evs[3], "replace", "time entry");
            AssertEvent (evs[4], "add", "time entry");
        }

        [Test]
        public async void TestMoveBackwardWithAdd ()
        {
            var dt = new DateTime (2015, 12, 15, 10, 0, 0);
            var entry1 = CreateTimeEntry (dt.AddDays (1));
            var entry2 = CreateTimeEntry (dt.AddDays (1).AddMinutes (-20));
            var entry3 = CreateTimeEntry (dt);
            var entry4 = CreateTimeEntry (dt.AddMinutes (-10));
            var entry5 = CreateTimeEntry (dt.AddMinutes (-20));

            // Allow some buffer so pushes are handled at the same time
            var feed = new TestFeed ();
            var singleView = CreateTimeEntriesCollection<TimeEntryHolder> (feed, 100, entry3, entry4, entry5);

            var evs = await GetEvents (6, singleView, feed, () => {
                feed.Push (entry2, DataAction.Put); // Add entry
                feed.Push (entry1, DataAction.Put); // Add entry
                feed.Push (CreateTimeEntry (entry4, 1), DataAction.Put); // Move entry to next day
            });

            Assert.AreEqual (6, evs.Count);
            AssertList (singleView, dt.AddDays (1), entry1, entry4, entry2, dt, entry3, entry5);

            AssertEvent (evs[0], "add", "date header");
            AssertEvent (evs[1], "add", "time entry");
            AssertEvent (evs[2], "move", "time entry");
            AssertEvent (evs[3], "replace", "time entry");
            AssertEvent (evs[4], "add", "time entry");
            AssertEvent (evs[5], "replace", "date header");
        }

        [Test]
        public async void TestMoveForwardWithTwoEntries ()
        {
            var dt = new DateTime (2015, 12, 15, 10, 0, 0);
            var entry1 = CreateTimeEntry (dt);
            var entry2 = CreateTimeEntry (dt.AddMinutes (-10));
            var entry3 = CreateTimeEntry (dt.AddMinutes (-20));
            var entry4 = CreateTimeEntry (dt.AddDays (-1));
            var entry5 = CreateTimeEntry (dt.AddDays (-1).AddMinutes (-20));
            var entry6 = CreateTimeEntry (dt.AddDays (-1).AddMinutes (-40));

            // Allow some buffer so pushes are handled at the same time
            var feed = new TestFeed ();
            var singleView = CreateTimeEntriesCollection<TimeEntryHolder> (
                                 feed, 100, entry1, entry2, entry4, entry5, entry6);

            var evs = await GetEvents (8, singleView, feed, () => {
                feed.Push (entry1, DataAction.Delete);
                feed.Push (entry3, DataAction.Put);
                feed.Push (entry6, DataAction.Delete);
                feed.Push (CreateTimeEntry (entry4, minutesOffset: -30), DataAction.Put);
                feed.Push (CreateTimeEntry (entry2, daysOffset: -1), DataAction.Put);
            });

            Assert.AreEqual (8, evs.Count);
            AssertList (singleView, dt, entry3, dt.AddDays (-1), entry2, entry5, entry4);

            AssertEvent (evs[0], "replace", "date header");
            AssertEvent (evs[1], "remove", "time entry");
            AssertEvent (evs[2], "add", "time entry");
            AssertEvent (evs[3], "move", "time entry");
            AssertEvent (evs[4], "replace", "time entry");
            AssertEvent (evs[5], "remove", "time entry");
            AssertEvent (evs[6], "move", "time entry");
            AssertEvent (evs[7], "replace", "time entry");
        }

        [Test]
        public async void TestMoveBackwardWithTwoEntries ()
        {
            var dt = new DateTime (2015, 12, 15, 10, 0, 0);
            var entry1 = CreateTimeEntry (dt.AddMinutes (-10));
            var entry2 = CreateTimeEntry (dt.AddMinutes (-20));
            var entry3 = CreateTimeEntry (dt.AddMinutes (-40));
            var entry4 = CreateTimeEntry (dt.AddDays (-1).AddMinutes (-10));
            var entry5 = CreateTimeEntry (dt.AddDays (-1).AddMinutes (-30));
            var entry6 = CreateTimeEntry (dt.AddDays (-1).AddMinutes (-40));

            // Allow some buffer so pushes are handled at the same time
            var feed = new TestFeed ();
            var singleView = CreateTimeEntriesCollection<TimeEntryHolder> (feed, 100, entry1, entry3, entry4, entry5);

            var evs = await GetEvents (9, singleView, feed, () => {
                feed.Push (entry4, DataAction.Delete);
                feed.Push (entry2, DataAction.Put);
                feed.Push (entry6, DataAction.Put);
                feed.Push (CreateTimeEntry (entry3, minutesOffset: 40), DataAction.Put);
                feed.Push (CreateTimeEntry (entry5, daysOffset: 1), DataAction.Put);
            });

            Assert.AreEqual (9, evs.Count);
            AssertList (singleView, dt, entry3, entry1, entry2, entry5, dt.AddDays (-1), entry6);

            AssertEvent (evs[0], "replace", "date header");
            AssertEvent (evs[1], "move", "time entry");
            AssertEvent (evs[2], "replace", "time entry");
            AssertEvent (evs[3], "add", "time entry");
            AssertEvent (evs[4], "move", "time entry");
            AssertEvent (evs[5], "replace", "time entry");
            AssertEvent (evs[6], "replace", "date header");
            AssertEvent (evs[7], "remove", "time entry");
            AssertEvent (evs[8], "add", "time entry");
        }

        [Test]
        public async void TestMoveForwardAndBackward ()
        {
            var dt = new DateTime (2015, 12, 15, 10, 0, 0);
            var entry1 = CreateTimeEntry (dt);
            var entry2 = CreateTimeEntry (dt.AddMinutes (-20));
            var entry3 = CreateTimeEntry (dt.AddDays (-1));
            var entry4 = CreateTimeEntry (dt.AddDays (-1).AddMinutes (-10));

            // Allow some buffer so pushes are handled at the same time
            var feed = new TestFeed ();
            var singleView = CreateTimeEntriesCollection<TimeEntryHolder> (feed, 100, entry1, entry3, entry4);

            var evs = await GetEvents (8, singleView, feed, () => {
                feed.Push (CreateTimeEntry (entry3, 1, -10), DataAction.Put);
                feed.Push (CreateTimeEntry (entry1, -1, -20), DataAction.Put);
                feed.Push (entry2, DataAction.Put);
                feed.Push (entry4, DataAction.Delete);
            });

            Assert.AreEqual (8, evs.Count);
            AssertList (singleView, dt, entry3, entry2, dt.AddDays (-1), entry1);

            AssertEvent (evs[0], "replace", "date header");
            AssertEvent (evs[1], "move", "time entry");
            AssertEvent (evs[2], "replace", "time entry");
            AssertEvent (evs[3], "add", "time entry");
            AssertEvent (evs[4], "replace", "date header");
            AssertEvent (evs[5], "remove", "time entry");
            AssertEvent (evs[6], "move", "time entry");
            AssertEvent (evs[7], "replace", "time entry");
        }

        [Test]
        public async void TestUpdateThreeEntries ()
        {
            var dt = new DateTime (2015, 12, 14, 19, 0, 0);
            var entries = new [] {
                CreateTimeEntry (dt),
                CreateTimeEntry (dt.AddMinutes (-10)),
                CreateTimeEntry (dt.AddMinutes (-20)),
                CreateTimeEntry (dt.AddMinutes (-30)),
                CreateTimeEntry (dt.AddDays (-1))
            };

            // Allow some buffer so pushes are handled at the same time
            var feed = new TestFeed ();
            var singleView = CreateTimeEntriesCollection<TimeEntryHolder> (feed, 100, entries);

            var evs = await GetEvents (3, singleView, feed, () => {
                feed.Push (CreateTimeEntry (entries[0], 0, 2), DataAction.Put);
                feed.Push (CreateTimeEntry (entries[1], 0, 2), DataAction.Put);
                feed.Push (CreateTimeEntry (entries[4], 0, 2), DataAction.Put);
            });

            Assert.AreEqual (3, evs.Count);
            AssertList (singleView, dt, entries[0], entries[1], entries[2], entries[3], dt.AddDays (-1), entries[4]);

            // The date header doesn't change because total duration remains the same
            // (mock entries' duration is always 1 minute)
            AssertEvent (evs[0], "replace", "time entry");
            AssertEvent (evs[1], "replace", "time entry");
            AssertEvent (evs[2], "replace", "time entry");
        }

        [Test]
        public async void TestChangeDateHeaderToFuture ()
        {
            var dt = new DateTime (2015, 12, 14, 10, 10, 11, 0);
            var entry1 = CreateTimeEntry (dt);
            var entry2 = CreateTimeEntry (dt.AddMinutes (-5));

            // Allow some buffer so pushes are handled at the same time
            var feed = new TestFeed ();
            var singleView = CreateTimeEntriesCollection<TimeEntryHolder> (feed, 100, entry1, entry2);

            // Order check before update
            AssertList (singleView, dt, entry1, entry2);

            var evs = await GetEvents (4, singleView, feed, () => {
                // Move entries to next day
                feed.Push (CreateTimeEntry (entry2, daysOffset: 1), DataAction.Put);
                feed.Push (CreateTimeEntry (entry1, daysOffset: 1), DataAction.Put);
            });

            Assert.AreEqual (4, evs.Count);
            AssertList (singleView, dt.AddDays (1), entry1, entry2);

            AssertEvent (evs[0], "remove", "date header"); // Remove old header
            AssertEvent (evs[1], "add", "date header");    // Add new header
            AssertEvent (evs[2], "replace", "time entry"); // Update time entry
            AssertEvent (evs[3], "replace", "time entry"); // Update time entry
        }

        [Test]
        public async void TestChangeDateHeaderToPast ()
        {
            var dt = new DateTime (2015, 12, 14, 10, 10, 11, 0);
            var entry1 = CreateTimeEntry (dt);                 // First at list
            var entry2 = CreateTimeEntry (dt.AddMinutes (-5)); // Second at list

            // Allow some buffer so pushes are handled at the same time
            var feed = new TestFeed ();
            var singleView = CreateTimeEntriesCollection<TimeEntryHolder> (feed, 100, entry1, entry2);

            // Order check before update
            AssertList (singleView, dt, entry1, entry2);

            var evs = await GetEvents (4, singleView, feed, () => {
                // Move entries to previous day
                feed.Push (CreateTimeEntry (entry1, daysOffset: -1), DataAction.Put);
                feed.Push (CreateTimeEntry (entry2, daysOffset: -1), DataAction.Put);
            });

            Assert.AreEqual (4, evs.Count);
            AssertList (singleView, dt.AddDays (-1), entry1, entry2);

            AssertEvent (evs[0], "remove", "date header"); // Remove old header
            AssertEvent (evs[1], "add",    "date header"); // Add new header
            AssertEvent (evs[2], "replace", "time entry"); // Replace entry1
            AssertEvent (evs[3], "replace", "time entry"); // Replace entry2
        }

        [Test]
        public async void TestMoveToDifferentDate ()
        {
            var dt = new DateTime (2015, 12, 14, 10, 10, 11, 0);
            var entry1 = CreateTimeEntry (dt);                  // First at list
            var entry2 = CreateTimeEntry (dt.AddMinutes (-5));  // Second at list
            var entry3 = CreateTimeEntry (dt.AddDays (-1).AddMinutes (-5)); // First at previous day

            var feed = new TestFeed ();
            var singleView = CreateTimeEntriesCollection<TimeEntryHolder> (feed, 0, entry1, entry2, entry3);

            // Order check before update
            AssertList (singleView, dt, entry1, entry2, dt.AddDays (-1), entry3);

            var evs = await GetEvents (4, singleView, feed, () =>
                                       // Move first entry to previous day
                                       feed.Push (CreateTimeEntry (entry1, daysOffset: -1), DataAction.Put));

            Assert.AreEqual (4, evs.Count);
            AssertList (singleView, dt, entry2, dt.AddDays (-1), entry1, entry3);

            AssertEvent (evs[0], "replace", "date header");
            AssertEvent (evs[1], "replace", "date header");
            AssertEvent (evs[2], "move", "time entry");
            AssertEvent (evs[3], "replace", "time entry");
        }

        [Test]
        public async void TestMoveTopTimeEntryToPreviousDay ()
        {
            var dt = new DateTime (2015, 12, 14, 10, 10, 11, 0);
            var entry1 = CreateTimeEntry (dt);                  // First at list
            var entry2 = CreateTimeEntry (dt.AddMinutes (-5));  // Second at list
            var entry3 = CreateTimeEntry (dt.AddMinutes (-10)); // Third at list
            var entry4 = CreateTimeEntry (dt.AddMinutes (-15)); // Fourth at list

            var feed = new TestFeed ();
            var singleView = CreateTimeEntriesCollection<TimeEntryHolder> (feed, 0, entry1, entry2, entry3, entry4);

            // Order check before update
            AssertList (singleView, dt, entry1, entry2, entry3, entry4);

            var evs = await GetEvents (4, singleView, feed, () =>
                                       // Move first entry to previous day
                                       feed.Push (CreateTimeEntry (entry1, daysOffset: -1), DataAction.Put));

            Assert.AreEqual (4, evs.Count);
            AssertList (singleView, dt, entry2, entry3, entry4, dt.AddDays (-1), entry1);

            AssertEvent (evs[0], "replace", "date header"); // Update old header
            AssertEvent (evs[1], "add", "date header");     // Add new header
            AssertEvent (evs[2], "move", "time entry");     // Move time entry
            AssertEvent (evs[3], "replace", "time entry");
        }

        [Test]
        public async void TestMoveBottomTimeEntryToNextDay ()
        {
            var dt = new DateTime (2015, 12, 14, 10, 10, 11, 0);
            var entry1 = CreateTimeEntry (dt);                  // First at list
            var entry2 = CreateTimeEntry (dt.AddMinutes (-5));  // Second at list
            var entry3 = CreateTimeEntry (dt.AddMinutes (-10)); // Third at list
            var entry4 = CreateTimeEntry (dt.AddMinutes (-15)); // Fourth at list

            var feed = new TestFeed ();
            var singleView = CreateTimeEntriesCollection<TimeEntryHolder> (feed, 0, entry1, entry2, entry3, entry4);

            // Order check before update
            AssertList (singleView, dt, entry1, entry2, entry3, entry4);

            var evs = await GetEvents (4, singleView, feed, () =>
                                       // Move first entry to next day
                                       feed.Push (CreateTimeEntry (entry4, daysOffset: 1), DataAction.Put));

            Assert.AreEqual (4, evs.Count);
            AssertList (singleView, dt.AddDays (1), entry4, dt, entry1, entry2, entry3);

            AssertEvent (evs[0], "add", "date header");     // Add new header
            AssertEvent (evs[1], "move", "time entry");     // Move time entry
            AssertEvent (evs[2], "replace", "time entry");
            AssertEvent (evs[3], "replace", "date header"); // Update old header
        }

        [Test]
        public async void TestTripleTimeMovement ()
        {
            var dt = new DateTime (2015, 12, 14, 10, 10, 0, 0);
            var entry1 = CreateTimeEntry (dt);                  // First at list
            var entry2 = CreateTimeEntry (dt.AddMinutes (-5));  // Second at list
            var entry3 = CreateTimeEntry (dt.AddMinutes (-10)); // Third at list

            // Allow some buffer so pushes are handled at the same time
            var feed = new TestFeed ();
            var singleView = CreateTimeEntriesCollection<TimeEntryHolder> (feed, 100, entry1, entry2, entry3);

            var evs = await GetEvents (4, singleView, feed, () => {
                // Shuffle time entries
                feed.Push (CreateTimeEntry (entry1, minutesOffset: -5), DataAction.Put);
                feed.Push (CreateTimeEntry (entry2, minutesOffset: -5), DataAction.Put);
                feed.Push (CreateTimeEntry (entry3, minutesOffset: 10), DataAction.Put);
            });

            Assert.AreEqual (4, evs.Count);
            AssertList (singleView, dt, entry3, entry1, entry2);

            AssertEvent (evs[0], "move", "time entry");    // Move time entry3
            AssertEvent (evs[1], "replace", "time entry");

            AssertEvent (evs[2], "replace", "time entry"); // Update time entry1
            AssertEvent (evs[3], "replace", "time entry"); // Update time entry2
        }

        [Test]
        public async void GroupTestSendPutsToEmptyList ()
        {
            var feed = new TestFeed ();
            var groupedView = CreateTimeEntriesCollection<TimeEntryGroup> (feed, 0);

            var dt = new DateTime (2015, 12, 14, 10, 0, 0, 0);
            Guid prj = Guid.NewGuid (), task1 = Guid.NewGuid (), task2 = Guid.NewGuid ();

            var entry1 = CreateTimeEntry (dt, task1, prj);
            var entry2 = CreateTimeEntry (dt.AddHours (1), task2, prj);
            var entry3 = CreateTimeEntry (dt.AddHours (2), task1, prj);

            var evs = await GetEvents (7, groupedView, feed, () => {
                feed.Push (entry1, DataAction.Put);
                feed.Push (entry2, DataAction.Put);
                feed.Push (entry3, DataAction.Put);
            });

            Assert.AreEqual (7, evs.Count);
            AssertList (groupedView, dt, new[] { entry3, entry1 }, entry2);

            // Events after first push
            AssertEvent (evs[0], "add", "date header");
            AssertEvent (evs[1], "add", "time entry");

            // Events after second push
            AssertEvent (evs[2], "replace", "date header");
            AssertEvent (evs[3], "add", "time entry");

            // Events after third push
            AssertEvent (evs[4], "replace", "date header");
            AssertEvent (evs[5], "move", "time entry");
            AssertEvent (evs[6], "replace", "time entry");
        }

        [Test]
        public async void GroupTestAddEntriesInPlace ()
        {
            var dt = new DateTime (2015, 12, 14, 10, 0, 0, 0);
            Guid prj = Guid.NewGuid (), task1 = Guid.NewGuid (), task2 = Guid.NewGuid ();

            var entry1 = CreateTimeEntry (dt, task1, prj);
            var entry2 = CreateTimeEntry (dt.AddHours (1), task2, prj);
            var entry3 = CreateTimeEntry (dt.AddHours (2), task2, prj);

            var feed = new TestFeed ();
            var groupedView = CreateTimeEntriesCollection<TimeEntryGroup> (feed, 0, entry1, entry2);

            var evs = await GetEvents (2, groupedView, feed, () => feed.Push (entry3, DataAction.Put));

            Assert.AreEqual (2, evs.Count);
            AssertList (groupedView, dt, new[] { entry3, entry2 }, entry1);

            AssertEvent (evs[0], "replace", "date header");
            AssertEvent (evs[1], "replace", "time entry");
        }

        [Test]
        public async void GroupTestDeleteEntries ()
        {
            var dt = new DateTime (2015, 12, 14, 10, 0, 0, 0);
            Guid prj = Guid.NewGuid (), task1 = Guid.NewGuid (), task2 = Guid.NewGuid ();

            var entry1 = CreateTimeEntry (dt, task1, prj);
            var entry2 = CreateTimeEntry (dt.AddMinutes (10), task2, prj);
            var entry3 = CreateTimeEntry (dt.AddMinutes (20), task1, prj);
            var entry4 = CreateTimeEntry (dt.AddMinutes (30), task2, prj);

            var feed = new TestFeed ();
            var groupedView = CreateTimeEntriesCollection<TimeEntryGroup> (feed, 100, entry1, entry2, entry3, entry4);

            AssertList (groupedView, dt, new[] { entry4, entry2 }, new[] { entry3, entry1 });

            // Remove completely first group and partially the second
            var evs = await GetEvents (3, groupedView, feed, () => {
                feed.Push (entry4, DataAction.Delete);
                feed.Push (entry2, DataAction.Delete);
                feed.Push (entry3, DataAction.Delete);
            });

            Assert.AreEqual (3, evs.Count);
            AssertList (groupedView, dt, entry1);

            AssertEvent (evs[0], "replace", "date header");
            AssertEvent (evs[1], "remove", "time entry");
            AssertEvent (evs[2], "replace", "time entry");
        }

        [Test]
        public async void GroupTestEditProject ()
        {
            var dt = new DateTime (2015, 12, 14, 10, 0, 0, 0);
            Guid task = Guid.NewGuid (), proj1 = Guid.NewGuid (), proj2 = Guid.NewGuid (), proj3 = Guid.NewGuid ();

            var entry1 = CreateTimeEntry (dt.AddMinutes (20), task, proj1);
            var entry2 = CreateTimeEntry (dt.AddMinutes (15), task, proj1);
            var entry3 = CreateTimeEntry (dt.AddMinutes (10), task, proj1);
            var entry4 = CreateTimeEntry (dt.AddMinutes (5), task, proj2);
            var entry5 = CreateTimeEntry (dt, task, proj2);


            var feed = new TestFeed ();
            var groupedView = CreateTimeEntriesCollection<TimeEntryGroup> (feed, 100, entry1, entry2, entry3, entry4, entry5);

            AssertList (groupedView, dt, new[] { entry1, entry2, entry3 }, new[] { entry4, entry5 });

            // Edit project for all entries in group
            var evs = await GetEvents (2, groupedView, feed, () => {
                feed.Push (CreateTimeEntry (entry1, proj: proj3), DataAction.Put);
                feed.Push (CreateTimeEntry (entry2, proj: proj3), DataAction.Put);
                feed.Push (CreateTimeEntry (entry3, proj: proj3), DataAction.Put);
            });

            Assert.AreEqual (2, evs.Count);
            AssertList (groupedView, dt, new[] { entry1, entry2, entry3 }, new[] { entry4, entry5 });

            AssertEvent (evs[0], "remove", "time entry");
            AssertEvent (evs[1], "add", "time entry");
            Assert.AreEqual (proj3, ((TimeEntryGroup)groupedView[1]).Data.ProjectId);
            Assert.AreEqual (proj2, ((TimeEntryGroup)groupedView[2]).Data.ProjectId);
        }

        [Test]
        public async void GroupTestSplit ()
        {
            var dt = new DateTime (2015, 12, 14, 10, 0, 0, 0);
            Guid task = Guid.NewGuid (), proj1 = Guid.NewGuid (), proj2 = Guid.NewGuid ();

            var entry1 = CreateTimeEntry (dt.AddMinutes (10), task, proj1);
            var entry2 = CreateTimeEntry (dt.AddMinutes (5), task, proj1);
            var entry3 = CreateTimeEntry (dt, task, proj1);

            var feed = new TestFeed ();
            var groupedView = CreateTimeEntriesCollection<TimeEntryGroup> (feed, 0, entry1, entry2, entry3);

            AssertList (groupedView, dt, new[] { entry1, entry2, entry3 });

            var evs = await GetEvents (
                          2, groupedView, feed, () => feed.Push (CreateTimeEntry (entry1, proj: proj2), DataAction.Put));

            Assert.AreEqual (2, evs.Count);
            AssertList (groupedView, dt, entry1, new[] { entry2, entry3 });

            AssertEvent (evs[0], "add", "time entry");
            AssertEvent (evs[1], "replace", "time entry");
        }

        [Test]
        public async void GroupTestJoin ()
        {
            var dt = new DateTime (2015, 12, 14, 10, 0, 0, 0);
            Guid task = Guid.NewGuid (), proj1 = Guid.NewGuid (), proj2 = Guid.NewGuid ();

            var entry1 = CreateTimeEntry (dt.AddMinutes (10), task, proj1);
            var entry2 = CreateTimeEntry (dt, task, proj2);

            var feed = new TestFeed ();
            var groupedView = CreateTimeEntriesCollection<TimeEntryGroup> (feed, 0, entry1, entry2);

            AssertList (groupedView, dt, entry1, entry2);

            var evs = await GetEvents (
                          2, groupedView, feed, () => feed.Push (CreateTimeEntry (entry1, proj: proj2), DataAction.Put));

            Assert.AreEqual (2, evs.Count);
            AssertList (groupedView, dt, new[] { entry1, entry2 });

            AssertEvent (evs[0], "remove", "time entry");
            AssertEvent (evs[1], "replace", "time entry");
        }

        [Test]
        public async void GroupTestRestartBottomGroup ()
        {
            var dt = new DateTime (2015, 12, 14, 10, 0, 0, 0);
            Guid task = Guid.NewGuid (), proj1 = Guid.NewGuid (), proj2 = Guid.NewGuid ();

            var entry1 = CreateTimeEntry (dt.AddMinutes (10), task, proj2);
            var entry2 = CreateTimeEntry (dt.AddMinutes (5), task, proj1);
            var entry3 = CreateTimeEntry (dt, task, proj2);

            var feed = new TestFeed ();
            var groupedView = CreateTimeEntriesCollection<TimeEntryGroup> (feed, 0, entry2, entry3);

            AssertList (groupedView, dt, entry2, entry3);

            entry1.State = TimeEntryState.Running;
            var evs = await GetEvents (3, groupedView, feed, () => feed.Push (entry1, DataAction.Put));

            Assert.AreEqual (3, evs.Count);
            AssertList (groupedView, dt, new[] { entry1, entry3 }, entry2);

            AssertEvent (evs[0], "replace", "date header");
            AssertEvent (evs[1], "move", "time entry");
            AssertEvent (evs[2], "replace", "time entry");
            Assert.AreEqual (TimeEntryState.Running, ((ITimeEntryHolder)evs [1].Item).Data.State);
        }

        [Test]
        public async void GroupTestDifferentDays ()
        {
            var dt = new DateTime (2015, 12, 14, 10, 0, 0, 0);
            var yesterday = dt.AddDays (-1);
            Guid task = Guid.NewGuid (), proj1 = Guid.NewGuid ();

            var entry1 = CreateTimeEntry (dt.AddMinutes (10), task, proj1);
            var entry2 = CreateTimeEntry (dt.AddMinutes (5), task, proj1);
            var entry3 = CreateTimeEntry (yesterday, task, proj1);

            var feed = new TestFeed ();
            var groupedView = CreateTimeEntriesCollection<TimeEntryGroup> (feed, 0, entry2, entry3);

            AssertList (groupedView, dt, entry2, yesterday, entry3);

            var evs = await GetEvents (2, groupedView, feed, () => feed.Push (entry1, DataAction.Put));

            Assert.AreEqual (2, evs.Count);
            AssertList (groupedView, dt, new[] { entry1, entry2 }, yesterday, entry3);

            AssertEvent (evs[0], "replace", "date header");
            AssertEvent (evs[1], "replace", "time entry");
        }
    }
}
