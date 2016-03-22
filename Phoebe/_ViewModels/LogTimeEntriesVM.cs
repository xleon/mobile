using System;
using System.Linq;
using System.Collections.ObjectModel;
using System.Reactive.Linq;
using GalaSoft.MvvmLight;
using PropertyChanged;
using Toggl.Phoebe.Analytics;
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
        private readonly System.Timers.Timer durationTimer;
        private readonly IDisposable subscriptionSettings, subscriptionState;

        public bool IsGroupedMode { get; private set; }
        public string Duration { get; private set; }
        public bool IsEntryRunning { get; private set; }
        public LoadInfoType LoadInfo { get; private set; }
        public RichTimeEntry ActiveEntry { get; private set; }
        public ObservableCollection<IHolder> Collection { get { return timeEntryCollection; } }

        public LogTimeEntriesVM (AppState appState)
        {
            // durationTimer will update the Duration value if ActiveTimeEntry is running
            durationTimer = new System.Timers.Timer ();
            durationTimer.Elapsed += DurationTimerCallback;

            ServiceContainer.Resolve<ITracker> ().CurrentScreen = "TimeEntryList Screen";

            ResetCollection (appState.Settings.GroupedEntries);
            subscriptionState = StoreManager
                                .Singleton
                                .Observe (x => x.State)
                                .StartWith (appState)
                                .Scan<AppState, Tuple<AppState, DownloadResult>> (
                                    null, (tuple, state) => Tuple.Create (state, tuple != null ? tuple.Item2 : null))
                                .Subscribe (tuple => UpdateState (tuple.Item1, tuple.Item2));

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
                isGroupedMode ? TimeEntryGroupMethod.ByDateAndTask : TimeEntryGroupMethod.Single);
        }

        public void Dispose ()
        {
            if (subscriptionSettings != null) {
                subscriptionSettings.Dispose ();
            }

            if (subscriptionState != null) {
                subscriptionState.Dispose ();
            }

            durationTimer.Elapsed -= DurationTimerCallback;
            DisposeCollection ();
        }

        private void DisposeCollection ()
        {
            if (timeEntryCollection != null) {
                timeEntryCollection.Dispose ();
            }
        }

        public void LoadMore (bool fullSync = false)
        {
            ServiceContainer.Resolve<IPlatformUtils> ().DispatchOnUIThread (() => {
                LoadInfo = new LoadInfoType (true, true, false);
                RxChain.Send (new DataMsg.TimeEntriesLoad (fullSync));
            });
        }

        public void ContinueTimeEntry (int index)
        {
            var timeEntryHolder = timeEntryCollection.Data.ElementAt (index) as ITimeEntryHolder;
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
            RxChain.Send (new DataMsg.TimeEntryDelete (te.Entry.Data));
        }

        public void ReportExperiment (string actionKey, string actionValue)
        {
            if (Collection.Count == 0 && StoreManager.Singleton.AppState.Settings.ShowWelcome) {
                OBMExperimentManager.Send (actionKey, actionValue, StoreManager.Singleton.AppState.User);
            }
        }

        private void UpdateState (AppState appState, DownloadResult prevDownloadResult)
        {
            ServiceContainer.Resolve<IPlatformUtils> ().DispatchOnUIThread (() => {
                if (appState.Settings.GroupedEntries != IsGroupedMode) {
                    ResetCollection (appState.Settings.GroupedEntries);
                }

                // Check if DownloadResult has changed
                if (LoadInfo == null || prevDownloadResult != appState.DownloadResult) {
                    LoadInfo = new LoadInfoType (
                        appState.DownloadResult.IsSyncing,
                        appState.DownloadResult.HasMore,
                        appState.DownloadResult.HadErrors
                    );
                }

                // Don't update ActiveEntry if both ActiveEntry and appState.ActiveEntry are empty
                if (ActiveEntry == null || !(ActiveEntry.Data.Id == Guid.Empty && appState.ActiveEntry.Data.Id == Guid.Empty)) {
                    ActiveEntry = appState.ActiveEntry;
                    IsEntryRunning = ActiveEntry.Data.State == TimeEntryState.Running;
                    // Check if an entry is running.
                    if (IsEntryRunning && !durationTimer.Enabled) {
                        durationTimer.Start ();
                    }
                    else if (!IsEntryRunning && durationTimer.Enabled) {
                        durationTimer.Stop ();
                        Duration = TimeSpan.FromSeconds (0).ToString ().Substring (0, 8);
                    }
                }
            });
        }

        private void DurationTimerCallback (object sender, System.Timers.ElapsedEventArgs e)
        {
            // Update on UI Thread
            ServiceContainer.Resolve<IPlatformUtils> ().DispatchOnUIThread (() => {
                var duration = ActiveEntry.Data.GetDuration ();
                durationTimer.Interval = 1000 - duration.Milliseconds;
                Duration = string.Format ("{0:D2}:{1:mm}:{1:ss}", (int)duration.TotalHours, duration);
            });
        }
    }
}
