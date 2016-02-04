using System;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using GalaSoft.MvvmLight;
using PropertyChanged;
using Toggl.Phoebe.Analytics;
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
        private Subscription<Toggl.Phoebe.Data.SettingChangedMessage> subscriptionSettingChanged;
//        private Subscription<SyncFinishedMessage> subscriptionSyncFinished;
//        private Subscription<UpdateFinishedMessage> subscriptionUpdateFinished;
        private readonly System.Timers.Timer durationTimer;
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

            UpdateView (activeTimeEntryManager.IsRunning, activeTimeEntryManager.ActiveTimeEntry);
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
                Collection.Dispose ();
            }
        }

        #region Properties for ViewModel binding
        public bool IsProcessingAction { get; private set; }

        public bool IsAppSyncing { get; private set; }

        public bool IsTimeEntryRunning { get; private set; }

        public bool IsGroupedMode { get; private set; }

        public bool HasMoreItems { get; private set; }

        public bool HasLoadErrors { get; private set; }

        public string Description { get; set; }

        public string ProjectName { get; set; }

        public string Duration { get; private set; }

        public TimeEntryCollectionVM Collection { get; private set; }
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
            var timeEntryHolder = Collection.Data.ElementAt (index) as ITimeEntryHolder;
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

            var active = activeTimeEntryManager.ActiveTimeEntry;
            var msgTag = active.State == TimeEntryState.Running
                ? DataTag.TimeEntryStop : DataTag.TimeEntryStart;

            IsProcessingAction = true;
            RxChain.Send (msgTag, active);
            // TODO: This must be at the end of the Reactive chain
            IsProcessingAction = false;

            if (activeTimeEntryManager.IsRunning) {
                ServiceContainer.Resolve<ITracker>().SendTimerStartEvent (TimerStartSource.AppNew);
            } else {
                ServiceContainer.Resolve<ITracker>().SendTimerStopEvent (TimerStopSource.App);
            }

            return active;
        }

        public void RemoveItemWithUndo (int index)
        {
            Collection.RemoveTimeEntryWithUndo (
                Collection.Data.ElementAt (index) as ITimeEntryHolder);
        }

        public void RestoreItemFromUndo()
        {
            Collection.RestoreTimeEntryFromUndo ();
        }

        public TimeEntryData GetActiveTimeEntry ()
        {
            return activeTimeEntryManager.ActiveTimeEntry;
        }
        #endregion

        private void SyncCollectionView ()
        {
            DisposeCollection ();
            IsGroupedMode = ServiceContainer.Resolve<Toggl.Phoebe.Data.ISettingsStore> ().GroupedTimeEntries;

            Collection = new TimeEntryCollectionVM (
                IsGroupedMode ? TimeEntryGroupMethod.Single : TimeEntryGroupMethod.ByDateAndTask);
            Collection.LoadFinished += OnLoadFinished;
        }

        private void UpdateView (bool isRunning, TimeEntryData data)
        {
            ServiceContainer.Resolve<IPlatformUtils> ().DispatchOnUIThread (async () => {
                // Check if an entry is running.
                if (isRunning) {
                    Toggl.Phoebe._Data.Models.TimeEntryInfo info = null;  //TODO TODO TODO await StoreRegister.LoadTimeEntryInfoAsync (data);
                    Description = info.Description;
                    ProjectName = info.ProjectData != null ? info.ProjectData.Name : string.Empty;
                    IsTimeEntryRunning = true;
                    durationTimer.Start ();
                } else {
                    Description = string.Empty;
                    ProjectName = string.Empty;
                    IsTimeEntryRunning = false;
                    durationTimer.Stop ();
                    Duration = TimeSpan.FromSeconds (0).ToString ().Substring (0, 8);
                }
            });
        }

        private void OnActiveTimeEntryChanged (object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == ActiveTimeEntryManager.PropertyIsRunning) {
                UpdateView (activeTimeEntryManager.IsRunning, activeTimeEntryManager.ActiveTimeEntry);
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

//        private void OnUpdateItemsFinished (UpdateFinishedMessage msg)
//        {
//            ServiceContainer.Resolve<IPlatformUtils> ().DispatchOnUIThread (() => {
//                HasMoreItems = msg.HadMore;
//                HasLoadErrors = msg.HadErrors;
//            });
//        }

        private void DurationTimerCallback (object sender, System.Timers.ElapsedEventArgs e)
        {
            var duration = activeTimeEntryManager.ActiveTimeEntry.GetDuration (Time.UtcNow);  //model.GetDuration ();
            durationTimer.Interval = 1000 - duration.Milliseconds;

            // Update on UI Thread
            ServiceContainer.Resolve<IPlatformUtils> ().DispatchOnUIThread (() => {
                Duration = TimeSpan.FromSeconds (duration.TotalSeconds).ToString ().Substring (0, 8);
            });
        }
    }
}