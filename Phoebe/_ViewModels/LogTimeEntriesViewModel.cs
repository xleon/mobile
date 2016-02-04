using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using GalaSoft.MvvmLight;
using PropertyChanged;
using Toggl.Phoebe.Analytics;
using Toggl.Phoebe.Data.DataObjects;
using Toggl.Phoebe.Data.Models;
using Toggl.Phoebe.Data.Utils;
using Toggl.Phoebe.Net;
using Toggl.Phoebe._Data;
using Toggl.Phoebe._Data.Models;
using Toggl.Phoebe._Helpers;
using Toggl.Phoebe._Reactive;
using Toggl.Phoebe._ViewModels;
using Toggl.Phoebe._ViewModels.Timer;
using XPlatUtils;

namespace Toggl.Phoebe._ViewModels
{
    [ImplementPropertyChanged]
    public class LogTimeEntriesViewModel : ViewModelBase, IDisposable
    {
        private readonly Timer durationTimer;
        private Subscription<SettingChangedMessage> subscriptionSettingChanged;
        private Subscription<SyncFinishedMessage> subscriptionSyncFinished;
        private Subscription<UpdateFinishedMessage> subscriptionUpdateFinished;
        private TimeEntriesFeed collectionFeed;
        private readonly ActiveTimeEntryManager activeTimeEntryManager;

        LogTimeEntriesViewModel ()
        {
            // durationTimer will update the Duration value if ActiveTimeEntry is running
            durationTimer = new System.Timers.Timer ();
            durationTimer.Elapsed += DurationTimerCallback;

            ServiceContainer.Resolve<ITracker> ().CurrentScreen = "TimeEntryList Screen";
            activeTimeEntryManager = ServiceContainer.Resolve<ActiveTimeEntryManager> ();
            activeTimeEntryManager.PropertyChanged += OnActiveTimeEntryChanged;

            var bus = ServiceContainer.Resolve<MessageBus> ();
            subscriptionSettingChanged = bus.Subscribe<Toggl.Phoebe.Data.SettingChangedMessage> (OnSettingChanged);
//            subscriptionSyncFinished = bus.Subscribe<SyncFinishedMessage> (OnSyncFinished);
//            subscriptionUpdateFinished = bus.Subscribe<UpdateFinishedMessage> (OnUpdateItemsFinished);

            HasMoreItems = true;
            HasLoadErrors = false;
            HasItems = false;

            UpdateView (activeTimeEntryManager.ActiveTimeEntry);
            SyncCollectionView ();
        }

        public static LogTimeEntriesViewModel Init ()
        {
            var vm = new LogTimeEntriesViewModel ();
            return vm;
        }

        public void Dispose ()
        {
            var bus = ServiceContainer.Resolve<MessageBus> ();
            if (subscriptionSettingChanged != null) {
                bus.Unsubscribe (subscriptionSettingChanged);
                subscriptionSettingChanged = null;
            }
//            if (subscriptionSyncFinished != null) {
//                bus.Unsubscribe (subscriptionSyncFinished);
//                subscriptionSyncFinished = null;
//            }
//            if (subscriptionUpdateFinished != null) {
//                bus.Unsubscribe (subscriptionUpdateFinished);
//                subscriptionUpdateFinished = null;
//            }

            activeTimeEntryManager.PropertyChanged -= OnActiveTimeEntryChanged;
            durationTimer.Elapsed -= DurationTimerCallback;
            DisposeCollection ();
        }

        private void DisposeCollection ()
        {
            if (Collection != null) {
                Collection.CollectionChanged -= OnDetectHasItems;
                if (Collection is TimeEntriesCollection<TimeEntryHolder>) {
                    ((TimeEntriesCollection<TimeEntryHolder>)Collection).Dispose ();
                } else {
                    ((TimeEntriesCollection<TimeEntryGroup>)Collection).Dispose ();
                }
            }
        }

        #region Properties for ViewModel binding
        public bool IsProcessingAction { get; private set; }

        public bool IsAppSyncing { get; private set; }

        public bool IsTimeEntryRunning { get; private set; }

        public bool IsGroupedMode { get; private set; }

        public bool HasMoreItems { get; private set; }

        public bool HasLoadErrors { get; private set; }

        public bool HasItems { get; private set; }

        public string Description { get; set; }

        public string ProjectName { get; set; }

        public string Duration { get; private set; }

        public ObservableCollection<IHolder> Collection { get; private set; }
        #endregion

        #region Sync operations
        public void TriggerFullSync ()
        {
            IsAppSyncing = true;

            var syncManager = ServiceContainer.Resolve<ISyncManager> ();
            syncManager.Run ();
        }

        public void LoadMore ()
        {
            HasMoreItems = true;
            HasLoadErrors = false;
            RxChain.Send (DataTag.TimeEntryLoad);
        }
        #endregion

        #region Time entry operations
        public async Task ContinueTimeEntry (int index)
        {
            var newTimeEntry = new TimeEntryData ();
            var timeEntryHolder = Collection.ElementAt (index) as ITimeEntryHolder;

            if (timeEntryHolder == null) {
                return;
            }

            if (timeEntryHolder.Data.State == TimeEntryState.Running) {
                RxChain.Send (DataTag.TimeEntryStop, timeEntryHolder.Data);
                ServiceContainer.Resolve<ITracker>().SendTimerStopEvent (TimerStopSource.App);
            } else {
                RxChain.Send (DataTag.TimeEntryContinue, timeEntryHolder.Data);
                ServiceContainer.Resolve<ITracker>().SendTimerStartEvent (TimerStartSource.AppContinue);
            }
        }

        public async Task<TimeEntryData> StartStopTimeEntry ()
        {
            // Protect from double clicks?
            if (IsProcessingAction) {
                return activeTimeEntryManager.ActiveTimeEntry;
            }

            IsProcessingAction = true;

            var active = activeTimeEntryManager.ActiveTimeEntry;
            active = active.State == TimeEntryState.Running ? await TimeEntryModel.StopAsync (active) : await TimeEntryModel.StartAsync (active);

            IsProcessingAction = true;
            RxChain.Send (msgTag, active);
            // TODO: This must be at the end of the Reactive chain
            IsProcessingAction = false;

            if (active.State == TimeEntryState.Running) {
                ServiceContainer.Resolve<ITracker>().SendTimerStartEvent (TimerStartSource.AppNew);
            } else {
                ServiceContainer.Resolve<ITracker>().SendTimerStopEvent (TimerStopSource.App);
            }

            // Welcome wizard isn't needed after a time entry is started / stopped.
            ServiceContainer.Resolve<ISettingsStore> ().ShowWelcome = false;

            return active;
        }

        public Task RemoveTimeEntryAsync (int index)
        {
            var te = Collection.ElementAt (index) as ITimeEntryHolder;
            return TimeEntryModel.DeleteTimeEntryDataAsync (te.Data);
        }

        public TimeEntryData GetActiveTimeEntry ()
        {
            return activeTimeEntryManager.ActiveTimeEntry;
        }
        #endregion

        #region Extra operations
        public void ReportExperiment (int number, string actionKey, string actionValue)
        {
            if (!HasItems && ServiceContainer.Resolve<ISettingsStore> ().ShowWelcome) {
                OBMExperimentManager.Send (number, actionKey, actionValue);
            }
        }
        #endregion

        private void SyncCollectionView ()
        {
            DisposeCollection ();
            IsGroupedMode = ServiceContainer.Resolve<Toggl.Phoebe.Data.ISettingsStore> ().GroupedTimeEntries;

            collectionFeed = new TimeEntriesFeed ();
            Collection = IsGroupedMode
                         ?        (ObservableCollection<IHolder>)new TimeEntriesCollection<TimeEntryGroup> (collectionFeed)
                         : new TimeEntriesCollection<TimeEntryHolder> (collectionFeed);
            Collection.CollectionChanged += OnDetectHasItems;
        }

        private void UpdateView (TimeEntryData data)
        {
            ServiceContainer.Resolve<IPlatformUtils> ().DispatchOnUIThread (async () => {

                if (data.State == TimeEntryState.Running) {
                    Description = string.IsNullOrEmpty (data.Description) ? string.Empty : data.Description;
                    if (data.ProjectId != null) {
                        var prj = await TimeEntryModel.GetProjectDataAsync (data.ProjectId.Value);
                        ProjectName = prj.Name;
                    } else {
                        ProjectName = string.Empty;
                    }
                    IsTimeEntryRunning = true;
                    durationTimer.Start ();
                } else {
                    IsTimeEntryRunning = false;
                    Description = string.Empty;
                    ProjectName = string.Empty;
                    durationTimer.Stop ();
                    Duration = TimeSpan.FromSeconds (0).ToString ().Substring (0, 8);
                }
            });
        }

        private void OnActiveTimeEntryChanged (object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == ActiveTimeEntryManager.PropertyActiveTimeEntry) {
                UpdateView (activeTimeEntryManager.ActiveTimeEntry);
            }
        }

        private void OnSettingChanged (Toggl.Phoebe.Data.SettingChangedMessage msg)
        {
            // Implement a GetPropertyName
            if (msg.Name == "GroupedTimeEntries") {
                SyncCollectionView ();
            }
        }

        void OnLoadFinished (object sender, TimeEntryCollectionVM.LoadFinishedArgs args)
        {
            ServiceContainer.Resolve<IPlatformUtils> ().DispatchOnUIThread (() => {
                HasMoreItems = args.HasMore;
                HasLoadErrors = args.HasErrors;
            });
        }

//        private void OnSyncFinished (SyncFinishedMessage msg)
//        {
//            ServiceContainer.Resolve<IPlatformUtils> ().DispatchOnUIThread (() => {
//                IsAppSyncing = false;
//            });
//        }

        private void OnDetectHasItems (object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            ServiceContainer.Resolve<IPlatformUtils> ().DispatchOnUIThread (() => {
                HasItems = Collection.Count > 0;
            });
        }

        private void DurationTimerCallback (object sender, System.Timers.ElapsedEventArgs e)
        {
            var duration = TimeEntryModel.GetDuration (activeTimeEntryManager.ActiveTimeEntry, Time.UtcNow);  //model.GetDuration ();
            durationTimer.Interval = 1000 - duration.Milliseconds;

            // Update on UI Thread
            ServiceContainer.Resolve<IPlatformUtils> ().DispatchOnUIThread (() => {
                Duration = TimeSpan.FromSeconds (duration.TotalSeconds).ToString ().Substring (0, 8);
            });
        }
    }
}
