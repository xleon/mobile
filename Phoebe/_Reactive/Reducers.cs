using System;
using System.Collections.Generic;
using System.Linq;
using Toggl.Phoebe.Logging;
using Toggl.Phoebe._Data;
using Toggl.Phoebe._Data.Models;
using Toggl.Phoebe._Helpers;
using Toggl.Phoebe._Net;
using XPlatUtils;
using Toggl.Phoebe.Net;

namespace Toggl.Phoebe._Reactive
{
    public interface IReducer
    {
        DataSyncMsg<object> Reduce (object state, DataMsg msg);
    }

    public class Reducer<T> : IReducer
    {
        readonly Func<T, DataMsg, DataSyncMsg<T>> reducer;

        public virtual DataSyncMsg<T> Reduce (T state, DataMsg msg)
        {
            return reducer (state, msg);
        }

        DataSyncMsg<object> IReducer.Reduce (object state, DataMsg msg)
        {
            return Reduce ((T)state, msg).Cast<object> ();
        }

        protected Reducer () { }

        public Reducer (Func<T, DataMsg, DataSyncMsg<T>> reducer)
        {
            this.reducer = reducer;
        }
    }

    public class TagCompositeReducer<T> : Reducer<T>, IReducer
    {
        readonly Dictionary<Type, Reducer<T>> reducers = new Dictionary<Type, Reducer<T>> ();

        public TagCompositeReducer<T> Add (Type msgType, Func<T, DataMsg, DataSyncMsg<T>> reducer)
        {
            return Add (msgType, new Reducer<T> (reducer));
        }

        public TagCompositeReducer<T> Add (Type msgType, Reducer<T> reducer)
        {
            reducers.Add (msgType, reducer);
            return this;
        }

        public override DataSyncMsg<T> Reduce (T state, DataMsg msg)
        {
            Reducer<T> reducer;
            if (reducers.TryGetValue (msg.GetType (), out reducer)) {
                return reducer.Reduce (state, msg);
            } else {
                return DataSyncMsg.Create (state);
            }
        }

        DataSyncMsg<object> IReducer.Reduce (object state, DataMsg msg)
        {
            return Reduce ((T)state, msg).Cast<object> ();
        }
    }

    public static class Reducers
    {
        public static Reducer<AppState> Init ()
        {
            return new TagCompositeReducer<AppState> ()
            .Add (typeof (DataMsg.Request), ServerRequest)
            .Add (typeof (DataMsg.ReceivedFromServer), ReceivedFromServer)
            .Add (typeof (DataMsg.TimeEntriesLoad), TimeEntriesLoad)
            .Add (typeof (DataMsg.TimeEntryPut), TimeEntryPut)
            .Add (typeof (DataMsg.TimeEntryDelete), TimeEntryDelete)
            .Add (typeof (DataMsg.TimeEntryContinue), TimeEntryContinue)
            .Add (typeof (DataMsg.TimeEntryStop), TimeEntryStop)
            .Add (typeof (DataMsg.TimeEntriesRemoveWithUndo), TimeEntriesRemoveWithUndo)
            .Add (typeof (DataMsg.TimeEntriesRestoreFromUndo), TimeEntriesRestoreFromUndo)
            .Add (typeof (DataMsg.TimeEntriesRemovePermanently), TimeEntriesRemovePermanently)
            .Add (typeof (DataMsg.TagsPut), TagsPut)
            .Add (typeof (DataMsg.ClientDataPut), ClientDataPut)
            .Add (typeof (DataMsg.ProjectDataPut), ProjectDataPut)
            .Add (typeof (DataMsg.UserDataPut), UserDataPut)
            .Add (typeof (DataMsg.ResetState), Reset);
        }

        static DataSyncMsg<AppState> ServerRequest (AppState state, DataMsg msg)
        {
            var request = (msg as DataMsg.Request).Data;
            var newState =
                request is ServerRequest.DownloadEntries
                ? state.With (downloadResult: state.DownloadResult.With (isSyncing: true))
                : state.With (authResult: AuthResult.Authenticating);
            return DataSyncMsg.Create (newState, request: request);
        }

        static DataSyncMsg<AppState> TimeEntriesLoad (AppState state, DataMsg msg)
        {
            var dataStore = ServiceContainer.Resolve <ISyncDataStore> ();
            var fullSync = (msg as DataMsg.TimeEntriesLoad).Data.ForceLeft ();
            var endDate = state.DownloadResult.NextDownloadFrom;

            // TODO RX: Should we load entries from db also in full sync? What should be the startDate?

            DownloadResult downloadInfo = state.DownloadResult;
            IList<TimeEntryData> dbEntries = new List<TimeEntryData> ();

            if (fullSync) {
                downloadInfo = downloadInfo.With (
                                   isSyncing: true,
                                   downloadFrom: DateTime.UtcNow,
                                   nextDownloadFrom: endDate);
            } else {
                var startDate = GetDatesByDays (dataStore, endDate, Literals.TimeEntryLoadDays);

                dbEntries = dataStore
                            .Table<TimeEntryData> ()
                            .Where (r =>
                                    r.State != TimeEntryState.New &&
                                    r.StartTime >= startDate && r.StartTime < endDate &&
                                    r.DeletedAt == null &&
                                    r.UserId == state.User.Id)
                            .Take (Literals.TimeEntryLoadMaxInit)
                            .OrderByDescending (r => r.StartTime)
                            .ToList ();

                downloadInfo =
                    downloadInfo.With (
                        isSyncing: true,
                        downloadFrom: endDate,
                        nextDownloadFrom: dbEntries.Any ()
                        ? dbEntries.Min (x => x.StartTime)
                        : endDate);
            }

            return DataSyncMsg.Create (
                       state.With (
                           downloadResult: downloadInfo,
                           timeEntries: state.UpdateTimeEntries (dbEntries)),
                       request: new ServerRequest.DownloadEntries (fullSync));
        }

        static DataSyncMsg<AppState> ReceivedFromServer (AppState state, DataMsg msg)
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
                               downloadResult: state.DownloadResult.With (isSyncing: false, hasMore: hasMore, hadErrors: false),
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
                state.With (downloadResult: state.DownloadResult.With (isSyncing: false, hadErrors: true))));
        }

        static DataSyncMsg<AppState> TimeEntryPut (AppState state, DataMsg msg)
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

        static DataSyncMsg<AppState> TimeEntryDelete (AppState state, DataMsg msg)
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

        static DataSyncMsg<AppState> TagsPut (AppState state, DataMsg msg)
        {
            var tags = (msg as DataMsg.TagsPut).Data.ForceLeft ();
            var dataStore = ServiceContainer.Resolve <ISyncDataStore> ();

            var updated = dataStore.Update (ctx => {
                foreach (var tag in tags) {
                    ctx.Put (tag);
                }
            });

            return DataSyncMsg.Create (state.With (tags: state.Update (state.Tags, updated)), updated);
        }

        static DataSyncMsg<AppState> ClientDataPut (AppState state, DataMsg msg)
        {
            var data = (msg as DataMsg.ClientDataPut).Data.ForceLeft ();
            var dataStore = ServiceContainer.Resolve <ISyncDataStore> ();

            var updated = dataStore.Update (ctx => ctx.Put (data));

            return DataSyncMsg.Create (state.With (clients: state.Update (state.Clients, updated)), updated);
        }

        static DataSyncMsg<AppState> ProjectDataPut (AppState state, DataMsg msg)
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

        static DataSyncMsg<AppState> UserDataPut (AppState state, DataMsg msg)
        {
            return (msg as DataMsg.UserDataPut).Data.Match (
            userData => {
                var dataStore = ServiceContainer.Resolve<ISyncDataStore> ();
                var updated = dataStore.Update (ctx => ctx.Put (userData));

                // This will throw an exception if user hasn't been correctly updated
                var userDataInDb = updated.Single () as UserData;

                // TODO RX: 
                ServiceContainer.Resolve<Data.ISettingsStore> ().UserId = userDataInDb.Id;

                return DataSyncMsg.Create (
                        state.With (
                            user: userDataInDb,
                            authResult: AuthResult.Success,
                            settings: state.Settings.With (userId: userDataInDb.Id)));
            },
            ex => {
                return DataSyncMsg.Create (state.With (
                                               user: new UserData (),
                                               authResult: ex.AuthResult
                                           ));
            });
        }

        static void CheckTimeEntryState (ITimeEntryData entryData, TimeEntryState expected, string action)
        {
            if (entryData.State != expected) {
                throw new InvalidOperationException (
                    String.Format ("Cannot {0} a time entry ({1}) in {2} state.",
                                   action, entryData.Id, entryData.State));
            }
        }

        static DataSyncMsg<AppState> TimeEntryContinue (AppState state, DataMsg msg)
        {
            var entryData = (msg as DataMsg.TimeEntryContinue).Data.ForceLeft ();
            var dataStore = ServiceContainer.Resolve <ISyncDataStore> ();

            var updated = dataStore.Update (ctx => {
                // Stop ActiveEntry if necessary
                var prev = state.ActiveEntry.Data;
                if (prev.Id != Guid.Empty && prev.State == TimeEntryState.Running) {
                    ctx.Put (new TimeEntryData (prev) {
                        State = TimeEntryState.Finished,
                        StopTime = Time.UtcNow
                    });
                }

                // TODO RX: Review the conditions to create a new time entry
                TimeEntryData newEntry = null;
                if (entryData.Id == Guid.Empty) {
                    newEntry = state.GetTimeEntryDraft ();
                } else {
                    CheckTimeEntryState (entryData, TimeEntryState.Finished, "continue");
                    newEntry = new TimeEntryData (entryData);
                }

                newEntry.RemoteId = null;
                newEntry.Id = Guid.NewGuid ();
                newEntry.State = TimeEntryState.Running;
                newEntry.StartTime = Time.UtcNow;
                newEntry.StopTime = null;
                ctx.Put (newEntry);
            });

            return DataSyncMsg.Create (
                       state.With (timeEntries: state.UpdateTimeEntries (updated)), updated);
        }

        static DataSyncMsg<AppState> TimeEntryStop (AppState state, DataMsg msg)
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
                       state.With (timeEntries: state.UpdateTimeEntries (updated)), updated);
        }

        static DataSyncMsg<AppState> TimeEntriesRemoveWithUndo (AppState state, DataMsg msg)
        {
            var removed = (msg as DataMsg.TimeEntriesRemoveWithUndo).Data.ForceLeft ()
            .Select (x => new TimeEntryData (x) {
                DeletedAt = Time.UtcNow
            });

            // Only update state, don't touch the db, nor send sync messages
            return DataSyncMsg.Create (state.With (timeEntries: state.UpdateTimeEntries (removed)));
        }

        static DataSyncMsg<AppState> TimeEntriesRestoreFromUndo (AppState state, DataMsg msg)
        {
            var restored = (msg as DataMsg.TimeEntriesRestoreFromUndo).Data.ForceLeft ();

            // Only update state, don't touch the db, nor send sync messages
            return DataSyncMsg.Create (state.With (timeEntries: state.UpdateTimeEntries (restored)));
        }

        static DataSyncMsg<AppState> TimeEntriesRemovePermanently (AppState state, DataMsg msg)
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

        static DataSyncMsg<AppState> Reset (AppState state, DataMsg msg)
        {
            var dataStore = ServiceContainer.Resolve <ISyncDataStore> ();
            dataStore.WipeTables ();

            // TODO RX: Clear platform settings?

            // Reset state
            var appState = AppState.Init ();

            // TODO: Clean settings?
            // TODO: Ping analytics?
            // TODO: Call Log service?

            return DataSyncMsg.Create (appState);
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

