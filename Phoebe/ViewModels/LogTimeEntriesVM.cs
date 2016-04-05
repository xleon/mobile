﻿using System;
using System.Linq;
using System.Collections.ObjectModel;
using System.Reactive.Linq;
using GalaSoft.MvvmLight;
using PropertyChanged;
using Toggl.Phoebe.Analytics;
using Toggl.Phoebe.Data;
using Toggl.Phoebe.Data.Models;
using Toggl.Phoebe.Helpers;
using Toggl.Phoebe.Reactive;
using Toggl.Phoebe.ViewModels.Timer;
using XPlatUtils;
using System.Threading;

namespace Toggl.Phoebe.ViewModels
{
    [ImplementPropertyChanged]
    public class LogTimeEntriesVM : ViewModelBase, IDisposable
    {
        public class LoadInfoType
        {
            public bool IsSyncing { get; private set; }
            public bool HasMore { get; private set; }
            public bool HadErrors { get; private set; }

            public LoadInfoType (bool isSyncing, bool hasMore, bool hadErrors)
            {
                IsSyncing = isSyncing;
                HasMore = hasMore;
                HadErrors = hadErrors;
            }
        }

        private TimeEntryCollectionVM timeEntryCollection;
        private readonly IDisposable subscriptionState;
        private readonly SynchronizationContext uiContext;
        private IDisposable durationSubscriber;

        public bool IsFullSyncing { get; private set; }
        public bool HasSyncErrors { get; private set; }
        public bool IsGroupedMode { get; private set; }
        public string Duration { get; private set; }
        public bool IsEntryRunning { get; private set; }
        public LoadInfoType LoadInfo { get; private set; }
        public RichTimeEntry ActiveEntry { get; private set; }
        public ObservableCollection<IHolder> Collection { get { return timeEntryCollection; } }
        public IObservable<long> TimerObservable { get; private set; }

        public LogTimeEntriesVM (AppState appState)
        {
            ServiceContainer.Resolve<ITracker> ().CurrentScreen = "TimeEntryList Screen";

            uiContext = SynchronizationContext.Current;
            ResetCollection (appState.Settings.GroupedEntries);

            subscriptionState = StoreManager
                                .Singleton
                                .Observe (x => x.State)
                                .ObserveOn (uiContext)
                                .StartWith (appState)
            .DistinctUntilChanged (state => new { state.ActiveEntry, state.DownloadResult, state.FullSyncResult, state.Settings})
            .Subscribe (x => UpdateState (x.Settings, x.ActiveEntry, x.DownloadResult, x.FullSyncResult));

            // TODO: Rx find a better solution to force
            // an inmediate update using Rx code.
            UpdateState (appState.Settings, appState.ActiveEntry, appState.DownloadResult, appState.FullSyncResult);

            TimerObservable = Observable.Timer (TimeSpan.FromMilliseconds (1000 - Time.Now.Millisecond),
                                                TimeSpan.FromSeconds (1))
                              .ObserveOn (uiContext);
            durationSubscriber = TimerObservable.Subscribe (x => UpdateDuration ());

            // TODO: RX Review this line.
            // The ViewModel is created and start to load
            // content. This line was in the View before because
            // was an async method.
            LoadMore ();
        }

        private void ResetCollection (bool isGroupedMode)
        {
            DisposeCollection ();
            IsGroupedMode = isGroupedMode;
            timeEntryCollection = new TimeEntryCollectionVM (
                isGroupedMode ? TimeEntryGroupMethod.ByDateAndTask : TimeEntryGroupMethod.Single, uiContext);
        }

        public void Dispose ()
        {
            if (durationSubscriber != null) {
                durationSubscriber.Dispose ();
            }

            if (subscriptionState != null) {
                subscriptionState.Dispose ();
            }

            DisposeCollection ();
        }

        private void DisposeCollection ()
        {
            if (timeEntryCollection != null) {
                timeEntryCollection.Dispose ();
            }
        }

        public void TriggerFullSync ()
        {
            IsFullSyncing = true;
            HasSyncErrors = false;
            RxChain.Send (new DataMsg.FullSync ());
        }

        public void LoadMore ()
        {
            LoadInfo = new LoadInfoType (true, true, false);
            RxChain.Send (new DataMsg.TimeEntriesLoad ());
        }

        public void ContinueTimeEntry (int index)
        {
            var timeEntryHolder = timeEntryCollection.ElementAt (index) as ITimeEntryHolder;
            if (timeEntryHolder == null) {
                return;
            }

            if (timeEntryHolder.Entry.Data.State == TimeEntryState.Running) {
                RxChain.Send (new DataMsg.TimeEntryStop (timeEntryHolder.Entry.Data));
                ServiceContainer.Resolve<ITracker> ().SendTimerStopEvent (TimerStopSource.App);
            } else {
                RxChain.Send (new DataMsg.TimeEntryContinue (timeEntryHolder.Entry.Data));
                ServiceContainer.Resolve<ITracker> ().SendTimerStartEvent (TimerStartSource.AppContinue);
            }

            // Set ShowWelcome setting to false.
            RxChain.Send (new DataMsg.UpdateSetting (nameof (SettingsState.ShowWelcome),false));
        }

        public void StartStopTimeEntry (bool startedByFAB = false)
        {
            // TODO RX: Protect from requests in short time (double click...)?
            var entry = ActiveEntry.Data;
            if (entry.State == TimeEntryState.Running) {
                RxChain.Send (new DataMsg.TimeEntryStop (entry));
                ServiceContainer.Resolve<ITracker> ().SendTimerStartEvent (TimerStartSource.AppNew);
            } else {
                RxChain.Send (new DataMsg.TimeEntryContinue (entry, startedByFAB));
                ServiceContainer.Resolve<ITracker> ().SendTimerStopEvent (TimerStopSource.App);
            }
        }

        public void RemoveTimeEntry (int index)
        {
            // TODO: Add analytic event
            var te = Collection.ElementAt (index) as ITimeEntryHolder;
            RxChain.Send (new DataMsg.TimeEntriesRemove (te.Entry.Data));
        }

        #region Extra
        public void ReportExperiment (string actionKey, string actionValue)
        {
            if (Collection.Count == 0 && StoreManager.Singleton.AppState.Settings.ShowWelcome) {
                OBMExperimentManager.Send (actionKey, actionValue, StoreManager.Singleton.AppState.User);
            }
        }

        public bool IsInExperiment ()
        {
            return OBMExperimentManager.IncludedInExperiment (StoreManager.Singleton.AppState.User);
        }

        public bool IsWelcomeMessageShown ()
        {
            return StoreManager.Singleton.AppState.Settings.ShowWelcome;
        }
        #endregion

        private void UpdateState (SettingsState settings, RichTimeEntry activeTimeEntry, DownloadResult downloadResults, FullSyncResult fullsyncReturn)
        {
            if (settings.GroupedEntries != IsGroupedMode) {
                ResetCollection (settings.GroupedEntries);
            }

            // Check full Sync info
            HasSyncErrors = fullsyncReturn.HadErrors;
            IsFullSyncing = fullsyncReturn.IsSyncing;

            // Check if DownloadResult has changed
            var newLoadInfo = new LoadInfoType (
                downloadResults.IsSyncing,
                downloadResults.HasMore,
                downloadResults.HadErrors
            );

            if (LoadInfo == null ||
                    (LoadInfo.HadErrors != newLoadInfo.HadErrors ||
                     LoadInfo.HasMore != newLoadInfo.HasMore ||
                     LoadInfo.IsSyncing != newLoadInfo.IsSyncing)) {
                LoadInfo = newLoadInfo;
            }
            // Don't update ActiveEntry if both ActiveEntry and appState.ActiveEntry are empty
            if (ActiveEntry == null || ! (ActiveEntry.Data.Id == Guid.Empty && activeTimeEntry.Data.Id == Guid.Empty)) {
                ActiveEntry = activeTimeEntry;
                IsEntryRunning = ActiveEntry.Data.State == TimeEntryState.Running;
            }

            UpdateDuration ();
        }

        private void UpdateDuration ()
        {
            if (IsEntryRunning) {
                Duration = string.Format ("{0:D2}:{1:mm}:{1:ss}", (int)ActiveEntry.Data.GetDuration ().TotalHours, ActiveEntry.Data.GetDuration ());
            } else {
                Duration = TimeSpan.FromSeconds (0).ToString ().Substring (0, 8);
            }
        }
    }
}