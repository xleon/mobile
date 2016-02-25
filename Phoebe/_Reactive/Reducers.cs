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
            .Add (typeof (DataMsg.TimeEntriesSync), TimeEntriesSync)
            .Add (typeof (DataMsg.TimeEntriesLoad), TimeEntriesLoad)
            .Add (typeof (DataMsg.TimeEntryPut), TimeEntryPut)
            .Add (typeof (DataMsg.TimeEntryDelete), TimeEntryDelete)
            .Add (typeof (DataMsg.TimeEntryContinue), TimeEntryContinue)
            .Add (typeof (DataMsg.TimeEntryStop), TimeEntryStop)
            .Add (typeof (DataMsg.TimeEntriesRemoveWithUndo), TimeEntriesRemoveWithUndo)
            .Add (typeof (DataMsg.TimeEntriesRestoreFromUndo), TimeEntriesRestoreFromUndo)
            .Add (typeof (DataMsg.TimeEntriesRemovePermanently), TimeEntriesRemovePermanently)
            .Add (typeof (DataMsg.TagPut), TagPut)
            .Add (typeof (DataMsg.TagsPut), TagsPut)
            .Add (typeof (DataMsg.ClientDataPut), ClientDataPut)
            .Add (typeof (DataMsg.ProjectDataPut), ProjectDataPut);

            return new PropertyCompositeReducer<AppState> ()
                   .Add (x => x.TimerState, tagReducer);
        }

        static DataSyncMsg<TimerState> TimeEntriesSync (TimerState state, DataMsg msg)
        {
            var newState = state.With (downloadInfo: state.DownloadInfo.With (isSyncing: true));
            return DataSyncMsg.Create (newState, isSyncRequested: true);
        }

        static DataSyncMsg<TimerState> TimeEntriesLoad (TimerState state, DataMsg msg)
        {
            var dataStore = ServiceContainer.Resolve <ISyncDataStore> ();
            var endDate = state.DownloadInfo.NextDownloadFrom;
            var startDate = GetDatesByDays (dataStore, endDate, Literals.TimeEntryLoadDays);

            var dbEntries = dataStore
                            .Table<TimeEntryData> ()
                            .Where (r =>
                                    r.State != TimeEntryState.New &&
                                    r.StartTime >= startDate && r.StartTime < endDate &&
                                    r.DeletedAt == null &&
                                    r.UserId == state.User.Id)
                            .Take (Literals.TimeEntryLoadMaxInit)
                            .OrderByDescending (r => r.StartTime)
                            .ToList ();

            var downloadInfo =
                state.DownloadInfo.With (
                    isSyncing: true,
                    downloadFrom: endDate,
                    nextDownloadFrom: dbEntries.Any ()
                    ? dbEntries.Min (x => x.StartTime)
                    : endDate);

            return DataSyncMsg.Create (
                       state.With (
                           downloadInfo: downloadInfo,
                           timeEntries: state.UpdateTimeEntries (dbEntries)),
                       isSyncRequested: true);
        }

        static DataSyncMsg<TimerState> ReceivedFromServer (TimerState state, DataMsg msg)
        {
            return (msg as DataMsg.ReceivedFromServer).Data.Match (
            receivedData => {
                var dataStore = ServiceContainer.Resolve <ISyncDataStore> ();

                var updated = dataStore.Update (ctx => {
                    foreach (var newData in receivedData) {
                        ICommonData oldData = null;
                        // Check first if the newData has localId assigned
                        // (for example, the ones returned by TogglClient.Create)
                        if (newData.Id != Guid.Empty) {
                            oldData = ctx.SingleOrDefault (x => x.Id == newData.Id);
                        }
                        // If no localId, check if an item with the same RemoteId is in the db
                        else {
                            oldData = ctx.SingleOrDefault (x => x.RemoteId == newData.RemoteId);
                        }

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

                var hasMore = receivedData.OfType<TimeEntryData> ().Any ();

            return DataSyncMsg.Create (
                    state.With (
                        downloadInfo: state.DownloadInfo.With (isSyncing: false, hasMore: hasMore, hadErrors: false),
                        workspaces: state.Update (state.Workspaces, updated),
                        projects: state.Update (state.Projects, updated),   
                        workspaceUsers: state.Update (state.WorkspaceUsers, updated),
                        projectUsers: state.Update (state.ProjectUsers, updated),
                        clients: state.Update (state.Clients, updated),
                        tasks: state.Update (state.Tasks, updated),
                        tags: state.Update (state.Tags, updated),
                        // TODO: Check if the updated entries are within the current scroll view
                        // Probably it's better to do this check in UpdateTimeEntries
                        timeEntries: state.UpdateTimeEntries (updated)
                    ));
            },
            ex => DataSyncMsg.Create (
                state.With (downloadInfo: state.DownloadInfo.With (isSyncing: false, hadErrors: true))));
        }

        static DataSyncMsg<TimerState> TimeEntryPut (TimerState state, DataMsg msg)
        {
            var entryData = (msg as DataMsg.TimeEntryPut).Data.ForceLeft ();
            var dataStore = ServiceContainer.Resolve <ISyncDataStore> ();

            // TODO: Entry sanity check
            var updated = dataStore.Update (ctx => ctx.Put (entryData));

            // TODO: Check updated.Count == 1?
            return DataSyncMsg.Create (
                       state.With (timeEntries: state.UpdateTimeEntries (updated)),
                       updated);
        }

        static DataSyncMsg<TimerState> TimeEntryDelete (TimerState state, DataMsg msg)
        {
            var entryData = (msg as DataMsg.TimeEntryDelete).Data.ForceLeft ();
            var dataStore = ServiceContainer.Resolve <ISyncDataStore> ();

            var updated = dataStore.Update (ctx => ctx.Delete (new TimeEntryData (entryData) {
                DeletedAt = Time.UtcNow
            }));

            // TODO: Check updated.Count == 1?
            return DataSyncMsg.Create (
                state.With (timeEntries: state.UpdateTimeEntries (updated)),
                updated);
        }

        static DataSyncMsg<TimerState> TagPut (TimerState state, DataMsg msg)
        {
            var tagData = (msg as DataMsg.TagPut).Data.ForceLeft ();
            var dataStore = ServiceContainer.Resolve <ISyncDataStore> ();

            var updated = dataStore.Update (ctx => ctx.Put (tagData));

            return DataSyncMsg.Create (
                state.With (timeEntries: state.UpdateTimeEntries (updated)),
                updated);
        }

        static DataSyncMsg<TimerState> TagsPut (TimerState state, DataMsg msg)
        {
            var tuple = (msg as DataMsg.TagsPut).Data.ForceLeft ();
            var dataStore = ServiceContainer.Resolve <ISyncDataStore> ();

            var updated = dataStore.Update (ctx => {
                ctx.Put (new TimeEntryData (tuple.Item1) {
                    // TODO: This should already have been done by the ViewModel
                    Tags = tuple.Item2.Select (x => x.Name).ToList ()
                });

                foreach (var tag in tuple.Item2) {
                    ctx.Put (tag);
                }
            });

            return DataSyncMsg.Create (
                state.With (timeEntries: state.UpdateTimeEntries (updated)),
                updated);

        }

        static DataSyncMsg<TimerState> ClientDataPut (TimerState state, DataMsg msg)
        {
            var data = (msg as DataMsg.ClientDataPut).Data.ForceLeft ();
            var dataStore = ServiceContainer.Resolve <ISyncDataStore> ();

            var updated = dataStore.Update (ctx => ctx.Put (data));

            return DataSyncMsg.Create (state.With (clients: state.Update (state.Clients, updated)), updated);
        }

        static DataSyncMsg<TimerState> ProjectDataPut (TimerState state, DataMsg msg)
        {
            var data = (msg as DataMsg.ProjectDataPut).Data.ForceLeft ();
            var dataStore = ServiceContainer.Resolve <ISyncDataStore> ();

            var updated = dataStore.Update (ctx => {
                ctx.Put (data.Item1);
                ctx.Put (data.Item2);
            });

            return DataSyncMsg.Create (
                state.With (
                    projects: state.Update (state.Projects, updated),
                    projectUsers: state.Update (state.ProjectUsers, updated)
                ), updated);
        }

        static void CheckTimeEntryState (ITimeEntryData entryData, TimeEntryState expected, string action)
        {
            if (entryData.State != expected) {
                throw new InvalidOperationException (
                    String.Format ("Cannot {0} a time entry ({1}) in {2} state.",
                                   action, entryData.Id, entryData.State));
            }
        }

        static DataSyncMsg<TimerState> TimeEntryContinue (TimerState state, DataMsg msg)
        {
            var entryData = (msg as DataMsg.TimeEntryContinue).Data.ForceLeft ();
            var dataStore = ServiceContainer.Resolve <ISyncDataStore> ();

            CheckTimeEntryState (entryData, TimeEntryState.Finished, "continue");

            var updated = dataStore.Update (ctx => ctx.Put (new TimeEntryData (entryData) {
                State = TimeEntryState.Running,
            }));

            // TODO: Check updated.Count == 1?
            return DataSyncMsg.Create (
                       state.With (timeEntries: state.UpdateTimeEntries (updated)),
                       updated);
        }

        static DataSyncMsg<TimerState> TimeEntryStop (TimerState state, DataMsg msg)
        {
            var entryData = (msg as DataMsg.TimeEntryStop).Data.ForceLeft ();
            var dataStore = ServiceContainer.Resolve <ISyncDataStore> ();

            CheckTimeEntryState (entryData, TimeEntryState.Running, "stop");

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

