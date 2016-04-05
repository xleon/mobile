using System;
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

            Duration = TimeSpan.FromSeconds (0).ToString ().Substring (0, 8);
            uiContext = SynchronizationContext.Current;
            ResetCollection (appState.Settings.GroupedEntries);
            subscriptionState = StoreManager
                                .Singleton
                                .Observe (x => x.State)
                                .StartWith (appState)
                                .ObserveOn (uiContext)
                                .Scan<AppState, Tuple<AppState, DownloadResult>> (
                                    null, (tuple, state) => Tuple.Create (state, tuple != null ? tuple.Item2 : null))
                                .Subscribe (tuple => UpdateState (tuple.Item1, tuple.Item2));

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

        private void UpdateState (AppState appState, DownloadResult prevDownloadResult)
        {
            if (appState.Settings.GroupedEntries != IsGroupedMode) {
                ResetCollection (appState.Settings.GroupedEntries);
            }

            // Check full Sync info
            HasSyncErrors = appState.FullSyncResult.HadErrors;
            IsFullSyncing = appState.FullSyncResult.IsSyncing;

            // Check if DownloadResult has changed
            if (LoadInfo == null || prevDownloadResult != appState.DownloadResult) {
                LoadInfo = new LoadInfoType (
                    appState.DownloadResult.IsSyncing,
                    appState.DownloadResult.HasMore,
                    appState.DownloadResult.HadErrors
                );
            }

            // Don't update ActiveEntry if both ActiveEntry and appState.ActiveEntry are empty
            if (ActiveEntry == null || ! (ActiveEntry.Data.Id == Guid.Empty && appState.ActiveEntry.Data.Id == Guid.Empty)) {
                ActiveEntry = appState.ActiveEntry;
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
