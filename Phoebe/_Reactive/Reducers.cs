using System;
using System.Collections.Generic;
using System.Linq;
using Toggl.Phoebe._Data;
using Toggl.Phoebe._Data.Models;
using Toggl.Phoebe._Helpers;
using XPlatUtils;

namespace Toggl.Phoebe._Reactive
{
    public static class Reducers
    {
        public static Reducer<AppState> Init ()
        {
            var tagReducer = new TagCompositeReducer<TimerState> ()
            .Add (typeof (DataMsg.ReceivedFromServer), ReceivedFromServer)
            .Add (typeof (DataMsg.TimeEntriesLoad), TimeEntriesLoad)
            .Add (typeof (DataMsg.TimeEntryAdd), TimeEntryAdd)
            .Add (typeof (DataMsg.TimeEntryContinue), TimeEntryContinue)
            .Add (typeof (DataMsg.TimeEntryStop), TimeEntryStop)
            .Add (typeof (DataMsg.TimeEntriesRemoveWithUndo), TimeEntriesRemoveWithUndo)
            .Add (typeof (DataMsg.TimeEntriesRestoreFromUndo), TimeEntriesRestoreFromUndo)
            .Add (typeof (DataMsg.TimeEntriesRemovePermanently), TimeEntriesRemovePermanently);

            return new PropertyCompositeReducer<AppState> ()
                   .Add (x => x.TimerState, tagReducer);
        }

        static DataSyncMsg<TimerState> TimeEntriesLoad (TimerState state, DataMsg msg)
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
            RxChain.Send (new DataMsg.EmptyQueueAndSync (paginationDate));
            paginationDate = dbEntries.Count > 0 ? startDate : paginationDate;

            return DataSyncMsg.Create (
                       state.With (
                           paginationDate: paginationDate,
                           timeEntries: state.UpdateTimeEntries (dbEntries)));
        }

        static DataSyncMsg<TimerState> ReceivedFromServer (TimerState state, DataMsg msg)
        {
            // TODO: Check if there had been errors
            var receivedData = (msg as DataMsg.ReceivedFromServer).Data.ForceLeft ();
            var dataStore = ServiceContainer.Resolve <ISyncDataStore> ();

            var updated = dataStore.Update (ctx => {
                foreach (var newData in receivedData) {
                    var oldData = ctx.SingleOrDefault (x => x.RemoteId == newData.RemoteId);
                    if (oldData != null) {
                        if (newData.CompareTo (oldData) >= 0) {
                            newData.Id = oldData.Id;
                            PutOrDelete (ctx, newData);
                        }
                    } else {
                        newData.Id = Guid.NewGuid (); // Assign new Id
                        PutOrDelete (ctx, newData);
                    }
                }
            });

            return DataSyncMsg.Create (
                       state.With (
                           workspaces: state.Update (state.Workspaces, updated),
                           projects: state.Update (state.Projects, updated),
                           clients: state.Update (state.Clients, updated),
                           tasks: state.Update (state.Tasks, updated),
                           tags: state.Update (state.Tags, updated),
                           timeEntries: state.UpdateTimeEntries (updated)
                       ));
        }

        static DataSyncMsg<TimerState> TimeEntryAdd (TimerState state, DataMsg msg)
        {
            var entryData = (msg as DataMsg.TimeEntryAdd).Data.ForceLeft ();
            var dataStore = ServiceContainer.Resolve <ISyncDataStore> ();

            // TODO: Entry sanity check
            var updated = dataStore.Update (ctx => ctx.Put (entryData));

            // TODO: Check updated.Count == 1?
            return DataSyncMsg.Create (
                       state.With (timeEntries: state.UpdateTimeEntries (updated)),
                       updated);
        }

        static DataSyncMsg<TimerState> TimeEntryContinue (TimerState state, DataMsg msg)
        {
            var entryData = (msg as DataMsg.TimeEntryContinue).Data.ForceLeft ();
            var dataStore = ServiceContainer.Resolve <ISyncDataStore> ();

            if (entryData.State != TimeEntryState.Finished) {
                throw new InvalidOperationException (
                    String.Format ("Cannot continue a time entry ({0}) in {1} state.",
                                   entryData.Id, entryData.State));
            }

            var updated = dataStore.Update (ctx => {
                // TODO: Create new entry
                throw new NotImplementedException ();
            });

            // TODO: Check updated.Count == 1?
            return DataSyncMsg.Create (
                       state.With (timeEntries: state.UpdateTimeEntries (updated)),
                       updated);
        }

        static DataSyncMsg<TimerState> TimeEntryStop (TimerState state, DataMsg msg)
        {
            var entryData = (msg as DataMsg.TimeEntryStop).Data.ForceLeft ();
            var dataStore = ServiceContainer.Resolve <ISyncDataStore> ();

            if (entryData.State != TimeEntryState.Running) {
                throw new InvalidOperationException (
                    String.Format ("Cannot stop a time entry ({0}) in {1} state.",
                                   entryData.Id, entryData.State));
            }

            var updated = dataStore.Update (ctx => ctx.Put (new TimeEntryData (entryData) {
                State = TimeEntryState.Finished,
                StopTime = Time.UtcNow
            }));

            // TODO: Check updated.Count == 1?
            return DataSyncMsg.Create (
                       state.With (timeEntries: state.UpdateTimeEntries (updated)),
                       updated);
        }

        static DataSyncMsg<TimerState> TimeEntriesRemoveWithUndo (TimerState state, DataMsg msg)
        {
            var removed = (msg as DataMsg.TimeEntriesRemoveWithUndo).Data.ForceLeft ()
            .Select (x => new TimeEntryData (x) {
                DeletedAt = Time.UtcNow
            });

            // Only update state, don't touch the db, nor send sync messages
            return DataSyncMsg.Create (state.With (timeEntries: state.UpdateTimeEntries (removed)));
        }

        static DataSyncMsg<TimerState> TimeEntriesRestoreFromUndo (TimerState state, DataMsg msg)
        {
            var restored = (msg as DataMsg.TimeEntriesRestoreFromUndo).Data.ForceLeft ();

            // Only update state, don't touch the db, nor send sync messages
            return DataSyncMsg.Create (state.With (timeEntries: state.UpdateTimeEntries (restored)));
        }

        static DataSyncMsg<TimerState> TimeEntriesRemovePermanently (TimerState state, DataMsg msg)
        {
            var entryMsg = (msg as DataMsg.TimeEntriesRemovePermanently).Data.ForceLeft ();
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

