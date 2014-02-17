using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
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

        private readonly int batchSize = 25;
        private int querySkip;
        private IModelQuery<TimeEntryModel> query;
        private readonly List<Group> data = new List<Group> ();
        #pragma warning disable 0414
        private readonly Subscription<ModelChangedMessage> subscriptionModelChanged;
        #pragma warning restore 0414

        public RecentTimeEntriesView (int batchSize = 25)
        {
            var bus = ServiceContainer.Resolve<MessageBus> ();
            subscriptionModelChanged = bus.Subscribe<ModelChangedMessage> (OnModelChanged);
            Reload ();
        }

        private void OnModelChanged (ModelChangedMessage msg)
        {
            if (!msg.Model.IsShared)
                return;
            var entry = msg.Model as TimeEntryModel;
            if (entry == null)
                return;

            var grp = FindGroup (entry);

            if (grp != null) {
                if (msg.PropertyName == TimeEntryModel.PropertyStartTime) {
                    // Update group and resort data:
                    ChangeDataAndNotify (delegate {
                        grp.Update (entry);
                        Sort ();
                    });
                } else if (msg.PropertyName == TimeEntryModel.PropertyDescription
                           || msg.PropertyName == TimeEntryModel.PropertyIsBillable
                           || msg.PropertyName == TimeEntryModel.PropertyTaskId
                           || msg.PropertyName == TimeEntryModel.PropertyProjectId) {
                    ChangeDataAndNotify (delegate {
                        // Remove from old group:
                        grp.Remove (entry);
                        if (grp.IsEmpty) {
                            data.Remove (grp);
                        }

                        // Add entry to correct group
                        Add (entry);
                        Sort ();
                    });
                }
                return;
            }

            var authManager = ServiceContainer.Resolve<AuthManager> ();
            if (entry.UserId != authManager.UserId)
                return;

            ChangeDataAndNotify (delegate {
                Add (entry);
                Sort ();
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

        public override void Reload ()
        {
            // TODO: Add support for multiple workspaces
            query = Model.Query<TimeEntryModel> ()
                .NotDeleted ()
                .ForCurrentUser ()
                .OrderBy ((e) => e.StartTime, false);
            querySkip = 0;

            ChangeDataAndNotify (delegate {
                data.Clear ();
            });

            LoadMore ();
        }

        private void Sort ()
        {
            data.Sort ((a, b) => b.RecentStartTime.CompareTo (a.RecentStartTime));
        }

        private Group FindGroup (TimeEntryModel entry)
        {
            return data.FirstOrDefault ((g) => g.Contains (entry));
        }

        private bool Contains (TimeEntryModel entry)
        {
            return FindGroup (entry) != null;
        }

        private void Add (TimeEntryModel entry)
        {
            var grp = data.FirstOrDefault ((g) => g.CanContain (entry));
            if (grp == null) {
                grp = new Group (entry);
                data.Add (grp);
            } else {
                grp.Add (entry);
            }
        }

        public override void LoadMore ()
        {
            int oldCount = data.Count;
            bool hasData = true;
            HasError = false;

            try {
                ChangeDataAndNotify (delegate {
                    while (hasData && oldCount + batchSize > data.Count) {
                        var q = query.Skip (querySkip).Take (batchSize);
                        querySkip += batchSize;
                        hasData = false;

                        // Find unique time entries and add them to the list
                        foreach (var entry in q) {
                            hasData = true;

                            if (Contains (entry)) {
                                continue;
                            }
                            Add (entry);
                        }
                    }

                    Sort ();
                });

                HasMore = hasData;
            } catch (Exception exc) {
                var log = ServiceContainer.Resolve<Logger> ();
                log.Error (Tag, exc, "Failed to compose recent time entries");

                HasError = true;
            }
        }

        public override IEnumerable<TimeEntryModel> Models {
            get { return data.Where ((g) => !g.IsEmpty).Select ((g) => g.First ()); }
        }

        public override long Count {
            get { return data.Count; }
        }

        private class Group : IEnumerable<TimeEntryModel>
        {
            private readonly string description;
            private readonly bool isBillable;
            private readonly Guid? taskId;
            private readonly Guid? projectId;
            private readonly List<Guid> tags;
            private readonly List<GroupItem> items = new List<GroupItem> ();

            private static IEnumerable<Guid> GetTags (TimeEntryModel entry)
            {
                return entry.Tags
                        .Where ((m) => m.To.Name != TimeEntryModel.DefaultTag)
                        .Select ((e) => e.ToId.Value);
            }

            public Group (TimeEntryModel entry)
            {
                description = entry.Description;
                isBillable = entry.IsBillable;
                taskId = entry.TaskId;
                projectId = entry.ProjectId;
                tags = GetTags (entry).ToList ();

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
                    || isBillable != entry.IsBillable
                    || taskId != entry.TaskId
                    || projectId != entry.ProjectId)
                    return false;

                // Check tags:
                if (tags.Intersect (GetTags (entry)).Any ())
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
