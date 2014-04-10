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
    /// This view combines IModelStore data and data from ITogglClient for time views. It tries to load data from
    /// web, but always falls back to data from the local store.
    /// </summary>
    public class AllTimeEntriesView : IDataView<object>, IDisposable
    {
        private static readonly string Tag = "AllTimeEntriesView";
        private readonly List<DateGroup> dateGroups = new List<DateGroup> ();
        private DateTime startFrom;
        private Subscription<ModelChangedMessage> subscriptionModelChanged;

        public AllTimeEntriesView ()
        {
            var bus = ServiceContainer.Resolve<MessageBus> ();
            subscriptionModelChanged = bus.Subscribe<ModelChangedMessage> (OnModelChanged);

            HasMore = true;
            Reload ();
        }

        public void Dispose ()
        {
            var bus = ServiceContainer.Resolve<MessageBus> ();
            if (subscriptionModelChanged != null) {
                bus.Unsubscribe (subscriptionModelChanged);
                subscriptionModelChanged = null;
            }
        }

        private void OnModelChanged (ModelChangedMessage msg)
        {
            if (!msg.Model.IsShared)
                return;
            var entry = msg.Model as TimeEntryModel;
            if (entry == null)
                return;

            if (entry.DeletedAt == null && entry.State != TimeEntryState.New) {
                var grp = FindGroupWith (entry);
                if (grp == null) {
                    grp = GetGroupFor (entry);
                    grp.Models.Add (entry);
                    grp.UpdateTotalDuration ();
                    Sort ();
                    OnUpdated ();
                } else if (msg.PropertyName == TimeEntryModel.PropertyStartTime) {
                    // Check that the entry is still in the correct group:
                    var date = entry.StartTime.ToLocalTime ().Date;
                    if (grp.Date != date) {
                        // Need to move entry:
                        grp.Models.Remove (entry);
                        grp.UpdateTotalDuration ();

                        grp = GetGroupFor (entry);
                        grp.Models.Add (entry);
                    }
                    grp.UpdateTotalDuration ();
                    Sort ();
                    OnUpdated ();
                } else if (msg.PropertyName == TimeEntryModel.PropertyStopTime) {
                    if (grp.UpdateTotalDuration ()) {
                        OnUpdated ();
                    }
                }
            } else if (msg.PropertyName == TimeEntryModel.PropertyDeletedAt) {
                var grp = FindGroupWith (entry);
                if (grp != null) {
                    grp.Models.Remove (entry);
                    grp.UpdateTotalDuration ();
                    if (grp.Models.Count == 0) {
                        dateGroups.Remove (grp);
                    }
                    OnUpdated ();
                }
            }
        }

        private DateGroup FindGroupWith (TimeEntryModel model)
        {
            return dateGroups.FirstOrDefault (g => g.Models.Contains (model));
        }

        private DateGroup GetGroupFor (TimeEntryModel model)
        {
            var date = model.StartTime.ToLocalTime ().Date;
            var grp = dateGroups.FirstOrDefault (g => g.Date == date);
            if (grp == null) {
                grp = new DateGroup (date);
                dateGroups.Add (grp);
            }
            return grp;
        }

        private void Sort ()
        {
            foreach (var grp in dateGroups) {
                grp.Models.Sort ((a, b) => b.StartTime.CompareTo (a.StartTime));
            }
            dateGroups.Sort ((a, b) => b.Date.CompareTo (a.Date));
        }

        public event EventHandler Updated;

        private void OnUpdated ()
        {
            var handler = Updated;
            if (handler != null) {
                handler (this, EventArgs.Empty);
            }
        }

        public void Reload ()
        {
            if (IsLoading)
                return;

            startFrom = DateTime.UtcNow;
            dateGroups.Clear ();
            HasMore = true;

            LoadMore ();
        }

        public async void LoadMore ()
        {
            if (IsLoading || !HasMore)
                return;

            IsLoading = true;
            var client = ServiceContainer.Resolve<ITogglClient> ();
            OnUpdated ();

            try {
                var endTime = startFrom;
                var startTime = startFrom = endTime - TimeSpan.FromDays (4);

                bool useLocal = false;


                // Try with latest data from server first:
                if (!useLocal) {
                    const int numDays = 5;
                    try {
                        var minStart = endTime;
                        var entries = await client.ListTimeEntries (endTime, numDays);
                        foreach (var entry in entries) {
                            // OnModelChanged catches the newly created time entries and adds them to the dataset

                            if (entry.StartTime < minStart) {
                                minStart = entry.StartTime;
                            }
                        }

                        startTime = minStart;
                        HasMore = (endTime.Date - minStart.Date).Days > 0;
                    } catch (Exception exc) {
                        var log = ServiceContainer.Resolve<Logger> ();
                        if (exc is System.Net.Http.HttpRequestException) {
                            log.Info (Tag, exc, "Failed to fetch time entries {1} days up to {0}", endTime, numDays);
                        } else {
                            log.Warning (Tag, exc, "Failed to fetch time entries {1} days up to {0}", endTime, numDays);
                        }

                        useLocal = true;
                    }
                }

                // Fall back to local data:
                if (useLocal) {
                    var entries = Model.Query<TimeEntryModel> (
                                      (te) => te.StartTime <= endTime && te.StartTime > startTime && te.State != TimeEntryState.New)
                        .NotDeleted ()
                        .ForCurrentUser ();
                    foreach (var entry in entries) {
                        // OnModelChanged catches the newly created time entries and adds them to the dataset
                    }

                    HasMore = Model.Query<TimeEntryModel> ((te) => te.StartTime <= startTime && te.State != TimeEntryState.New).Count () > 0;
                }
            } catch (Exception exc) {
                var log = ServiceContainer.Resolve<Logger> ();
                log.Error (Tag, exc, "Failed to fetch time entries");
            } finally {
                IsLoading = false;
                OnUpdated ();
            }
        }

        public IEnumerable<object> Data {
            get {
                foreach (var grp in dateGroups) {
                    yield return grp;
                    foreach (var model in grp.Models) {
                        yield return model;
                    }
                }
            }
        }

        public long Count {
            get { return dateGroups.Count + dateGroups.Sum (g => g.Models.Count); }
        }

        public bool HasMore { get; private set; }

        public bool IsLoading { get; private set; }

        public class DateGroup : ObservableObject
        {
            private static string GetPropertyName<T> (Expression<Func<DateGroup, T>> expr)
            {
                return expr.ToPropertyName ();
            }

            private readonly DateTime date;
            private readonly List<TimeEntryModel> models = new List<TimeEntryModel> ();
            private TimeSpan totalDuration;

            public DateGroup (DateTime date)
            {
                this.date = date.Date;
            }

            public bool UpdateTotalDuration ()
            {
                var duration = TimeSpan.FromSeconds (models.Sum (m => m.GetDuration ().TotalSeconds));
                if (totalDuration != duration) {
                    ChangePropertyAndNotify (PropertyTotalDuration, delegate {
                        totalDuration = duration;
                    });
                    return true;
                }
                return false;
            }

            public DateTime Date {
                get { return date; }
            }

            public static readonly string PropertyTotalDuration = GetPropertyName ((m) => m.TotalDuration);

            public TimeSpan TotalDuration {
                get { return totalDuration; }
            }

            public List<TimeEntryModel> Models {
                get { return models; }
            }
        }
    }
}
