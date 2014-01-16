using System;
using XPlatUtils;
using Toggl.Phoebe.Net;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace Toggl.Phoebe.Data.Views
{
    /// <summary>
    /// This view combines IModelStore data and data from ITogglClient for time views. It tries to load data from
    /// web, but always falls back to data from the local store.
    /// </summary>
    public class AllTimeEntriesView : ObservableObject, IModelsView<TimeEntryModel>
    {
        private static string GetPropertyName<K> (Expression<Func<AllTimeEntriesView, K>> expr)
        {
            return expr.ToPropertyName ();
        }

        private DateTime startFrom;
        private readonly List<TimeEntryModel> data = new List<TimeEntryModel> ();
        private readonly object subscriptionModelChanged;

        public AllTimeEntriesView ()
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

            if (!data.Contains (entry)) {
                data.Add (entry);
                data.Sort ((a, b) => b.StartTime.CompareTo (a.StartTime));
            } else if (msg.PropertyName == TimeEntryModel.PropertyStartTime) {
                data.Sort ((a, b) => b.StartTime.CompareTo (a.StartTime));
            }
        }

        public void Reload ()
        {
            if (IsLoading)
                return;

            startFrom = DateTime.UtcNow;
            ChangeDataAndNotify (delegate {
                data.Clear ();
            });

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

        public async void LoadMore ()
        {
            if (IsLoading)
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
                    try {
                        var entries = await client.ListTimeEntries (startTime, endTime);
                        foreach (var entry in entries) {
                            // OnModelChanged catches the newly created time entries and adds them to the dataset
                        }
                    } catch {
                        // TODO: Log error
                        HasError = true;
                        useLocal = true;
                    }
                }

                // Fall back to local data:
                if (useLocal) {
                    var entries = Model.Query<TimeEntryModel> (
                                      (te) => te.StartTime <= endTime && te.StartTime > startTime)
                        .NotDeleted ()
                        .ForCurrentUser ();
                    foreach (var entry in entries) {
                        // OnModelChanged catches the newly created time entries and adds them to the dataset
                    }
                }

                OnPropertyChanged (PropertyModels);
                OnPropertyChanged (PropertyCount);
            } catch {
                // TODO: Log error
                HasError = true;
            } finally {
                IsLoading = false;
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

        public static readonly string PropertyHasMore = GetPropertyName ((m) => m.HasMore);

        public bool HasMore {
            get { return true; }
        }

        private bool loading;
        public static readonly string PropertyIsLoading = GetPropertyName ((m) => m.IsLoading);

        public bool IsLoading {
            get { return loading; }
            set {
                if (loading == value)
                    return;

                ChangePropertyAndNotify (PropertyIsLoading, delegate {
                    loading = value;
                });
            }
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
