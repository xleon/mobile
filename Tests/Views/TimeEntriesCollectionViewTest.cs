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
        Guid userId;
        Guid workspaceId;

        public override void SetUp ()
        {
            base.SetUp ();

            userId = Guid.NewGuid ();
            workspaceId = Guid.NewGuid ();
            ServiceContainer.Register<IPlatformUtils> (new UpgradeManagerTest.PlatformUtils ());
        }

        // TODO: Extract this to a Test.Util class
        public TimeEntryData CreateTimeEntry (DateTime startTime,
                                              TimeEntryData prev = null,
                                              string desc = "Test entry",
                                              Guid taskId = default (Guid),
                                              Guid projId = default (Guid))
        {
            return new TimeEntryData () {
                Id = Guid.NewGuid (),
                StartTime = startTime,
                UserId = userId,
                WorkspaceId = workspaceId,
                TaskId = prev != null ? prev.TaskId : (taskId == Guid.Empty ? Guid.NewGuid () : taskId),
                ProjectId = prev != null ? prev.ProjectId : (projId == Guid.Empty ? Guid.NewGuid () : projId),
                Description = prev != null ? prev.Description : desc,
                State = TimeEntryState.Running,
//                IsBillable,
//                DurationOnly
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
            NotifyCollectionChangedEventArgs ev, string evType, Func<IHolder, IHolder, bool> additionalAssert = null)
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
                var newItem = ev.NewItems != null && ev.NewItems.Count > 0 ? ev.NewItems [0] as IHolder : null;
                var oldItem = ev.OldItems != null && ev.OldItems.Count > 0 ? ev.OldItems [0] as IHolder : null;
                Assert.IsTrue (additionalAssert (newItem, oldItem));
            }
        }

        private bool isDateHeader (IHolder x)
        {
            return x is TimeEntriesCollectionView.DateHolder;
        }

        [Test]
        public async void TestAddEntriesToSingle ()
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
            assert (evs[0], "add", (newItem, _) => isDateHeader (newItem));
            assert (evs[1], "add", (newItem, _) => newItem is TimeEntryHolder);

            // Events after second push
            assert (evs[2], "replace", (newItem, _) => isDateHeader (newItem));
            assert (evs[3], "add",     (newItem, _) => newItem is TimeEntryHolder);
        }
    }
}
