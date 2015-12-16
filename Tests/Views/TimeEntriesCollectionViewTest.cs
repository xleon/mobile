using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Toggl.Phoebe.Data;
using Toggl.Phoebe.Data.DataObjects;
using Toggl.Phoebe.Data.Utils;
using Toggl.Phoebe.Data.Views;
using XPlatUtils;

namespace Toggl.Phoebe.Tests.Views
{
    [TestFixture]
    public class TimeEntriesCollectionViewTest : Test
    {
        private const int Timeout = 500;

        public class TestFeed : TimeEntriesCollectionView.IFeed
        {
            public event EventHandler<Exception> FailReported;

            public int BufferMilliseconds { get; private set; }
            public IList<Action<DataChangeMessage>> Listeners { get; private set; }
            public bool UseThreadPool { get; private set; }

            public TestFeed (int bufferMilliseconds = 0)
            {
                BufferMilliseconds = bufferMilliseconds;
                UseThreadPool = false;
                Listeners = new List<Action<DataChangeMessage>> ();
            }

            public void Push (TimeEntryData data, DataAction action)
            {
                foreach (var listener in Listeners) {
                    listener (new DataChangeMessage (null, data, action));
                }
            }

#pragma warning disable 1998
            public async Task<ITimeEntryHolder> CreateTimeHolder (
                bool isGrouped, TimeEntryData entry, ITimeEntryHolder previous = null)
            {
                // Don't load info to prevent interacting with database in unit tests
                return isGrouped
                       ? (ITimeEntryHolder)new TimeEntryGroup (entry, previous)
                       : new TimeEntryHolder (entry);
            }
#pragma warning restore 1998

            public void SubscribeToMessageBus (Action<DataChangeMessage> action)
            {
                Listeners.Add (action);
            }

            public void ReportFailure (Exception ex)
            {
                if (FailReported != null) {
                    FailReported (this, ex);
                }
            }

            public Task<IList<TimeEntryData>> DownloadTimeEntries (DateTime endTime, int numDays, CancellationToken ct)
            {
                return Task.Run<IList<TimeEntryData>> (() => new List<TimeEntryData> ());
            }

            public void Dispose ()
            {
                Listeners.Clear ();
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
        public TimeEntryData CreateTimeEntry (
            DateTime startTime, string desc = "Test entry", Guid taskId = default (Guid), Guid projId = default (Guid))
        {
            return new TimeEntryData {
                Id = Guid.NewGuid (),
                StartTime = startTime,
                StopTime = startTime.AddMinutes (1),
                UserId = userId,
                WorkspaceId = workspaceId,
                TaskId = taskId == Guid.Empty ? Guid.NewGuid () : taskId,
                ProjectId = projId == Guid.Empty ? Guid.NewGuid () : projId,
                Description = desc,
                State = TimeEntryState.Finished,
            };
        }

        public TimeEntryData CreateTimeEntry (TimeEntryData prev, int daysOffset = 0, int minutesOffset = 0)
        {
            var startTime = prev.StartTime.AddDays (daysOffset).AddMinutes (minutesOffset);
            return new TimeEntryData {
                Id = prev.Id,
                StartTime = startTime,
                StopTime = startTime.AddMinutes (1),
                UserId = userId,
                WorkspaceId = workspaceId,
                TaskId = prev.TaskId,
                ProjectId = prev.ProjectId,
                Description = prev.Description,
                State = TimeEntryState.Finished,
            };
        }

        private bool CastEquals<T1, T2> (object a, object b, Func<T1, T2, bool> equals)
        {
            try {
                return equals ((T1)a, (T2)b);
            } catch {
                return false;
            }
        }

        private Task<IList<EventInfo>> GetEvents (
            int eventCount, INotifyCollectionChanged collection, TestFeed feed, Action raiseEvents)
        {
            var i = 0;
            var li = new List<EventInfo> ();
            var tcs = new TaskCompletionSource<IList<EventInfo>> ();

            feed.FailReported += (s, ex) => tcs.SetException (ex);
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
                } else if (itemA is ITimeEntryHolder) {
                    if (CastEquals<ITimeEntryHolder, TimeEntryData> (itemA, items [i], (a, b) => a.Data.Id == b.Id)) {
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
            var singleView = await TimeEntriesCollectionView.InitAdHoc (false, feed);

            var dt = new DateTime (2015, 12, 14, 10, 0, 0, 0);
            var entry1 = CreateTimeEntry (dt);
            var entry2 = CreateTimeEntry (dt.AddHours (1));

            var evs = await GetEvents (4, singleView, feed, () => {
                feed.Push (entry1, DataAction.Put);
                feed.Push (entry2, DataAction.Put);
            });

            AssertList (singleView.Data, dt, entry2, entry1);

            // Events after first push
            AssertEvent (evs[0], "add", "date header");
            AssertEvent (evs[1], "add", "time entry");

            // Events after second push
            AssertEvent (evs[2], "replace", "date header");
            AssertEvent (evs[3], "add", "time entry");
        }

        // TODO TestMoveForwardAndBackward (with adds and deletes in between)

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
            var feed = new TestFeed (100);
            var singleView = await TimeEntriesCollectionView.InitAdHoc (false, feed, entries);

            var evs = await GetEvents (4, singleView, feed, () => {
                feed.Push (CreateTimeEntry (entries[1], -1), DataAction.Put); // Move entry to previous day
                feed.Push (entries[2], DataAction.Delete);  // Delete entry
                feed.Push (entries[4], DataAction.Delete);  // Delete entry
            });

            AssertList (singleView.Data, dt, entries[0], dt.AddDays (-1), entries[3], entries[1]);

            AssertEvent (evs[0], "replace", "date header"); // Update today's header
            AssertEvent (evs[1], "remove", "time entry");   // Remove time entry
            AssertEvent (evs[2], "remove", "time entry");   // Remove time entry
            AssertEvent (evs[3], "move", "time entry");     // Move time entry
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
            var feed = new TestFeed (100);
            var singleView = await TimeEntriesCollectionView.InitAdHoc (false, feed, entries);

            var evs = await GetEvents (5, singleView, feed, () => {
                feed.Push (CreateTimeEntry (entries[3], 1), DataAction.Put); // Move entry to next day
                feed.Push (entries[0], DataAction.Delete); // Delete entry
                feed.Push (entries[1], DataAction.Delete); // Delete entry
            });

            AssertList (singleView.Data, dt.AddDays (1), entries[3], dt, entries[2], entries[4]);

            AssertEvent (evs[0], "replace", "date header"); // Update today's header
            AssertEvent (evs[1], "remove", "time entry");   // Remove time entry
            AssertEvent (evs[2], "remove", "time entry");     // Move time entry
            AssertEvent (evs[3], "move", "time entry");     // Move time entry
            AssertEvent (evs[4], "replace", "date header"); // Update yesterday's header
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
            var feed = new TestFeed (100);
            var singleView = await TimeEntriesCollectionView.InitAdHoc (false, feed, entry1, entry2, entry4);

            var evs = await GetEvents (4, singleView, feed, () => {
                feed.Push (CreateTimeEntry (entry2, -1), DataAction.Put); // Move entry to previous day
                feed.Push (entry3, DataAction.Put); // Add entry
                feed.Push (entry5, DataAction.Put); // Add entry
            });

            AssertList (singleView.Data, dt, entry1, entry3, dt.AddDays (-1), entry4, entry2, entry5);

            AssertEvent (evs[0], "add", "time entry");
            AssertEvent (evs[1], "replace", "date header");
            AssertEvent (evs[2], "move", "time entry");
            AssertEvent (evs[3], "add", "time entry");
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
            var feed = new TestFeed (100);
            var singleView = await TimeEntriesCollectionView.InitAdHoc (false, feed, entry3, entry4, entry5);

            var evs = await GetEvents (5, singleView, feed, () => {
                feed.Push (entry2, DataAction.Put); // Add entry
                feed.Push (entry1, DataAction.Put); // Add entry
                feed.Push (CreateTimeEntry (entry4, 1), DataAction.Put); // Move entry to next day
            });

            AssertList (singleView.Data, dt.AddDays (1), entry1, entry4, entry2, dt, entry3, entry5);

            AssertEvent (evs[0], "add", "date header");
            AssertEvent (evs[1], "add", "time entry");
            AssertEvent (evs[2], "move", "time entry");
            AssertEvent (evs[3], "add", "time entry");
            AssertEvent (evs[4], "replace", "date header");
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
            var feed = new TestFeed (100);
            var singleView = await TimeEntriesCollectionView.InitAdHoc (
                                 false, feed, entry1, entry2, entry4, entry5, entry6);

            var evs = await GetEvents (6, singleView, feed, () => {
                feed.Push (entry1, DataAction.Delete);
                feed.Push (entry3, DataAction.Put);
                feed.Push (entry6, DataAction.Delete);
                feed.Push (CreateTimeEntry (entry4, minutesOffset: -30), DataAction.Put);
                feed.Push (CreateTimeEntry (entry2, daysOffset: -1), DataAction.Put);
            });

            AssertList (singleView.Data, dt, entry3, dt.AddDays (-1), entry2, entry5, entry4);

            AssertEvent (evs[0], "replace", "date header");
            AssertEvent (evs[1], "remove", "time entry");
            AssertEvent (evs[2], "add", "time entry");
            AssertEvent (evs[3], "move", "time entry");
            AssertEvent (evs[4], "remove", "time entry");
            AssertEvent (evs[5], "move", "time entry");
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
            var feed = new TestFeed (100);
            var singleView = await TimeEntriesCollectionView.InitAdHoc (false, feed, entry1, entry3, entry4, entry5);

            var evs = await GetEvents (7, singleView, feed, () => {
                feed.Push (entry4, DataAction.Delete);
                feed.Push (entry2, DataAction.Put);
                feed.Push (entry6, DataAction.Put);
                feed.Push (CreateTimeEntry (entry3, minutesOffset: 40), DataAction.Put);
                feed.Push (CreateTimeEntry (entry5, daysOffset: 1), DataAction.Put);
            });

            AssertList (singleView.Data, dt, entry3, entry1, entry2, entry5, dt.AddDays (-1), entry6);

            AssertEvent (evs[0], "replace", "date header");
            AssertEvent (evs[1], "move", "time entry");
            AssertEvent (evs[2], "add", "time entry");
            AssertEvent (evs[3], "move", "time entry");
            AssertEvent (evs[4], "replace", "date header");
            AssertEvent (evs[5], "remove", "time entry");
            AssertEvent (evs[6], "add", "time entry");
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
            var feed = new TestFeed (100);
            var singleView = await TimeEntriesCollectionView.InitAdHoc (false, feed, entries);

            var evs = await GetEvents (3, singleView, feed, () => {
                feed.Push (CreateTimeEntry (entries[0], 0, 2), DataAction.Put);
                feed.Push (CreateTimeEntry (entries[1], 0, 2), DataAction.Put);
                feed.Push (CreateTimeEntry (entries[4], 0, 2), DataAction.Put);
            });

            AssertList (singleView.Data, dt, entries[0], entries[1], entries[2], entries[3], dt.AddDays (-1), entries[4]);

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
            var feed = new TestFeed (100);
            var singleView = await TimeEntriesCollectionView.InitAdHoc (false, feed, entry1, entry2);

            // Order check before update
            AssertList (singleView.Data, dt, entry1, entry2);

            var evs = await GetEvents (4, singleView, feed, () => {
                // Move entries to next day
                feed.Push (CreateTimeEntry (entry2, daysOffset: 1), DataAction.Put);
                feed.Push (CreateTimeEntry (entry1, daysOffset: 1), DataAction.Put);
            });

            // Check if date has changed
            AssertList (singleView.Data, dt.AddDays (1), entry1, entry2);

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
            var feed = new TestFeed (100);
            var singleView = await TimeEntriesCollectionView.InitAdHoc (false, feed, entry1, entry2);

            // Order check before update
            AssertList (singleView.Data, dt, entry1, entry2);

            var evs = await GetEvents (4, singleView, feed, () => {
                // Move entries to previous day
                feed.Push (CreateTimeEntry (entry1, daysOffset: -1), DataAction.Put);
                feed.Push (CreateTimeEntry (entry2, daysOffset: -1), DataAction.Put);
            });

            // Order check after update
            AssertList (singleView.Data, dt.AddDays (-1), entry1, entry2);

            Assert.LessOrEqual (evs.Count, 4);
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
            var singleView = await TimeEntriesCollectionView.InitAdHoc (false, feed, entry1, entry2, entry3);

            // Order check before update
            AssertList (singleView.Data, dt, entry1, entry2, dt.AddDays (-1), entry3);

            var evs = await GetEvents (3, singleView, feed, () =>
                                       // Move first entry to previous day
                                       feed.Push (CreateTimeEntry (entry1, daysOffset: -1), DataAction.Put));

            // Order check after update
            AssertList (singleView.Data, dt, entry2, dt.AddDays (-1), entry1, entry3);

            AssertEvent (evs[0], "replace", "date header");
            AssertEvent (evs[1], "replace", "date header");
            AssertEvent (evs[2], "move", "time entry");
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
            var singleView = await TimeEntriesCollectionView.InitAdHoc (false, feed, entry1, entry2, entry3, entry4);

            // Order check before update
            AssertList (singleView.Data, dt, entry1, entry2, entry3, entry4);

            var evs = await GetEvents (3, singleView, feed, () =>
                                       // Move first entry to previous day
                                       feed.Push (CreateTimeEntry (entry1, daysOffset: -1), DataAction.Put));

            // Order check after update
            AssertList (singleView.Data, dt, entry2, entry3, entry4, dt.AddDays (-1), entry1);

            AssertEvent (evs[0], "replace", "date header"); // Update old header
            AssertEvent (evs[1], "add", "date header");     // Add new header
            AssertEvent (evs[2], "move", "time entry");     // Move time entry
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
            var singleView = await TimeEntriesCollectionView.InitAdHoc (false, feed, entry1, entry2, entry3, entry4);

            // Order check before update
            AssertList (singleView.Data, dt, entry1, entry2, entry3, entry4);

            var evs = await GetEvents (3, singleView, feed, () =>
                                       // Move first entry to next day
                                       feed.Push (CreateTimeEntry (entry4, daysOffset: 1), DataAction.Put));

            // Order check after update
            AssertList (singleView.Data, dt.AddDays (1), entry4, dt, entry1, entry2, entry3);

            AssertEvent (evs[0], "add", "date header");     // Add new header
            AssertEvent (evs[1], "move", "time entry");     // Move time entry
            AssertEvent (evs[2], "replace", "date header"); // Update old header
        }

        [Test]
        public async void TestTripleTimeMovement ()
        {
            var dt = new DateTime (2015, 12, 14, 10, 10, 0, 0);
            var entry1 = CreateTimeEntry (dt);                  // First at list
            var entry2 = CreateTimeEntry (dt.AddMinutes (-5));  // Second at list
            var entry3 = CreateTimeEntry (dt.AddMinutes (-10)); // Third at list

            // Allow some buffer so pushes are handled at the same time
            var feed = new TestFeed (100);
            var singleView = await TimeEntriesCollectionView.InitAdHoc (false, feed, entry1, entry2, entry3);

            var evs = await GetEvents (3, singleView, feed, () => {
                // Shuffle time entries
                feed.Push (CreateTimeEntry (entry1, minutesOffset: -5), DataAction.Put);
                feed.Push (CreateTimeEntry (entry2, minutesOffset: -5), DataAction.Put);
                feed.Push (CreateTimeEntry (entry3, minutesOffset: 10), DataAction.Put);
            });

            // Order check after update
            AssertList (singleView.Data, dt, entry3, entry1, entry2);

            AssertEvent (evs[0], "move", "time entry");    // Move time entry3
            AssertEvent (evs[1], "replace", "time entry"); // Update time entry1
            AssertEvent (evs[2], "replace", "time entry"); // Update time entry2
        }
    }
}
