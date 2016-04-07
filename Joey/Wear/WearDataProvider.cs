using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Android.Content;
using Toggl.Phoebe.Analytics;
using XPlatUtils;

namespace Toggl.Joey.Wear
{
    public static class WearDataProvider
    {
        private const int itemCount = 10;

        public static async Task StartStopTimeEntry(Context ctx)
        {
            await Task.Delay(5);
            /*
            var manager = ServiceContainer.Resolve<ActiveTimeEntryManager> ();
            var active = manager.ActiveTimeEntry;
            if (manager.ActiveTimeEntry.State == TimeEntryState.Running) {
                await TimeEntryModel.StopAsync (active);
                ServiceContainer.Resolve<ITracker> ().SendTimerStopEvent (TimerStopSource.Watch);
            } else {
                active.Description = ctx.Resources.GetString (Resource.String.WearEntryDefaultDescription);
                await TimeEntryModel.StartAsync (active);
                ServiceContainer.Resolve<ITracker> ().SendTimerStartEvent (TimerStartSource.WatchStart);
            }
            */
        }

        public static async Task ContinueTimeEntry(Guid timeEntryId)
        {
            //var entryModel = new TimeEntryModel (timeEntryId);
            //await TimeEntryModel.StartAsync (entryModel.Data);
            ServiceContainer.Resolve<ITracker> ().SendTimerStartEvent(TimerStartSource.WatchContinue);
        }

        public static async Task<List<SimpleTimeEntryData>> GetTimeEntryData()
        {
            /*
            var store = ServiceContainer.Resolve<IDataStore> ();
            var userId = Guid.Empty; //ServiceContainer.Resolve<AuthManager> ().GetUserId ();

            var entries = await store.Table<TimeEntryData>()
                          .Where (r => r.State != TimeEntryState.New
                                  && r.DeletedAt == null
                                  && r.UserId == userId)
                          .OrderByDescending (r => r.StartTime)
                          .ToListAsync();

            var uniqueEntries = entries.GroupBy (x  => new {x.ProjectId, x.Description })
            .Select (grp => grp.First())
            .Take (itemCount)
            .ToList();

            var simpleEntries = new List<SimpleTimeEntryData> ();
            foreach (var entry in uniqueEntries) {
                var model = new TimeEntryModel (entry);
                await model.LoadAsync();

                int color = 0;
                String projectName = "";
                if (model.Project != null) {
                    color = model.Project.Color;
                    projectName = model.Project.Name;
                }
                var colorString = ProjectModel.HexColors [color % ProjectModel.HexColors.Length];

                simpleEntries.Add (
                new SimpleTimeEntryData {
                    Id = entry.Id,
                    IsRunning = entry.State == TimeEntryState.Running,
                    Description = entry.Description,
                    Project = projectName,
                    ProjectColor = colorString,
                    StartTime = entry.StartTime,
                    StopTime = entry.StopTime ?? DateTime.MinValue
                });
            }
            */
            return new List<SimpleTimeEntryData>();
        }
    }
}