using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using XPlatUtils;

namespace Toggl.Phoebe.Data.Views
{
    /// <summary>
    /// This view returns the recent unique time entries.
    /// </summary>
    public class RecentTimeEntriesView : ObservableObject, IModelsView<TimeEntryModel>
    {
        private static string GetPropertyName<K> (Expression<Func<RecentTimeEntriesView, K>> expr)
        {
            return expr.ToPropertyName ();
        }

        private readonly int batchSize = 25;
        private int querySkip;
        private IModelQuery<TimeEntryModel> query;
        private readonly List<TimeEntryModel> data = new List<TimeEntryModel> ();
        private readonly object subscriptionModelChanged;

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

            if (data.Contains (entry)) {
                data.Sort ((a, b) => b.StartTime.CompareTo (a.StartTime));
                return;
            }

            var oldEntry = GetSimilar (entry);
            if (oldEntry != null) {
                if (oldEntry.StartTime >= entry.StartTime) {
                    // Newer version already exists in the dataset.
                    return;
                } else {
                    data.Remove (oldEntry);
                }
            }

            data.Add (entry);
            data.Sort ((a, b) => b.StartTime.CompareTo (a.StartTime));
        }

        private void ChangeDataAndNotify (Action change)
        {
            OnPropertyChanging (PropertyCount);
            OnPropertyChanging (PropertyModels);
            change ();
            OnPropertyChanged (PropertyModels);
            OnPropertyChanged (PropertyCount);
        }

        public void Reload ()
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

        private TimeEntryModel GetSimilar (TimeEntryModel entry)
        {
            return data.FirstOrDefault (
                (e) => e.Description == entry.Description
                && e.IsBillable == entry.IsBillable
                // TODO: Compare tags
                && e.TaskId == e.TaskId
                && e.ProjectId == e.ProjectId);
        }

        public void LoadMore ()
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
                            if (GetSimilar (entry) != null) {
                                continue;
                            }

                            data.Add (entry);
                        }
                    }
                });

                HasMore = hasData;
            } catch {
                // TODO: Log error
                HasError = true;
            }
        }

        public static readonly string PropertyModels = GetPropertyName ((m) => m.Models);

        public IEnumerable<TimeEntryModel> Models {
            get { return data; }
        }

        public static readonly string PropertyCount = GetPropertyName ((m) => m.Count);

        public long Count {
            get { return data.Count; }
        }

        public static readonly string PropertyTotalCount = GetPropertyName ((m) => m.TotalCount);

        public long? TotalCount {
            get { return null; }
        }

        private bool hasMore;
        public static readonly string PropertyHasMore = GetPropertyName ((m) => m.HasMore);

        public bool HasMore {
            get { return hasMore; }
            set {
                if (hasMore == value)
                    return;

                ChangePropertyAndNotify (PropertyHasMore, delegate {
                    hasMore = value;
                });
            }
        }

        public static readonly string PropertyIsLoading = GetPropertyName ((m) => m.IsLoading);

        public bool IsLoading {
            get { return false; }
        }

        private bool hasError;
        public static readonly string PropertyHasError = GetPropertyName ((m) => m.HasError);

        public bool HasError {
            get { return hasError; }
            set {
                if (hasError == value)
                    return;

                ChangePropertyAndNotify (PropertyHasError, delegate {
                    hasError = value;
                });
            }
        }
    }
}
