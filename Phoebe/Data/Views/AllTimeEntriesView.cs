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
    public class AllTimeEntriesView : ModelsView<TimeEntryModel>
    {
        private static readonly string Tag = "AllTimeEntriesView";

        private static string GetPropertyName<K> (Expression<Func<AllTimeEntriesView, K>> expr)
        {
            return expr.ToPropertyName ();
        }

        private DateTime startFrom;
        private readonly List<TimeEntryModel> data = new List<TimeEntryModel> ();
        #pragma warning disable 0414
        private readonly Subscription<ModelChangedMessage> subscriptionModelChanged;
        #pragma warning restore 0414

        public AllTimeEntriesView ()
        {
            var bus = ServiceContainer.Resolve<MessageBus> ();
            subscriptionModelChanged = bus.Subscribe<ModelChangedMessage> (OnModelChanged);
            HasMore = true;
            Reload ();
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
                    ChangeDataAndNotify (delegate {
                        data.Add (entry);
                        data.Sort ((a, b) => b.StartTime.CompareTo (a.StartTime));
                    });
                } else if (msg.PropertyName == TimeEntryModel.PropertyStartTime) {
                    ChangeDataAndNotify (delegate {
                        data.Sort ((a, b) => b.StartTime.CompareTo (a.StartTime));
                    });
                }
            } else if (msg.PropertyName == TimeEntryModel.PropertyDeletedAt) {
                if (data.Contains (entry)) {
                    ChangeDataAndNotify (delegate {
                        data.Remove (entry);
                    });
                }
            }
        }

        public override void Reload ()
        {
            if (IsLoading)
                return;

            startFrom = DateTime.UtcNow;
            ChangeDataAndNotify (delegate {
                data.Clear ();
            });
            HasMore = true;

            LoadMore ();
        }

        private void ChangeDataAndNotify (Action change)
        {
            OnPropertyChanging (PropertyCount);
            OnPropertyChanging (PropertyModels);
            change ();
            OnPropertyChanged (PropertyModels);
            OnPropertyChanged (PropertyCount);
        }

        public async override void LoadMore ()
        {
            if (IsLoading || !HasMore)
                return;

            IsLoading = true;
            var client = ServiceContainer.Resolve<ITogglClient> ();
            HasError = false;

            try {
                var endTime = startFrom;
                var startTime = startFrom = endTime - TimeSpan.FromDays (4);

                bool useLocal = false;

                OnPropertyChanging (PropertyCount);
                OnPropertyChanging (PropertyModels);

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

                        HasError = true;
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

                OnPropertyChanged (PropertyModels);
                OnPropertyChanged (PropertyCount);
            } catch (Exception exc) {
                var log = ServiceContainer.Resolve<Logger> ();
                log.Error (Tag, exc, "Failed to fetch time entries");

                HasError = true;
            } finally {
                IsLoading = false;
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
