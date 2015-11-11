using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Android.Content;
using Toggl.Phoebe.Analytics;
using Toggl.Phoebe.Data;
using Toggl.Phoebe.Data.DataObjects;
using Toggl.Phoebe.Data.Models;
using Toggl.Phoebe.Net;
using XPlatUtils;

namespace Toggl.Joey.Wear
{
    public static class WearDataProvider
    {
        private const int itemCount = 5;

        public static async Task StartStopTimeEntry (Context ctx)
        {
            var manager = ServiceContainer.Resolve<ActiveTimeEntryManager> ();
            if (manager.Active == null) {
                return;
            }

            var active = new TimeEntryModel (manager.Active);
            if (manager.Active.State == TimeEntryState.Running) {
                await active.StopAsync ();
//                ServiceContainer.Resolve<ITracker> ().SendTimerStopEvent (TimerStopSource.Watch);
            } else {
                active.Data.Description = ctx.Resources.GetString (Resource.String.WearEntryDefaultDescription);
                await active.StartAsync ();
//                ServiceContainer.Resolve<ITracker> ().SendTimerStartEvent (TimerStartSource.WatchStart);
            }
        }

        public static async Task ContinueTimeEntry (Guid timeEntryId)
        {
            var entryModel = new TimeEntryModel (timeEntryId);
            await entryModel.StartAsync ();
            ServiceContainer.Resolve<ITracker> ().SendTimerStartEvent (TimerStartSource.WatchContinue);
        }

        public static async Task<List<SimpleTimeEntryData>> GetTimeEntryData ()
        {
            var store = ServiceContainer.Resolve<IDataStore> ();
            var userId = ServiceContainer.Resolve<AuthManager> ().GetUserId ();

            var entriesQuery = store.Table<TimeEntryData> ()
                               .Where (r => r.State != TimeEntryState.New
                                       && r.DeletedAt == null
                                       && r.UserId == userId)
                               .OrderBy (r => r.StartTime, false);
            var entries = await entriesQuery.QueryAsync();

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
            return simpleEntries;
        }
    }
}
