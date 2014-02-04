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
        private static string GetPropertyName<K> (Expression<Func<RecentTimeEntriesView, K>> expr)
        {
            return expr.ToPropertyName ();
        }

        private readonly int batchSize = 25;
        private int querySkip;
        private IModelQuery<TimeEntryModel> query;
        private readonly List<TimeEntryModel> data = new List<TimeEntryModel> ();
        #pragma warning disable 0414
        private readonly object subscriptionModelChanged;
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

            if (data.Contains (entry)) {
                ChangeDataAndNotify (delegate {
                    data.Sort ((a, b) => b.StartTime.CompareTo (a.StartTime));
                });
                return;
            }

            var authManager = ServiceContainer.Resolve<AuthManager> ();
            if (entry.UserId != authManager.UserId)
                return;

            var oldEntry = GetSimilar (entry);
            if (oldEntry != null) {
                if (oldEntry.StartTime >= entry.StartTime) {
                    // Newer version already exists in the dataset.
                    return;
                } else {
                    ChangeDataAndNotify (delegate {
                        data.Remove (oldEntry);
                    });
                }
            }

            ChangeDataAndNotify (delegate {
                data.Add (entry);
                data.Sort ((a, b) => b.StartTime.CompareTo (a.StartTime));
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

        private TimeEntryModel GetSimilar (TimeEntryModel entry)
        {
            return data.FirstOrDefault (
                (e) => e.Description == entry.Description
                && e.IsBillable == entry.IsBillable
                && e.TaskId == e.TaskId
                && e.ProjectId == e.ProjectId
                && HasSameTags (e, entry));
        }

        private static List<Guid?> GetTimeEntryTagIds (TimeEntryModel entry)
        {
            return entry.Tags
                    .Where ((m) => m.To.Name != TimeEntryModel.DefaultTag)
                    .Select ((m) => m.ToId)
                    .ToList ();
        }

        private static bool HasSameTags (TimeEntryModel a, TimeEntryModel b)
        {
            var at = GetTimeEntryTagIds (a);
            var bt = GetTimeEntryTagIds (b);
            return !at.Union (bt).Except (bt).Any ();
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

        public override IEnumerable<TimeEntryModel> Models {
            get { return data; }
        }

        public override long Count {
            get { return data.Count; }
        }
    }
}
