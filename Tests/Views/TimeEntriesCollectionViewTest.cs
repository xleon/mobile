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
        public TimeEntryData CreateTimeEntry (DateTime startTime = default (DateTime),
                                              string description = "Test entry",
                                              Guid taskId = default (Guid),
                                              Guid projectId = default (Guid))
        {
            return new TimeEntryData () {
                UserId = userId,
                WorkspaceId = workspaceId,
                TaskId = taskId == Guid.Empty ? Guid.NewGuid () : taskId,
                ProjectId = projectId == Guid.Empty ? Guid.NewGuid () : projectId,
                Description = description,
                StartTime = startTime == DateTime.MinValue ? DateTime.UtcNow : startTime,
                State = TimeEntryState.Running,
//                IsBillable,
//                DurationOnly
            };
        }

        Task<IList<NotifyCollectionChangedEventArgs>> GetEvents (
            INotifyCollectionChanged col, int evCount, Action action)
        {
            var i = 0;
            var tcs = new TaskCompletionSource<IList<NotifyCollectionChangedEventArgs>> ();
            var li = new List<NotifyCollectionChangedEventArgs> ();

            // TODO: Set also  a timeout
            col.CollectionChanged += (sender, e) => {
                li.Add (e);
                if (++i >= evCount) {
                    tcs.SetResult (li);
                }
            };
            action ();
            return tcs.Task;
        }

        [Test]
        public async void TestAddEntriesToSingle ()
        {
            var feed = new TimeEntriesCollectionView.TestFeed ();
            var singleView = TimeEntriesCollectionView.InitEmpty (isGrouped: false, feed: feed);
            var oldCount = singleView.Count;

            var evs = await GetEvents (singleView, 2, () => {
                var entry = CreateTimeEntry ();
                feed.Push (entry, DataAction.Put);
            });

            Assert.True (evs[0].Action == NotifyCollectionChangedAction.Add); // Date header
            Assert.True (evs[1].Action == NotifyCollectionChangedAction.Add); // Time entry
        }
    }
}

