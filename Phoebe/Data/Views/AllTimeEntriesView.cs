using System;
using XPlatUtils;
using Toggl.Phoebe.Data.Models;
using Toggl.Phoebe.Net;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace Toggl.Phoebe.Data.Views
{
    /// <summary>
    /// This view combines IModelStore data and data from ITogglClient for time views. It tries to load data from
    /// web, but always falls back to data from the local store.
    /// </summary>
    public class AllTimeEntriesView : IDataView<TimeEntryModel>, IDisposable
    {
        private static readonly string Tag = "AllTimeEntriesView";
        private readonly List<TimeEntryModel> data = new List<TimeEntryModel> ();
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
                if (!data.Contains (entry)) {
                    data.Add (entry);
                    data.Sort ((a, b) => b.StartTime.CompareTo (a.StartTime));
                    OnUpdated ();
                } else if (msg.PropertyName == TimeEntryModel.PropertyStartTime) {
                    data.Sort ((a, b) => b.StartTime.CompareTo (a.StartTime));
                    OnUpdated ();
                }
            } else if (msg.PropertyName == TimeEntryModel.PropertyDeletedAt) {
                if (data.Contains (entry)) {
                    data.Remove (entry);
                    OnUpdated ();
                }
            }
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
            data.Clear ();
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

        public IEnumerable<TimeEntryModel> Data {
            get { return data; }
        }

        public long Count {
            get { return data.Count; }
        }

        public bool HasMore { get; private set; }

        public bool IsLoading { get; private set; }
    }
}
