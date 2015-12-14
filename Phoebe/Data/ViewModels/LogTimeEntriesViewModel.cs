using System;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;
using GalaSoft.MvvmLight;
using PropertyChanged;
using Toggl.Phoebe.Analytics;
using Toggl.Phoebe.Data.DataObjects;
using Toggl.Phoebe.Data.Models;
using Toggl.Phoebe.Data.Utils;
using Toggl.Phoebe.Data.ViewModels;
using Toggl.Phoebe.Net;
using XPlatUtils;

namespace Toggl.Phoebe.Data.ViewModels
{
    [ImplementPropertyChanged]
    public class LogTimeEntriesViewModel : ViewModelBase, IDisposable
    {
        private readonly Timer durationTimer;
        private Subscription<SettingChangedMessage> subscriptionSettingChanged;
        private Subscription<SyncFinishedMessage> subscriptionSyncFinished;
        private Subscription<UpdateFinishedMessage> subscriptionUpdateFinished;
        private ActiveTimeEntryManager timeEntryManager;
        private TimeEntryModel model;
        private TimeEntriesFeed collectionFeed;

        LogTimeEntriesViewModel ()
        {
            // durationTimer will update the Duration value if ActiveTimeEntry is running
            durationTimer = new Timer ();
            durationTimer.Elapsed += DurationTimerCallback;

            ServiceContainer.Resolve<ITracker> ().CurrentScreen = "TimeEntryList Screen";
            timeEntryManager = ServiceContainer.Resolve<ActiveTimeEntryManager> ();
            timeEntryManager.PropertyChanged += OnActiveTimeEntryManagerPropertyChanged;

            var bus = ServiceContainer.Resolve<MessageBus> ();
            subscriptionSettingChanged = bus.Subscribe<SettingChangedMessage> (OnSettingChanged);
            subscriptionSyncFinished = bus.Subscribe<SyncFinishedMessage> (OnSyncFinished);
            subscriptionUpdateFinished = bus.Subscribe<UpdateFinishedMessage> (OnUpdateItemsFinished);
        }

        public static async Task<LogTimeEntriesViewModel> Init ()
        {
            var vm = new LogTimeEntriesViewModel ();
            await vm.SyncModel ();
            vm.SyncCollectionView ();
            return vm;
        }

        public void Dispose ()
        {
            var bus = ServiceContainer.Resolve<MessageBus> ();
            if (subscriptionSettingChanged != null) {
                bus.Unsubscribe (subscriptionSettingChanged);
                subscriptionSettingChanged = null;
            }
            if (subscriptionSyncFinished != null) {
                bus.Unsubscribe (subscriptionSyncFinished);
                subscriptionSyncFinished = null;
            }
            if (subscriptionUpdateFinished != null) {
                bus.Unsubscribe (subscriptionUpdateFinished);
                subscriptionUpdateFinished = null;
            }

            DisposeCollection ();

            timeEntryManager.PropertyChanged -= OnActiveTimeEntryManagerPropertyChanged;
            timeEntryManager = null;
            model = null;
        }

        private void DisposeCollection ()
        {
            if (collectionFeed != null) {
                collectionFeed.Dispose ();
                collectionFeed = null;
            }

            if (Collection != null) {
                Collection.Dispose ();
                Collection = null;
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

        public ICollectionData<IHolder> Collection { get; private set; }
        #endregion

        #region Sync operations
        public void TriggerFullSync ()
        {
            IsAppSyncing = true;

            var syncManager = ServiceContainer.Resolve<ISyncManager> ();
            syncManager.Run ();
        }

        public async Task LoadMore ()
        {
            HasMoreItems = true;
            HasLoadErrors = false;

            var startDate = await collectionFeed.LoadMore ();
            var syncManager = ServiceContainer.Resolve<ISyncManager> ();
            syncManager.RunTimeEntriesUpdate (startDate, TimeEntriesFeed.DaysLoad);
        }
        #endregion

        #region Time entry operations
        public async Task ContinueTimeEntryAsync (int index)
        {
            var timeEntryHolder = Collection.Data.ElementAt (index) as ITimeEntryHolder;
            if (timeEntryHolder == null) {
                return;
            }

            if (timeEntryHolder.Data.State == TimeEntryState.Running) {
                await TimeEntryModel.StopAsync (timeEntryHolder.Data);
                ServiceContainer.Resolve<ITracker>().SendTimerStopEvent (TimerStopSource.App);
            } else {
                await TimeEntryModel.ContinueTimeEntryDataAsync (timeEntryHolder.Data);
                ServiceContainer.Resolve<ITracker>().SendTimerStartEvent (TimerStartSource.AppContinue);
            }
        }

        public async Task<TimeEntryData> StartStopTimeEntry ()
        {
            // Protect from double clicks
            if (IsProcessingAction) {
                return model.Data;
            }

            if (model.State == TimeEntryState.Running) {
                await model.StopAsync ();
            } else {
                await model.StartAsync ();
            }

            IsProcessingAction = false;

            return model.Data;
        }

        public Task RemoveItemWithUndoAsync (int index)
        {
            return collectionFeed.RemoveItemWithUndoAsync (
                       Collection.Data.ElementAt (index) as ITimeEntryHolder);
        }

        public void RestoreItemFromUndo()
        {
            collectionFeed.RestoreItemFromUndo ();
        }

        public TimeEntryData GetActiveTimeEntry ()
        {
            return model.Data;
        }
        #endregion

        private async Task SyncModel ()
        {
            var data = timeEntryManager.Active;
            if (data != null) {
                model = new TimeEntryModel (data);
                await model.LoadAsync ();
                UpdateView ();
            }
        }

        private void SyncCollectionView ()
        {
            DisposeCollection ();
            IsGroupedMode = ServiceContainer.Resolve<ISettingsStore> ().GroupedTimeEntries;

            collectionFeed = new TimeEntriesFeed ();
            var col = new TimeEntriesCollection (collectionFeed, IsGroupedMode);
            Collection = col;
        }

        private void UpdateView ()
        {
            ServiceContainer.Resolve<IPlatformUtils> ().DispatchOnUIThread (() => {
                Description = model.Description;
                ProjectName = model.Project != null ? model.Project.Name : string.Empty;

                // Check if an entry is running.
                if (model.State == TimeEntryState.Running && !IsTimeEntryRunning) {
                    IsTimeEntryRunning = true;
                    durationTimer.Start ();
                } else if (model.State != TimeEntryState.Running) {
                    IsTimeEntryRunning = false;
                    durationTimer.Stop ();
                    Duration = TimeSpan.FromSeconds (0).ToString ().Substring (0, 8);
                }
            });
        }

        private async void OnActiveTimeEntryManagerPropertyChanged (object sender, PropertyChangedEventArgs args)
        {
            if (args.PropertyName == ActiveTimeEntryManager.PropertyActive) {
                await SyncModel ();
            }
        }

        private void OnSettingChanged (SettingChangedMessage msg)
        {
            // Implement a GetPropertyName
            if (msg.Name == "GroupedTimeEntries") {
                SyncCollectionView ();
            }
        }

        private void OnSyncFinished (SyncFinishedMessage msg)
        {
            ServiceContainer.Resolve<IPlatformUtils> ().DispatchOnUIThread (() => {
                IsAppSyncing = false;
            });
        }

        private void OnUpdateItemsFinished (UpdateFinishedMessage msg)
        {
            ServiceContainer.Resolve<IPlatformUtils> ().DispatchOnUIThread (() => {
                HasMoreItems = msg.HadMore;
                HasLoadErrors = msg.HadErrors;
            });
        }

        private void DurationTimerCallback (object sender, ElapsedEventArgs e)
        {
            var duration = model.GetDuration ();
            durationTimer.Interval = 1000 - duration.Milliseconds;

            // Update on UI Thread
            ServiceContainer.Resolve<IPlatformUtils> ().DispatchOnUIThread (() => {
                Duration = TimeSpan.FromSeconds (duration.TotalSeconds).ToString ().Substring (0, 8);
            });
        }
    }
}