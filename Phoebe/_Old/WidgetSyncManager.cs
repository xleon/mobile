using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Toggl.Phoebe.Analytics;
using Toggl.Phoebe.Data;
using Toggl.Phoebe.Data.DataObjects;
using Toggl.Phoebe.Data.Models;
using Toggl.Phoebe.Logging;
using Toggl.Phoebe.Net;
using Toggl.Phoebe._Data;
using Toggl.Phoebe._Reactive;
using XPlatUtils;

namespace Toggl.Phoebe
{
    public class WidgetSyncManager : IDisposable
    {
        private const string Tag = "WidgetSyncManager";
        private const string DefaultDurationText = " 00:00:00 ";

        private readonly AuthManager authManager;
        private Toggl.Phoebe._Data.ActiveTimeEntryManager activeTimeEntryManager;
        private readonly IWidgetUpdateService widgetUpdateService;
        private Subscription<SyncStartedMessage> subscriptionSyncStarted;
        private Subscription<SyncFinishedMessage> subscriptionSyncFinished;
        private Subscription<Toggl.Phoebe._Data.StartStopMessage> subscriptionStartStopFinished;

        private bool isLoading;
        private MessageBus messageBus;

        public WidgetSyncManager ()
        {
            authManager = ServiceContainer.Resolve<AuthManager>();
            authManager.PropertyChanged += OnAuthPropertyChanged;

            activeTimeEntryManager = ServiceContainer.Resolve<ActiveTimeEntryManager> ();
            widgetUpdateService = ServiceContainer.Resolve<IWidgetUpdateService> ();

            messageBus = ServiceContainer.Resolve<MessageBus> ();
            subscriptionSyncStarted = messageBus.Subscribe<SyncStartedMessage> (OnSyncWidget);
            subscriptionSyncFinished = messageBus.Subscribe<SyncFinishedMessage> (OnSyncWidget);
            subscriptionStartStopFinished = messageBus.Subscribe<Toggl.Phoebe._Data.StartStopMessage> (OnSyncWidget);
        }

        public void Dispose ()
        {
            var bus = ServiceContainer.Resolve<MessageBus> ();

            if (subscriptionSyncStarted != null) {
                bus.Unsubscribe (subscriptionSyncStarted);
                subscriptionSyncStarted = null;
            }

            if (subscriptionSyncFinished != null) {
                bus.Unsubscribe (subscriptionSyncFinished);
                subscriptionSyncFinished = null;
            }

            if (subscriptionSyncFinished != null) {
                bus.Unsubscribe (subscriptionSyncFinished);
                subscriptionSyncFinished = null;
            }
        }

        public async Task StartStopTimeEntry ()
        {
            if (activeTimeEntryManager.IsRunning) {
                RxChain.Send (this.GetType (), DataTag.TimeEntryStop, activeTimeEntryManager.ActiveTimeEntry);
                ServiceContainer.Resolve<ITracker>().SendTimerStopEvent (TimerStopSource.Widget);
            } else {
                var startedEntry = await TimeEntryModel.StartAsync (TimeEntryModel.GetDraft ());

                // Show new screen on platform
                widgetUpdateService.ShowNewTimeEntryScreen (new TimeEntryModel (startedEntry));
                ServiceContainer.Resolve<ITracker>().SendTimerStartEvent (TimerStartSource.WidgetNew);
            }
        }

        public async Task ContinueTimeEntry (Guid timeEntryId)
        {
            var entryModel = new TimeEntryModel (timeEntryId);
            await TimeEntryModel.ContinueAsync (entryModel.Data);
            ServiceContainer.Resolve<ITracker>().SendTimerStartEvent (TimerStartSource.WidgetStart);
        }

        private async void OnSyncWidget (Message msg)
        {
            await SyncWidgetData ();
        }

        public async Task SyncWidgetData ()
        {
            try {
                // Dispathc started message.
                messageBus.Send<SyncWidgetMessage> (new SyncWidgetMessage (this,true));

                // Query local data:
                var store = ServiceContainer.Resolve<IDataStore> ();
                var entries = await store.Table<TimeEntryData> ()
                              .OrderByDescending (r => r.StartTime) .Where (r => r.DeletedAt == null && r.State != TimeEntryState.New)
                              .Take (4).ToListAsync ().ConfigureAwait (false);

                var widgetEntries = new List<WidgetEntryData>();

                foreach (var entry in entries) {

                    ProjectData project;
                    if (entry.ProjectId != null) {
                        var q = store.Table<ProjectData>().Where (p => p.Id == entry.ProjectId.Value);
                        var l = await q.ToListAsync ().ConfigureAwait (false);
                        project = l.FirstOrDefault();
                    } else {
                        project = new ProjectData {
                            Name = string.Empty,
                            Color = ProjectModel.HexColors.Length - 1,
                        };
                    }

                    widgetEntries.Add (new WidgetEntryData {
                        Id = entry.Id.ToString(),
                        ProjectName = project.Name,
                        Description = entry.Description,
                        Color = ProjectModel.HexColors [ project.Color % ProjectModel.HexColors.Length],
                        IsRunning = entry.State == TimeEntryState.Running,
                        TimeValue = (entry.StopTime - entry.StartTime).ToString(),
                        Duration = (entry.StopTime.HasValue ? entry.StopTime.Value : Time.UtcNow) - entry.StartTime,
                    });

                }

                widgetUpdateService.LastEntries = widgetEntries;

            } catch (Exception exc) {
                var log = ServiceContainer.Resolve<ILogger> ();
                log.Error (Tag, exc, "Failed to fetch time entries");
            } finally {
                isLoading = false;

                // Dispathc ended message.
                messageBus.Send<SyncWidgetMessage> (new SyncWidgetMessage (this));
            }
        }

        private void OnAuthPropertyChanged (object sender, PropertyChangedEventArgs args)
        {
            if (args.PropertyName == AuthManager.PropertyIsAuthenticated) {
                widgetUpdateService.IsUserLogged = authManager.IsAuthenticated;
            }
        }

        public class WidgetEntryData
        {
            public string Id { get; set; }

            public string ProjectName { get; set; }

            public string Description { get; set; }

            public string TimeValue { get; set; }

            public string Color { get; set; }

            public bool IsRunning { get; set; }

            public TimeSpan Duration { get; set; }
        }
    }
}

