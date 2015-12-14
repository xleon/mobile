using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
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
        class EventInfo
        {
            public IHolder NewItem { get; set; }
            public int NewIndex { get; set; }
            public IHolder OldItem { get; set; }
            public int OldIndex { get; set; }
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

        private Task<IList<NotifyCollectionChangedEventArgs>> GetEvents (int eventCount, INotifyCollectionChanged collection, Action raiseEvents)
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
            NotifyCollectionChangedEventArgs ev, string evType, Func<EventInfo, bool> additionalAssert = null)
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

            Assert.IsTrue (ev.Action == evAction);

            if (additionalAssert != null) {
                var evInfo = new EventInfo {
                    NewItem = ev.NewItems != null && ev.NewItems.Count > 0 ? ev.NewItems [0] as IHolder : null,
                    NewIndex = ev.NewStartingIndex,
                    OldItem = ev.OldItems != null && ev.OldItems.Count > 0 ? ev.OldItems [0] as IHolder : null,
                    OldIndex = ev.OldStartingIndex
                };
                Assert.IsTrue (additionalAssert (evInfo));
            }
        }

        private bool IsDateHeader (IHolder holder)
        {
            return holder is TimeEntriesCollectionView.DateHolder;
        }

        [Test]
        public async void TestAddSingle ()
        {
            var feed = new TimeEntriesCollectionView.TestFeed ();
            var singleView = await TimeEntriesCollectionView.InitAdHoc (false, feed);

            var evs = await GetEvents (4, singleView, () => {
                var entry1 = CreateTimeEntry (new DateTime (2015, 12, 14, 10, 0, 0, 0));
                var entry2 = CreateTimeEntry (new DateTime (2015, 12, 14, 11, 0, 0, 0));
                feed.Push (entry1, DataAction.Put);
                feed.Push (entry2, DataAction.Put);
            });

            // Events after first push
            AssertEvent (evs[0], "add", x => IsDateHeader (x.NewItem));
            AssertEvent (evs[1], "add", x => x.NewItem is TimeEntryHolder);

            // Events after second push
            AssertEvent (evs[2], "replace", x => IsDateHeader (x.NewItem));
            AssertEvent (evs[3], "add", x => x.NewItem is TimeEntryHolder);
        }

        [Test]
        public async void TestMoveSingle ()
        {
            var dt = new DateTime (2015, 12, 14, 19, 0, 0);
            var entries = new [] {
                CreateTimeEntry (dt), CreateTimeEntry (dt.AddMinutes (-10)), CreateTimeEntry (dt.AddMinutes (-20)),
                CreateTimeEntry (dt.AddMinutes (-30)), CreateTimeEntry (dt.AddDays (-1))
            };

            var feed = new TimeEntriesCollectionView.TestFeed ();
            var singleView = await TimeEntriesCollectionView.InitAdHoc (false, feed, entries);

            var evs = await GetEvents (3, singleView, () => {
                var entry = CreateTimeEntry (entries[2], -1); // Move entry to yesterday
                feed.Push (entry, DataAction.Put);
            });

            AssertEvent (evs[0], "replace", x => IsDateHeader (x.NewItem));  // Update today's header
            AssertEvent (evs[1], "replace", x => IsDateHeader (x.NewItem));  // Update yesterday's header
            AssertEvent (evs[2], "move", x => x.NewItem is TimeEntryHolder); // Move time entry
        }

        [Test]
        public async void TestReplaceSingle ()
        {
            var dt = new DateTime (2015, 12, 14, 19, 0, 0);
            var entries = new [] {
                CreateTimeEntry (dt), CreateTimeEntry (dt.AddMinutes (-10)), CreateTimeEntry (dt.AddMinutes (-20)),
                CreateTimeEntry (dt.AddMinutes (-30)), CreateTimeEntry (dt.AddDays (-1))
            };

            // Allow some buffer so pushes are handled at the same time
            var feed = new TimeEntriesCollectionView.TestFeed (100);
            var singleView = await TimeEntriesCollectionView.InitAdHoc (false, feed, entries);

            var evs = await GetEvents (3, singleView, () => {
                feed.Push (CreateTimeEntry (entries[0], 0, 2), DataAction.Put);
                feed.Push (CreateTimeEntry (entries[1], 0, 2), DataAction.Put);
                feed.Push (CreateTimeEntry (entries[2], 0, 2), DataAction.Put);
            });

            // The date header doesn't change because total duration remains the same
            // (mock entries' duration is always 1 minute)
            AssertEvent (evs[0], "replace", x => x.NewItem is TimeEntryHolder);
            AssertEvent (evs[1], "replace", x => x.NewItem is TimeEntryHolder);
            AssertEvent (evs[2], "replace", x => x.NewItem is TimeEntryHolder);
        }

        [Test]
        public async void TestChangeDateHeaderToFuture ()
        {
            var dt = new DateTime (2015, 12, 14, 10, 10, 11, 0);
            var entry1 = CreateTimeEntry (dt);
            var entry2 = CreateTimeEntry (dt.AddMinutes (-5));
            var feed = new TimeEntriesCollectionView.TestFeed ();
            var singleView = await TimeEntriesCollectionView.InitAdHoc (false, feed, entry1, entry2);

            // Order check before update
            var holderList = singleView.Data.ToList ();
            Assert.IsTrue (IsDateHeader (holderList [0]));
            Assert.AreEqual (((TimeEntryHolder)holderList [1]).Data.Id, entry1.Id);
            var dateValue = ((TimeEntriesCollectionView.DateHolder)holderList [0]).Date;

            var evs = await GetEvents (2, singleView, () =>
                // Move first entry to next day
                feed.Push (CreateTimeEntry (entry1, daysOffset: 1), DataAction.Put));

            // Check if date has changed
            holderList = singleView.Data.ToList ();
            var newDateValue = ((TimeEntriesCollectionView.DateHolder)holderList [0]).Date;
            Assert.AreNotEqual (dateValue, newDateValue);

            AssertEvent (evs[0], "add", x => IsDateHeader (x.NewItem));      // Add new header
            AssertEvent (evs[1], "move", x => x.NewItem is TimeEntryHolder); // Move time entry
        }


        [Test]
        public async void TestChangeDateHeaderToPast ()
        {
            var feed = new TimeEntriesCollectionView.TestFeed ();
            var singleView = await TimeEntriesCollectionView.InitAdHoc (false, feed);

            var entry1 = CreateTimeEntry (new DateTime (2015, 12, 14, 10, 10, 11, 0)); // First at list
            var entry2 = CreateTimeEntry (new DateTime (2015, 12, 14, 10, 10, 10, 0)); // Second at list
            feed.Push (entry1, DataAction.Put);
            feed.Push (entry2, DataAction.Put);

            await Task.Delay (100);

            // Order check before update
            var holderList = singleView.Data.ToList ();
            Assert.IsTrue (IsDateHeader (holderList [0]));
            Assert.AreEqual (((TimeEntryHolder)holderList [1]).Data.Id, entry1.Id);
            Assert.AreEqual (((TimeEntryHolder)holderList [2]).Data.Id, entry2.Id);
            var dateValue = ((TimeEntriesCollectionView.DateHolder)holderList [0]).Date;

            var evs = await GetEvents (5, singleView, () => {
                // Move first entry to previous day
                entry1.StartTime = entry1.StartTime.AddDays (-1);
                entry2.StartTime = entry1.StartTime.AddDays (-1);
                feed.Push (entry1, DataAction.Put);
                feed.Push (entry2, DataAction.Put);
            });

            // Order check after update
            holderList = singleView.Data.ToList ();
            var newDateValue = ((TimeEntriesCollectionView.DateHolder)holderList [0]).Date;
            Assert.AreNotEqual (dateValue, newDateValue);

            // Events after first push
            Assert.LessOrEqual (evs.Count, 2);
            AssertEvent (evs[0], "replace", x => IsDateHeader (x.NewItem));
        }

        [Test]
        public async void TestMoveToDifferentDate ()
        {
            var feed = new TimeEntriesCollectionView.TestFeed ();
            var singleView = await TimeEntriesCollectionView.InitAdHoc (false, feed);

            var entry1 = CreateTimeEntry (new DateTime (2015, 12, 14, 10, 10, 11, 0)); // First at list
            var entry2 = CreateTimeEntry (new DateTime (2015, 12, 14, 10, 10, 10, 0)); // Second at list
            var entry3 = CreateTimeEntry (new DateTime (2015, 12, 13, 10, 9, 9, 0));  // First at previous day
            feed.Push (entry1, DataAction.Put);
            feed.Push (entry2, DataAction.Put);
            feed.Push (entry3, DataAction.Put);

            await Task.Delay (100);

            // Order check before update
            var holderList = singleView.Data.ToList ();
            Assert.IsTrue (IsDateHeader (holderList [0]));
            Assert.AreEqual (((TimeEntryHolder)holderList [1]).Data.Id, entry1.Id);
            Assert.AreEqual (((TimeEntryHolder)holderList [2]).Data.Id, entry2.Id);
            Assert.IsTrue (IsDateHeader (holderList [3]));
            Assert.AreEqual (((TimeEntryHolder)holderList [4]).Data.Id, entry3.Id);

            var evs = await GetEvents (2, singleView, () => {
                // Move first entry to previous day
                entry1.StartTime = entry1.StartTime.AddDays (-1);
                feed.Push (entry1, DataAction.Put);
            });

            // Order check after update
            holderList = singleView.Data.ToList ();
            Assert.IsTrue (IsDateHeader (holderList [0]));
            Assert.AreEqual (((TimeEntryHolder)holderList [1]).Data.Id, entry2.Id);
            Assert.IsTrue (IsDateHeader (holderList [2]));
            Assert.AreEqual (((TimeEntryHolder)holderList [3]).Data.Id, entry1.Id);
            Assert.AreEqual (((TimeEntryHolder)holderList [4]).Data.Id, entry3.Id);

            // Events after first push
            AssertEvent (evs[0], "move", x => x.NewItem is TimeEntryHolder);
            AssertEvent (evs[1], "replace", x => IsDateHeader (x.NewItem));
            AssertEvent (evs[2], "replace", x => IsDateHeader (x.NewItem));
        }

        [Test]
        public async void TestMoveTopTimeEntryToDifferentDate ()
        {
            var feed = new TimeEntriesCollectionView.TestFeed ();
            var singleView = await TimeEntriesCollectionView.InitAdHoc (false, feed);

            var entry1 = CreateTimeEntry (new DateTime (2015, 12, 14, 10, 10, 11, 0)); // First at list
            var entry2 = CreateTimeEntry (new DateTime (2015, 12, 14, 10, 10, 10, 0)); // Second at list
            var entry3 = CreateTimeEntry (new DateTime (2015, 12, 14, 10, 10, 9, 0));  // Third at list
            var entry4 = CreateTimeEntry (new DateTime (2015, 12, 14, 10, 10, 9, 0));  // Fourth at list
            feed.Push (entry1, DataAction.Put);
            feed.Push (entry2, DataAction.Put);
            feed.Push (entry3, DataAction.Put);
            feed.Push (entry4, DataAction.Put);

            await Task.Delay (100);

            // Order check before update
            var holderList = singleView.Data.ToList ();
            Assert.IsTrue (IsDateHeader (holderList [0]));
            Assert.AreEqual (((TimeEntryHolder)holderList [1]).Data.Id, entry1.Id);
            Assert.AreEqual (((TimeEntryHolder)holderList [2]).Data.Id, entry2.Id);
            Assert.AreEqual (((TimeEntryHolder)holderList [3]).Data.Id, entry3.Id);
            Assert.AreEqual (((TimeEntryHolder)holderList [4]).Data.Id, entry4.Id);

            await GetEvents (2, singleView, () => {
                // Move first entry to previous day
                entry1.StartTime = entry1.StartTime.AddDays (-1);
                feed.Push (entry1, DataAction.Put);
            });

            // Order check after update
            holderList = singleView.Data.ToList ();
            Assert.IsTrue (IsDateHeader (holderList [0]));
            Assert.AreEqual (((TimeEntryHolder)holderList [1]).Data.Id, entry2.Id);
            Assert.AreEqual (((TimeEntryHolder)holderList [2]).Data.Id, entry3.Id);
            Assert.AreEqual (((TimeEntryHolder)holderList [3]).Data.Id, entry4.Id);
            Assert.IsTrue (IsDateHeader (holderList [4]));
            Assert.AreEqual (((TimeEntryHolder)holderList [5]).Data.Id, entry1.Id);

            // TODO: check events
        }

        [Test]
        public async void TestTripleTimeMovement ()
        {
            var feed = new TimeEntriesCollectionView.TestFeed ();
            var singleView = await TimeEntriesCollectionView.InitAdHoc (false, feed);

            var entry1 = CreateTimeEntry (new DateTime (2015, 12, 14, 10, 10, 11, 0)); // First at list
            var entry2 = CreateTimeEntry (new DateTime (2015, 12, 14, 10, 10, 10, 0)); // Second at list
            var entry3 = CreateTimeEntry (new DateTime (2015, 12, 14, 10, 10, 9, 0));  // Third at list
            feed.Push (entry1, DataAction.Put);
            feed.Push (entry2, DataAction.Put);
            feed.Push (entry3, DataAction.Put);

            await Task.Delay (100);

            await GetEvents (3, singleView, () => {
                // Move first entry to previous day
                entry1.StartTime = entry2.StartTime;
                entry2.StartTime = entry3.StartTime;
                entry3.StartTime = entry1.StartTime;
                feed.Push (entry1, DataAction.Put);
            });

            // Order check after update
            var holderList = singleView.Data.ToList ();
            Assert.IsTrue (IsDateHeader (holderList [0]));
            Assert.AreEqual (((TimeEntryHolder)holderList [1]).Data.Id, entry3.Id);
            Assert.AreEqual (((TimeEntryHolder)holderList [2]).Data.Id, entry1.Id);
            Assert.AreEqual (((TimeEntryHolder)holderList [3]).Data.Id, entry2.Id);

            // TODO: check events
        }
    }
}
