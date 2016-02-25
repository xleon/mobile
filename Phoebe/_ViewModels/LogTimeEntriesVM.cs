using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
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
using Toggl.Phoebe._ViewModels.Timer;
using XPlatUtils;

namespace Toggl.Phoebe._ViewModels
{
    [ImplementPropertyChanged]
    public class LogTimeEntriesVM : ViewModelBase, IDisposable
    {
        private Subscription<Toggl.Phoebe.Data.SettingChangedMessage> subscriptionSettingChanged;
        private readonly System.Timers.Timer durationTimer;
        private readonly ActiveTimeEntryManager activeTimeEntryManager;
        private readonly IDisposable subscriptionState;

        private object __timerStateLock = new object ();
        private TimerState __timerStateUnsafe;
        private TimerState timerState
        {
            get {
                lock (__timerStateLock) {
                    return __timerStateUnsafe;
                }
            }
            set {
                lock (__timerStateLock) {
                    __timerStateUnsafe = value;
                }
            }
        }

        public LogTimeEntriesVM (TimerState timerState)
        {
            this.timerState = timerState;

            // durationTimer will update the Duration value if ActiveTimeEntry is running
            durationTimer = new System.Timers.Timer ();
            durationTimer.Elapsed += DurationTimerCallback;

            ServiceContainer.Resolve<ITracker> ().CurrentScreen = "TimeEntryList Screen";
            activeTimeEntryManager = ServiceContainer.Resolve<ActiveTimeEntryManager> ();
            activeTimeEntryManager.PropertyChanged += OnActiveTimeEntryChanged;

            var bus = ServiceContainer.Resolve<MessageBus> ();
            subscriptionSettingChanged = bus.Subscribe<Toggl.Phoebe.Data.SettingChangedMessage> (OnSettingChanged);
            subscriptionState = StoreManager.Singleton
                                            .Observe (app => app.TimerState)
                                            .Subscribe (OnStateUpdated);

            HasMoreItems = true;
            HasLoadErrors = false;
            HasItems = false;

            UpdateView (activeTimeEntryManager.ActiveTimeEntry);
            SyncCollectionView ();
        }

        public void Dispose ()
        {
            var bus = ServiceContainer.Resolve<MessageBus> ();
            if (subscriptionSettingChanged != null) {
                bus.Unsubscribe (subscriptionSettingChanged);
                subscriptionSettingChanged = null;
            }
            if (subscriptionState != null) {
                subscriptionState.Dispose ();
            }
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
            RxChain.Send (new DataMsg.TimeEntriesLoad ());
        }
        #endregion

        #region Time entry operations
        public void ContinueTimeEntry (int index)
        {
            var newTimeEntry = new TimeEntryData ();
            var timeEntryHolder = Collection.ElementAt (index) as ITimeEntryHolder;

            if (timeEntryHolder == null) {
                return;
            }

            if (timeEntryHolder.Data.State == TimeEntryState.Running) {
                RxChain.Send (new DataMsg.TimeEntryStop (timeEntryHolder.Data));
                ServiceContainer.Resolve<ITracker> ().SendTimerStopEvent (TimerStopSource.App);
            }
            else {
                RxChain.Send (new DataMsg.TimeEntryContinue (timeEntryHolder.Data));
                ServiceContainer.Resolve<ITracker> ().SendTimerStartEvent (TimerStartSource.AppContinue);
            }
        }

        public TimeEntryData StartStopTimeEntry ()
        {
            // Protect from double clicks?
            if (IsProcessingAction) {
                return activeTimeEntryManager.ActiveTimeEntry;
            }

            IsProcessingAction = true;

            var active = activeTimeEntryManager.ActiveTimeEntry;
            active = active.State == TimeEntryState.Running ? await TimeEntryModel.StopAsync (active) : await TimeEntryModel.StartAsync (active);

            IsProcessingAction = true;
            var active = activeTimeEntryManager.ActiveTimeEntry;
            if (active.State == TimeEntryState.Running) {
                RxChain.Send (new DataMsg.TimeEntryStop (active));
            }
            else {
                RxChain.Send (new DataMsg.TimeEntryContinue (active));
            }

            if (activeTimeEntryManager.IsRunning) {
                ServiceContainer.Resolve<ITracker> ().SendTimerStartEvent (TimerStartSource.AppNew);
            }
            else {
                ServiceContainer.Resolve<ITracker> ().SendTimerStopEvent (TimerStopSource.App);
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

        public void RestoreItemFromUndo ()
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

            Collection = new TimeEntryCollectionVM (
                IsGroupedMode ? TimeEntryGroupMethod.Single : TimeEntryGroupMethod.ByDateAndTask);
        }

        private void UpdateView (TimeEntryData data)
        {
            ServiceContainer.Resolve<IPlatformUtils> ().DispatchOnUIThread (() => {
                // Check if an entry is running.
                if (isRunning) {
                    TimeEntryInfo info = timerState.TimeEntries[data.Id].Info;
                    Description = data.Description;
                    ProjectName = info.ProjectData != null ? info.ProjectData.Name : string.Empty;
                    IsTimeEntryRunning = true;
                    durationTimer.Start ();
                }
                else {
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

        private void OnStateUpdated (TimerState timerState)
        {
            var info = timerState.DownloadInfo;
            this.timerState = timerState;

            ServiceContainer.Resolve<IPlatformUtils> ().DispatchOnUIThread (() => {
                IsProcessingAction = false;
                IsAppSyncing = info.IsSyncing;
                HasMoreItems = info.HasMore;
                HasLoadErrors = info.HadErrors;
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
