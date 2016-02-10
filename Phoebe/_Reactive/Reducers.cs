using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Toggl.Phoebe.Logging;
using Toggl.Phoebe._Data;
using Toggl.Phoebe._Data.Json;
using Toggl.Phoebe._Data.Models;
using Toggl.Phoebe._Helpers;
using Toggl.Phoebe._Net;
using Toggl.Phoebe._ViewModels.Timer;
using XPlatUtils;

namespace Toggl.Phoebe._Reactive
{
    public static class Reducers
    {
        static DataSyncMsg<TimerState> TimeEntryLoad (TimerState state, IDataMsg msg)
        {
            var userId = state.User.Id;
            var paginationDate = state.PaginationDate;
            var dataStore = ServiceContainer.Resolve <ISyncDataStore> ();
            var startDate = GetDatesByDays (dataStore, paginationDate, Literals.TimeEntryLoadDays);

            var dbEntries = dataStore
                            .Table<TimeEntryData> ()
                            .Where (r =>
                                    r.State != TimeEntryState.New &&
                                    r.StartTime >= startDate && r.StartTime < paginationDate &&
                                    r.DeletedAt == null &&
                                    r.UserId == userId)
                            .Take (Literals.TimeEntryLoadMaxInit)
                            .OrderByDescending (r => r.StartTime)
                            .ToList ();

            // Try to update with latest data from server with old paginationDate to get the same data
            RxChain.Send (typeof (Reducers), DataTag.EmptyQueueAndSync, paginationDate);
            paginationDate = dbEntries.Count > 0 ? startDate : paginationDate;

            return DataSyncMsg.Create (state.With (
                                           paginationDate: paginationDate,
                                           timeEntries: state.UpdateTimeEntries (dbEntries)));
        }

        static DataSyncMsg<TimerState> TimeEntryReceivedFromServer (TimerState state, IDataMsg msg)
        {
            var receivedData = msg.ForceGetData<IReadOnlyList<CommonData>> ();

            var dataStore = ServiceContainer.Resolve <ISyncDataStore> ();

            var updated = dataStore.Update (ctx => {
                foreach (var newData in receivedData) {
                    var oldData = ctx.SingleOrDefault (x => x.RemoteId == newData.RemoteId);
                    if (oldData != null) {
                        if (newData.CompareTo (oldData) > 0) {
                            newData.Id = oldData.Id;
                            PutOrDelete (ctx, newData);
                        }
                    } else {
                        newData.Id = Guid.NewGuid (); // Assign new Id
                        PutOrDelete (ctx, newData);
                    }
                }
            });

            return DataSyncMsg.Create (state.With (
                                           workspaces: state.Update (state.Workspaces, updated),
                                           projects: state.Update (state.Projects, updated),
                                           clients: state.Update (state.Clients, updated),
                                           tasks: state.Update (state.Tasks, updated),
                                           tags: state.Update (state.Tags, updated),
                                           timeEntries: state.UpdateTimeEntries (updated)
                                       ));
        }

        static DataSyncMsg<TimerState> TimeEntryStop (TimerState state, IDataMsg msg)
        {
            var entryData = msg.ForceGetData<ITimeEntryData> ();
            var dataStore = ServiceContainer.Resolve <ISyncDataStore> ();

            if (entryData.State != TimeEntryState.Running) {
                throw new InvalidOperationException (
                    String.Format ("Cannot stop a time entry ({0}) in {1} state.",
                                   entryData.Id, entryData.State));
            }

            var updated = dataStore.Update (ctx => {
                ctx.Put (new TimeEntryData (entryData) {
                    State = TimeEntryState.Finished,
                    StopTime = Time.UtcNow
                });
            });

            // TODO: Check updated.Count == 1?
            return DataSyncMsg.Create (
                       state.With (timeEntries: state.UpdateTimeEntries (updated)),
                       updated);
        }

        static DataSyncMsg<TimerState> TimeEntriesRemovePermanently (TimerState state, IDataMsg msg)
        {
            var entryMsg = msg.ForceGetData<IEnumerable<ITimeEntryData>> ();
            var dataStore = ServiceContainer.Resolve <ISyncDataStore> ();

            var removed = dataStore.Update (ctx => {
                foreach (var entryData in entryMsg) {
                    ctx.Delete (new TimeEntryData (entryData) {
                        DeletedAt = Time.UtcNow
                    });
                }
            });

            // TODO: Check removed.Count?
            return DataSyncMsg.Create (
                       state.With (timeEntries: state.UpdateTimeEntries (removed)),
                       removed);
        }

        #region Util
        static void PutOrDelete (ISyncDataStoreContext ctx, ICommonData data)
        {
            if (data.DeletedAt == null) {
                ctx.Put (data);
            } else {
                ctx.Delete (data);
            }
        }

        // TODO: replace this method with the SQLite equivalent.
        static DateTime GetDatesByDays (ISyncDataStore dataStore, DateTime startDate, int numDays)
        {
            var baseQuery = dataStore.Table<TimeEntryData> ().Where (
                                r => r.State != TimeEntryState.New &&
                                r.StartTime < startDate &&
                                r.DeletedAt == null);

            var entries = baseQuery.ToList ();
            if (entries.Count > 0) {
                var group = entries
                            .OrderByDescending (r => r.StartTime)
                            .GroupBy (t => t.StartTime.Date)
                            .Take (numDays)
                            .LastOrDefault ();
                return group.Key;
            }
            return DateTime.MinValue;
        }
        #endregion
    }
}

