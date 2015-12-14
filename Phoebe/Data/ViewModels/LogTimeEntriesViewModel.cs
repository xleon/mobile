using System;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Timers;
using GalaSoft.MvvmLight;
using PropertyChanged;
using Toggl.Phoebe.Analytics;
using Toggl.Phoebe.Data.DataObjects;
using Toggl.Phoebe.Data.Models;
using Toggl.Phoebe.Data.ViewModels;
using Toggl.Phoebe.Data.Views;
using XPlatUtils;

namespace Toggl.Phoebe.Data.ViewModels
{
    [ImplementPropertyChanged]
    public class LogTimeEntriesViewModel : ViewModelBase, IDisposable
    {
        private Subscription<SettingChangedMessage> subscriptionSettingChanged;
        private ActiveTimeEntryManager timeEntryManager;
        private TimeEntryModel model;
        private Timer durationTimer;

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
        }

        public static async Task<LogTimeEntriesViewModel> Init ()
        {
            var vm = new LogTimeEntriesViewModel ();
            await vm.SyncModel ();
            await vm.SyncCollectionView ();
            return vm;
        }

        public void Dispose ()
        {
            var bus = ServiceContainer.Resolve<MessageBus> ();
            if (subscriptionSettingChanged != null) {
                bus.Unsubscribe (subscriptionSettingChanged);
                subscriptionSettingChanged = null;
            }

            if (CollectionView != null) {
                CollectionView.Dispose ();
                CollectionView = null;
            }

            timeEntryManager.PropertyChanged -= OnActiveTimeEntryManagerPropertyChanged;
            timeEntryManager = null;
            model = null;
        }

        #region Properties for ViewModel binding
        public bool IsProcessingAction { get; set; }

        public bool IsTimeEntryRunning { get; set; }

        public bool IsGroupedMode { get; set; }

        public string Description { get; set; }

        public string ProjectName { get; set; }

        public string Duration { get; set; }

        public TimeEntriesCollectionView CollectionView { get; set; }

        #endregion

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

        public TimeEntryData GetActiveTimeEntry ()
        {
            return model.Data;
        }

        private async void OnActiveTimeEntryManagerPropertyChanged (object sender, PropertyChangedEventArgs args)
        {
            if (args.PropertyName == ActiveTimeEntryManager.PropertyActive) {
                await SyncModel ();
            }
        }

        private async void OnSettingChanged (SettingChangedMessage msg)
        {
            // Implement a GetPropertyName
            if (msg.Name == "GroupedTimeEntries") {
                await SyncCollectionView ();
            }
        }

        private async Task SyncModel ()
        {
            var data = timeEntryManager.Active;
            if (data != null) {
                model = new TimeEntryModel (data);
                await model.LoadAsync ();
                UpdateView ();
            }
        }

        private async Task SyncCollectionView ()
        {
            IsGroupedMode = ServiceContainer.Resolve<ISettingsStore> ().GroupedTimeEntries;

            if (CollectionView != null) {
                CollectionView.Dispose ();
            }

            CollectionView = await TimeEntriesCollectionView.Init (IsGroupedMode);
        }

        private void UpdateView ()
        {
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