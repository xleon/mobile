using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using Toggl.Phoebe.Data;
using Toggl.Phoebe.Data.DataObjects;
using Toggl.Phoebe.Data.Views;
using Toggl.Phoebe.Net;
using XPlatUtils;
using Toggl.Phoebe.Data.Utils;

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
        public TimeEntryData create (DateTime startTime,
                                     string desc = "Test entry",
                                     Guid taskId = default (Guid),
                                     Guid projId = default (Guid))
        {
            return new TimeEntryData () {
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

        public TimeEntryData create (TimeEntryData prev, int daysOffset = 0, int minutesOffset = 0)
        {
            var startTime = prev.StartTime.AddDays (daysOffset).AddMinutes (minutesOffset);
            return new TimeEntryData () {
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

        Task<IList<NotifyCollectionChangedEventArgs>> GetEvents (
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

        private void assert (
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
                var evInfo = new EventInfo () {
                    NewItem = ev.NewItems != null && ev.NewItems.Count > 0 ? ev.NewItems [0] as IHolder : null,
                    NewIndex = ev.NewStartingIndex,
                    OldItem = ev.OldItems != null && ev.OldItems.Count > 0 ? ev.OldItems [0] as IHolder : null,
                    OldIndex = ev.OldStartingIndex
                };
                Assert.IsTrue (additionalAssert (evInfo));
            }
        }

        private bool isDateHeader (IHolder x)
        {
            return x is TimeEntriesCollectionView.DateHolder;
        }

        [Test]
        public async void TestAddSingle ()
        {
            var feed = new TimeEntriesCollectionView.TestFeed ();
            var singleView = await TimeEntriesCollectionView.InitAdHoc (false, feed);

            var evs = await GetEvents (4, singleView, () => {
                var entry1 = create (new DateTime (2015, 12, 14, 10, 0, 0, 0));
                var entry2 = create (new DateTime (2015, 12, 14, 11, 0, 0, 0));
                feed.Push (entry1, DataAction.Put);
                feed.Push (entry2, DataAction.Put);
            });

            // Events after first push
            assert (evs[0], "add", x => isDateHeader (x.NewItem));
            assert (evs[1], "add", x => x.NewItem is TimeEntryHolder);

            // Events after second push
            assert (evs[2], "replace", x => isDateHeader (x.NewItem));
            assert (evs[3], "add", x => x.NewItem is TimeEntryHolder);
        }

        [Test]
        public async void TestMoveSingle ()
        {
            var dt = new DateTime (2015, 12, 14, 19, 0, 0);
            var entries = new TimeEntryData[] {
                create (dt), create (dt.AddMinutes (-10)), create (dt.AddMinutes (-20)),
                create (dt.AddMinutes (-30)), create (dt.AddDays (-1))
            };

            var feed = new TimeEntriesCollectionView.TestFeed ();
            var singleView = await TimeEntriesCollectionView.InitAdHoc (false, feed, entries);

            var evs = await GetEvents (3, singleView, () => {
                var entry = create (entries[2], -1, 0); // Move entry to yesterday
                feed.Push (entry, DataAction.Put);
            });

            assert (evs[0], "replace", x => isDateHeader (x.NewItem));  // Update today's header
            assert (evs[1], "replace", x => isDateHeader (x.NewItem));  // Update yesterday's header
            assert (evs[2], "move", x => x.NewItem is TimeEntryHolder); // Move time entry
        }

        [Test]
        public async void TestReplaceSingle ()
        {
            var dt = new DateTime (2015, 12, 14, 19, 0, 0);
            var entries = new TimeEntryData[] {
                create (dt), create (dt.AddMinutes (-10)), create (dt.AddMinutes (-20)),
                create (dt.AddMinutes (-30)), create (dt.AddDays (-1))
            };

            // Allow some buffer so pushes are handled at the same time
            var feed = new TimeEntriesCollectionView.TestFeed (100);
            var singleView = await TimeEntriesCollectionView.InitAdHoc (false, feed, entries);

            var evs = await GetEvents (3, singleView, () => {
                feed.Push (create (entries[0], 0, 2), DataAction.Put);
                feed.Push (create (entries[1], 0, 2), DataAction.Put);
                feed.Push (create (entries[2], 0, 2), DataAction.Put);
            });

            // The date header doesn't change because total duration remains the same
            // (mock entries' duration is always 1 minute)
            assert (evs[0], "replace", x => x.NewItem is TimeEntryHolder);
            assert (evs[1], "replace", x => x.NewItem is TimeEntryHolder);
            assert (evs[2], "replace", x => x.NewItem is TimeEntryHolder);
        }
    }
}
