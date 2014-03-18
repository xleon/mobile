using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Toggl.Phoebe.Data.Models;
using Toggl.Phoebe.Net;
using XPlatUtils;

namespace Toggl.Phoebe.Data.Views
{
    /// <summary>
    /// This view returns the recent unique time entries.
    /// </summary>
    public class RecentTimeEntriesView : ModelsView<TimeEntryModel>
    {
        private static readonly string Tag = "RecentTimeEntriesView";

        private static string GetPropertyName<K> (Expression<Func<RecentTimeEntriesView, K>> expr)
        {
            return expr.ToPropertyName ();
        }

        private DateTime queryStartDate;
        private GroupContainer data = new GroupContainer ();
        private Subscription<ModelChangedMessage> subscriptionModelChanged;
        private Subscription<SyncFinishedMessage> subscriptionSyncFinished;

        public RecentTimeEntriesView ()
        {
            Reload ();
        }

        private void OnModelChanged (ModelChangedMessage msg)
        {
            if (!msg.Model.IsShared)
                return;
            var entry = msg.Model as TimeEntryModel;
            if (entry == null)
                return;

            var grp = data.FindGroup (entry);

            if (grp != null) {
                if (msg.PropertyName == TimeEntryModel.PropertyStartTime) {
                    if (entry.StartTime >= queryStartDate) {
                        // Update group and resort data:
                        ChangeDataAndNotify (delegate {
                            grp.Update (entry);
                            data.Sort ();
                        });
                    } else {
                        // Out side of date range, remove from list
                        grp.Remove (entry);
                        if (grp.IsEmpty) {
                            data.Remove (grp);
                        } else {
                            data.Sort ();
                        }
                    }
                } else if (msg.PropertyName == TimeEntryModel.PropertyDescription
                           || msg.PropertyName == TimeEntryModel.PropertyTaskId
                           || msg.PropertyName == TimeEntryModel.PropertyProjectId) {
                    ChangeDataAndNotify (delegate {
                        // Remove from old group:
                        grp.Remove (entry);
                        if (grp.IsEmpty) {
                            data.Remove (grp);
                        }

                        // Add entry to correct group
                        data.Add (entry);
                        data.Sort ();
                    });
                } else if (msg.PropertyName == TimeEntryModel.PropertyDeletedAt
                           || msg.PropertyName == TimeEntryModel.PropertyIsPersisted) {
                    if (!entry.IsPersisted || entry.DeletedAt.HasValue) {
                        ChangeDataAndNotify (delegate {
                            grp.Remove (entry);
                            if (grp.IsEmpty) {
                                data.Remove (grp);
                            } else {
                                data.Sort ();
                            }
                        });
                    }
                }
                return;
            }

            // Prevent showing of non-persisted, deleted entries and entries outside of the date range:
            if (!entry.IsPersisted || entry.DeletedAt.HasValue || entry.State == TimeEntryState.New || entry.StartTime < queryStartDate)
                return;

            var authManager = ServiceContainer.Resolve<AuthManager> ();
            if (entry.UserId != authManager.UserId)
                return;

            ChangeDataAndNotify (delegate {
                data.Add (entry);
                data.Sort ();
            });
        }

        private void ChangeDataAndNotify (Action change)
        {
            OnPropertyChanging (PropertyCount);
            OnPropertyChanging (PropertyModels);
            change ();
            OnPropertyChanged (PropertyModels);
            OnPropertyChanged (PropertyCount);
        }

        public override async void Reload ()
        {
            if (IsLoading)
                return;

            var bus = ServiceContainer.Resolve<MessageBus> ();
            if (subscriptionModelChanged != null) {
                bus.Unsubscribe (subscriptionModelChanged);
                subscriptionModelChanged = null;
            }

            ChangeDataAndNotify (delegate {
                data.Clear ();
            });

            IsLoading = true;

            // Group only items in the past 9 days
            queryStartDate = DateTime.UtcNow - TimeSpan.FromDays (9);
            var query = Model.Query<TimeEntryModel> ()
                .NotDeleted ()
                .ForCurrentUser ()
                .Where ((e) => e.State != TimeEntryState.New && e.StartTime >= queryStartDate)
                .OrderBy ((e) => e.StartTime, false);

            // Get new data
            var groups = await Task.Factory.StartNew (() => FromQuery (query));
            ChangeDataAndNotify (delegate {
                data = groups;
            });
            subscriptionModelChanged = bus.Subscribe<ModelChangedMessage> (OnModelChanged);

            // Determine if sync is running, if so, delay setting IsLoading to false
            var syncManager = ServiceContainer.Resolve<SyncManager> ();
            if (!syncManager.IsRunning) {
                IsLoading = false;
            } else {
                subscriptionSyncFinished = bus.Subscribe<SyncFinishedMessage> (OnSyncFinished);
            }
        }

        private void OnSyncFinished (SyncFinishedMessage msg)
        {
            IsLoading = false;

            if (subscriptionSyncFinished != null) {
                var bus = ServiceContainer.Resolve<MessageBus> ();
                bus.Unsubscribe (subscriptionSyncFinished);
                subscriptionSyncFinished = null;
            }
        }

        public override void LoadMore ()
        {
        }

        private static GroupContainer FromQuery (IModelQuery<TimeEntryModel> query)
        {
            var groups = new GroupContainer ();

            try {
                // Find unique time entries and add them to the list
                foreach (var entry in query) {
                    if (groups.Contains (entry)) {
                        continue;
                    }
                    groups.Add (entry);
                }

                groups.Sort ();
            } catch (Exception exc) {
                var log = ServiceContainer.Resolve<Logger> ();
                log.Error (Tag, exc, "Failed to compose recent time entries");
            }
            return groups;
        }

        public override IEnumerable<TimeEntryModel> Models {
            get { return data.Where ((g) => !g.IsEmpty).Select ((g) => g.First ()); }
        }

        public override long Count {
            get { return data.Count; }
        }

        private class GroupContainer : IEnumerable<Group>
        {
            readonly List<Group> data = new List<Group> ();

            public void Sort ()
            {
                data.Sort ((a, b) => b.RecentStartTime.CompareTo (a.RecentStartTime));
            }

            public Group FindGroup (TimeEntryModel entry)
            {
                return data.FirstOrDefault ((g) => g.Contains (entry));
            }

            public bool Contains (TimeEntryModel entry)
            {
                return FindGroup (entry) != null;
            }

            public void Add (TimeEntryModel entry)
            {
                var grp = data.FirstOrDefault ((g) => g.CanContain (entry));
                if (grp == null) {
                    grp = new Group (entry);
                    data.Add (grp);
                } else {
                    grp.Add (entry);
                }
            }

            public void Clear ()
            {
                data.Clear ();
            }

            public bool Remove (Group item)
            {
                return data.Remove (item);
            }

            public IEnumerator<Group> GetEnumerator ()
            {
                return data.GetEnumerator ();
            }

            System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator ()
            {
                return GetEnumerator ();
            }

            public int Count {
                get { return data.Count; }
            }
        }

        private class Group : IEnumerable<TimeEntryModel>
        {
            private readonly string description;
            private readonly Guid? taskId;
            private readonly Guid? projectId;
            private readonly List<GroupItem> items = new List<GroupItem> ();

            public Group (TimeEntryModel entry)
            {
                description = entry.Description;
                taskId = entry.TaskId;
                projectId = entry.ProjectId;

                Add (entry);
            }

            public DateTime RecentStartTime {
                get {
                    if (items.Count < 1)
                        return DateTime.MinValue;
                    return items [0].StartTime;
                }
            }

            public bool IsEmpty {
                get { return items.Count < 1; }
            }

            public bool CanContain (TimeEntryModel entry)
            {
                // Check data:
                if (description != entry.Description
                    || taskId != entry.TaskId
                    || projectId != entry.ProjectId)
                    return false;

                return true;
            }

            public void Add (TimeEntryModel entry)
            {
                items.Add (new GroupItem (entry));
                items.Sort ((a, b) => b.StartTime.CompareTo (a.StartTime));
            }

            public void Update (TimeEntryModel entry)
            {
                var item = items.FirstOrDefault ((gi) => gi.Id == entry.Id);
                if (item == null)
                    return;

                item.Update (entry);
                items.Sort ((a, b) => b.StartTime.CompareTo (a.StartTime));
            }

            public void Remove (TimeEntryModel entry)
            {
                items.RemoveAll ((gi) => gi.Id == entry.Id);
            }

            public bool Contains (TimeEntryModel entry)
            {
                return items.Any ((gi) => gi.Id == entry.Id);
            }

            public IEnumerator<TimeEntryModel> GetEnumerator ()
            {
                return items.Select ((gi) => Model.ById<TimeEntryModel> (gi.Id)).GetEnumerator ();
            }

            System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator ()
            {
                return GetEnumerator ();
            }
        }

        private class GroupItem
        {
            public GroupItem (TimeEntryModel entry)
            {
                Id = entry.Id.Value;
                StartTime = entry.StartTime;
            }

            public void Update (TimeEntryModel entry)
            {
                if (entry == null)
                    throw new ArgumentNullException ("entry");
                if (entry.Id != Id)
                    throw new ArgumentException ("Entry with invalid Id given.", "entry");
                StartTime = entry.StartTime;
            }

            public Guid Id { get; private set; }

            public DateTime StartTime { get; private set; }
        }
    }
}
