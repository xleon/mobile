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
        public class TestFeed : TimeEntriesCollectionView.IFeed
        {
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
            public IHolder NewItem { get; private set; }
            public int NewIndex { get; private set; }
            public IHolder OldItem { get; private set; }
            public int OldIndex { get; private set; }
                
            public EventInfo (NotifyCollectionChangedEventArgs ev)
            {
                NewItem = ev.NewItems != null && ev.NewItems.Count > 0 ? ev.NewItems [0] as IHolder : null;
                NewIndex = ev.NewStartingIndex;
                OldItem = ev.OldItems != null && ev.OldItems.Count > 0 ? ev.OldItems [0] as IHolder : null;
                OldIndex = ev.OldStartingIndex;
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

        private Task<IList<NotifyCollectionChangedEventArgs>> GetEvents (
            int eventCount, INotifyCollectionChanged collection, Action raiseEvents)
        {
            var i = 0;
            var li = new List<NotifyCollectionChangedEventArgs> ();
            var tcs = new TaskCompletionSource<IList<NotifyCollectionChangedEventArgs>> ();

            // TODO: Set also  a timeout
            collection.CollectionChanged += (sender, e) => {
                li.Add (e);
                if (++i >= eventCount) {
                    tcs.SetResult (li);
                }
            };
            raiseEvents ();
            return tcs.Task;
        }

        private void AssertEvent (
            NotifyCollectionChangedEventArgs ev, string evType, string itemType)
        {
            IHolder holder;
            NotifyCollectionChangedAction evAction;

            if (evType == "add") {
                holder = ev.NewItems [0] as IHolder;
                evAction = NotifyCollectionChangedAction.Add;
            } else if (evType == "move") {
                holder = ev.NewItems [0] as IHolder;
                evAction = NotifyCollectionChangedAction.Move;
            } else if (evType == "replace") {
                holder = ev.NewItems [0] as IHolder;
                evAction = NotifyCollectionChangedAction.Replace;
            } else if (evType == "remove") {
                holder = ev.OldItems [0] as IHolder;
                evAction = NotifyCollectionChangedAction.Remove;
            } else {
                throw new NotSupportedException ();
            }

            Assert.IsTrue (ev.Action == evAction);

            if (itemType == "date header") {
                Assert.IsTrue (holder is DateHolder);
            } else if (itemType == "time entry") {
                Assert.IsTrue (holder is ITimeEntryHolder);
            } else {
                throw new NotSupportedException ();
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

            if (++i < items.Length)
                Assert.Fail ("Collection has less items than expected");
        }

        [Test]
        public async void TestSendTwoPutsToEmptyList ()
        {
            var feed = new TestFeed ();
            var singleView = await TimeEntriesCollectionView.InitAdHoc (false, feed);

            var dt = new DateTime (2015, 12, 14, 10, 0, 0, 0);
            var entry1 = CreateTimeEntry (dt);
            var entry2 = CreateTimeEntry (dt.AddHours (1));

            var evs = await GetEvents (4, singleView, () => {
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

        [Test]
        public async void TestMoveMidTimeEntryToPreviousDay ()
        {
            var dt = new DateTime (2015, 12, 14, 19, 0, 0);
            var entries = new [] {
                CreateTimeEntry (dt),
                CreateTimeEntry (dt.AddMinutes (-10)),
                CreateTimeEntry (dt.AddMinutes (-20)),
                CreateTimeEntry (dt.AddMinutes (-30)),
                CreateTimeEntry (dt.AddDays (-1))
            };

            var feed = new TestFeed ();
            var singleView = await TimeEntriesCollectionView.InitAdHoc (false, feed, entries);

            var evs = await GetEvents (3, singleView, () =>
                // Move entry to yesterday
                feed.Push (CreateTimeEntry (entries[2], -1), DataAction.Put));

            AssertList (singleView.Data, dt, entries[0], entries[1], entries[3], dt.AddDays (-1), entries[4], entries[2]);

            AssertEvent (evs[0], "replace", "date header"); // Update today's header
            AssertEvent (evs[1], "replace", "date header"); // Update yesterday's header
            AssertEvent (evs[2], "move", "time entry");
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

            var evs = await GetEvents (3, singleView, () => {
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

            var feed = new TestFeed ();
            var singleView = await TimeEntriesCollectionView.InitAdHoc (false, feed, entry1, entry2);

            // Order check before update
            AssertList (singleView.Data, dt, entry1, entry2);

            var evs = await GetEvents (2, singleView, () =>
                                       // Move first entry to next day
                                       feed.Push (CreateTimeEntry (entry1, daysOffset: 1), DataAction.Put));

            // Check if date has changed
            AssertList (singleView.Data, dt.AddDays (1), entry1, dt, entry2);

            AssertEvent (evs[0], "add", "date header"); // Add new header
            AssertEvent (evs[1], "move", "time entry"); // Move time entry
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

            var evs = await GetEvents (4, singleView, () => {
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

            var evs = await GetEvents (3, singleView, () =>
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

            var evs = await GetEvents (3, singleView, () =>
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

            var evs = await GetEvents (3, singleView, () =>
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

            var evs = await GetEvents (3, singleView, () => {
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
