using System;
using System.Collections.Generic;
using System.Linq;
using Toggl.Phoebe.Data;
using Toggl.Phoebe.Data.Models;
using Toggl.Phoebe.Helpers;
using Toggl.Phoebe.Net;
using XPlatUtils;

namespace Toggl.Phoebe.Reactive
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
                   .Add (typeof (DataMsg.ReceivedFromSync), ReceivedFromSync)
                   .Add (typeof (DataMsg.ReceivedFromDownload), ReceivedFromDownload)
                   .Add (typeof (DataMsg.FullSync), FullSync)
                   .Add (typeof (DataMsg.TimeEntriesLoad), TimeEntriesLoad)
                   .Add (typeof (DataMsg.TimeEntryPut), TimeEntryPut)
                   .Add (typeof (DataMsg.TimeEntriesRemove), TimeEntryRemove)
                   .Add (typeof (DataMsg.TimeEntryContinue), TimeEntryContinue)
                   .Add (typeof (DataMsg.TimeEntryStop), TimeEntryStop)
                   .Add (typeof (DataMsg.TagsPut), TagsPut)
                   .Add (typeof (DataMsg.ClientDataPut), ClientDataPut)
                   .Add (typeof (DataMsg.ProjectDataPut), ProjectDataPut)
                   .Add (typeof (DataMsg.UserDataPut), UserDataPut)
                   .Add (typeof (DataMsg.ResetState), Reset)
                   .Add (typeof (DataMsg.UpdateSetting), UpdateSettings);
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

        static DataSyncMsg<AppState> FullSync (AppState state, DataMsg msg)
        {
            var syncDate = state.Settings.SyncLastRun;
            FullSyncResult syncResult = state.FullSyncResult;

            syncResult = syncResult.With (
                             isSyncing: true,
                             hadErrors:false,
                             syncLastRun:syncDate);

            return DataSyncMsg.Create (
                       state.With (fullSyncResult:syncResult),
                       request: new ServerRequest.FullSync ());
        }

        static DataSyncMsg<AppState> TimeEntriesLoad (AppState state, DataMsg msg)
        {
            var dataStore = ServiceContainer.Resolve <ISyncDataStore> ();
            var endDate = state.DownloadResult.NextDownloadFrom;

            DownloadResult downloadInfo = state.DownloadResult;
            IList<TimeEntryData> dbEntries = new List<TimeEntryData>();

            var startDate = GetDatesByDays (dataStore, endDate, Literals.TimeEntryLoadDays);
            dbEntries = dataStore
                        .Table<TimeEntryData>()
                        .Where (r =>
                                r.State != TimeEntryState.New &&
                                r.StartTime >= startDate && r.StartTime < endDate &&
                                r.DeletedAt == null)
                        // TODO TODO TODO: Rx why the entries are saved without local user ID.
                        //r.UserId == state.User.Id)
                        .Take (Literals.TimeEntryLoadMaxInit)
                        .OrderByDescending (r => r.StartTime)
                        .ToList();

            downloadInfo =
                downloadInfo.With (
                    isSyncing: true,
                    downloadFrom: endDate,
                    nextDownloadFrom: dbEntries.Any()
                    ? dbEntries.Min (x => x.StartTime)
                    : endDate);

            return DataSyncMsg.Create (
                       state.With (
                           downloadResult: downloadInfo,
                           timeEntries: state.UpdateTimeEntries (dbEntries)),
                       request: new ServerRequest.DownloadEntries());
        }

        static DataSyncMsg<AppState> ReceivedFromSync (AppState state, DataMsg msg)
        {
            var serverMsg = msg as DataMsg.ReceivedFromSync;
            return serverMsg.Data.Match (receivedData => {
                // Side effect operation.
                // Update state inside.
                state = UpdateStateWithNewData (state, receivedData);

                // Update user
                var dataStore = ServiceContainer.Resolve<ISyncDataStore>();
                UserData user = serverMsg.FullSyncInfo.Item1;
                user.Id = state.User.Id;
                user.DefaultWorkspaceId = state.Workspaces.Values.Single (x => x.RemoteId == user.DefaultWorkspaceRemoteId).Id;
                var userUpdated = (UserData)dataStore.Update (ctx => ctx.Put (user)).Single();

                // Modify state.
                return DataSyncMsg.Create (
                           state.With (
                               user: userUpdated,
                               fullSyncResult: state.FullSyncResult.With (isSyncing: false, hadErrors: false, syncLastRun: serverMsg.FullSyncInfo.Item2),
                               settings: state.Settings.With (syncLastRun: serverMsg.FullSyncInfo.Item2)));
            },
            ex => DataSyncMsg.Create (state.With (fullSyncResult: state.FullSyncResult.With (isSyncing: false, hadErrors: true))));
        }

        static DataSyncMsg<AppState> ReceivedFromDownload (AppState state, DataMsg msg)
        {
            var serverMsg = msg as DataMsg.ReceivedFromDownload;
            return serverMsg.Data.Match (receivedData => {
                // Side effect operation.
                // Update state inside.
                state = UpdateStateWithNewData (state, receivedData);

                // Modify state with download info
                var hasMore = receivedData.OfType<TimeEntryData>().Any();
                return DataSyncMsg.Create (
                           state.With (downloadResult: state.DownloadResult.With (isSyncing: false, hasMore: hasMore, hadErrors: false)
                                      ));
            },
            ex => DataSyncMsg.Create (state.With (downloadResult: state.DownloadResult.With (isSyncing: false, hadErrors: true))));
        }

        static DataSyncMsg<AppState> TimeEntryPut (AppState state, DataMsg msg)
        {
            var entryData = (msg as DataMsg.TimeEntryPut).Data.ForceLeft();
            var dataStore = ServiceContainer.Resolve<ISyncDataStore>();

            // TODO: Entry sanity check
            var updated = dataStore.Update (ctx => ctx.Put (entryData));

            // TODO: Check updated.Count == 1?
            return DataSyncMsg.Create (
                       state.With (timeEntries: state.UpdateTimeEntries (updated)),
                       updated);
        }

        static DataSyncMsg<AppState> TimeEntryRemove (AppState state, DataMsg msg)
        {
            // The TEs should have been already removed from AppState but try to remove them again just in case
            var entriesData = (msg as DataMsg.TimeEntriesRemove).Data.ForceLeft();
            var dataStore = ServiceContainer.Resolve<ISyncDataStore>();

            var updated = dataStore.Update (ctx => {
                foreach (var entryData in entriesData) {
                    ctx.Delete (entryData.With (x => x.DeletedAt = Time.UtcNow));
                }
            });

            return DataSyncMsg.Create (
                       state.With (timeEntries: state.UpdateTimeEntries (updated)),
                       updated);
        }

        static DataSyncMsg<AppState> TagsPut (AppState state, DataMsg msg)
        {
            var tags = (msg as DataMsg.TagsPut).Data.ForceLeft();
            var dataStore = ServiceContainer.Resolve<ISyncDataStore>();

            var updated = dataStore.Update (ctx => {
                foreach (var tag in tags) {
                    ctx.Put (tag);
                }
            });

            return DataSyncMsg.Create (state.With (tags: state.Update (state.Tags, updated)), updated);
        }

        static DataSyncMsg<AppState> ClientDataPut (AppState state, DataMsg msg)
        {
            var data = (msg as DataMsg.ClientDataPut).Data.ForceLeft();
            var dataStore = ServiceContainer.Resolve<ISyncDataStore>();

            var updated = dataStore.Update (ctx => ctx.Put (data));

            return DataSyncMsg.Create (state.With (clients: state.Update (state.Clients, updated)), updated);
        }

        static DataSyncMsg<AppState> ProjectDataPut (AppState state, DataMsg msg)
        {
            var data = (msg as DataMsg.ProjectDataPut).Data.ForceLeft();
            var dataStore = ServiceContainer.Resolve<ISyncDataStore>();

            var updated = dataStore.Update (ctx => ctx.Put (data));

            return DataSyncMsg.Create (state.With (projects: state.Update (state.Projects, updated)), updated);
        }

        static DataSyncMsg<AppState> UserDataPut (AppState state, DataMsg msg)
        {
            return (msg as DataMsg.UserDataPut).Data.Match (
            userData => {

                // Create user and workspace at the same time,
                // workspace created with default data and will be
                // updated in the next sync.
                var dataStore = ServiceContainer.Resolve<ISyncDataStore> ();
                var updated = dataStore.Update (ctx => { ctx.Put (userData); });

                // This will throw an exception if user hasn't been correctly updated
                var userDataInDb = updated.OfType<UserData> ().Single ();

                return DataSyncMsg.Create (
                           state.With (
                               user: userDataInDb,
                               authResult: AuthResult.Success,
                               workspaces: state.Update (state.Workspaces, updated),
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
                    string.Format ("Cannot {0} a time entry ({1}) in {2} state.",
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
                    ctx.Put (prev.With (x => {
                        x.State = TimeEntryState.Finished;
                        x.StopTime = Time.UtcNow;
                    }));
                }

                ITimeEntryData draft = null;
                if (entryData.Id == Guid.Empty) {
                    draft = state.GetTimeEntryDraft ();
                } else {
                    CheckTimeEntryState (entryData, TimeEntryState.Finished, "continue");
                    draft = entryData;
                }

                ctx.Put (TimeEntryData.Create (draft: draft, transform: x => {
                    x.RemoteId = null;
                    x.State = TimeEntryState.Running;
                    x.StartTime = Time.UtcNow;
                    x.StopTime = null;
                }));
            });

            return DataSyncMsg.Create (
                       state.With (timeEntries: state.UpdateTimeEntries (updated)), updated);
        }

        static DataSyncMsg<AppState> TimeEntryStop (AppState state, DataMsg msg)
        {
            var entryData = (msg as DataMsg.TimeEntryStop).Data.ForceLeft ();
            var dataStore = ServiceContainer.Resolve <ISyncDataStore> ();

            CheckTimeEntryState (entryData, TimeEntryState.Running, "stop");

            var updated = dataStore.Update (ctx => ctx.Put (entryData.With (x => {
                x.State = TimeEntryState.Finished;
                x.StopTime = Time.UtcNow;
            })));

            // TODO: Check updated.Count == 1?
            return DataSyncMsg.Create (
                       state.With (timeEntries: state.UpdateTimeEntries (updated)), updated);
        }

        static DataSyncMsg<AppState> Reset (AppState state, DataMsg msg)
        {
            var dataStore = ServiceContainer.Resolve <ISyncDataStore> ();
            dataStore.WipeTables ();

            // Clear platform settings.
            Settings.SerializedSettings = string.Empty;

            // Reset state
            var appState = AppState.Init ();

            // TODO: Ping analytics?
            // TODO: Call Log service?

            return DataSyncMsg.Create (appState);
        }

        static DataSyncMsg<AppState> UpdateSettings (AppState state, DataMsg msg)
        {
            var info = (msg as DataMsg.UpdateSetting).Data.ForceLeft ();
            SettingsState newSettings = state.Settings;

            if (info.Item1 == nameof (SettingsState.ShowWelcome)) {
                newSettings = newSettings.With (showWelcome: (bool)info.Item2);
            } else if (info.Item1 == nameof (SettingsState.ProjectSort)) {
                newSettings = newSettings.With (projectSort: (string)info.Item2);
            } else if (info.Item1 == nameof (SettingsState.ReportsCurrentItem)) {
                newSettings = newSettings.With (reportsCurrentItem: (int)info.Item2);
            } else if (info.Item1 == nameof (SettingsState.LastReportZoom)) {
                newSettings = newSettings.With (lastReportZoom: (int)info.Item2);
            } else if (info.Item1 == nameof (SettingsState.RossReadDurOnlyNotice)) {
                newSettings = newSettings.With (rossReadDurOnlyNotice: (bool)info.Item2);
            }

            return DataSyncMsg.Create (state.With (settings:newSettings));
        }

        #region Util
        static AppState UpdateStateWithNewData (AppState state, IEnumerable<CommonData> receivedData)
        {
            var dataStore = ServiceContainer.Resolve <ISyncDataStore> ();
            dataStore.Update (ctx => {
                for (var i = 0; i < receivedData.Count (); i++) {
                    ICommonData oldData = null;
                    CommonData newData = receivedData.ElementAt (i);
                    // Check first if the newData has localId assigned
                    // (for example, the ones returned by TogglClient.Create)
                    if (newData.Id != Guid.Empty) {
                        oldData = ctx.GetByColumn (newData.GetType (), nameof (ICommonData.Id), newData.Id);
                    }
                    // If no localId, check if an item with the same RemoteId is in the db
                    else {
                        oldData = ctx.GetByColumn (newData.GetType (), nameof (ICommonData.RemoteId), newData.RemoteId);
                    }

                    if (oldData != null) {
                        // TODO RX check this criteria to compare.
                        // and evaluate if local relations are needed.
                        if (newData.CompareTo (oldData) >= 0) {
                            newData.Id = oldData.Id;
                            var data = BuildLocalRelationships (state, newData); // Set local Id values.
                            PutOrDelete (ctx, data);
                        }
                    } else {
                        newData.Id = Guid.NewGuid (); // Assign new Id
                        newData = BuildLocalRelationships (state, newData); // Set local Id values.
                        PutOrDelete (ctx, newData);
                    }

                    // TODO RX Create a single update method
                    var updatedList = new List<ICommonData> {newData};
                    state = state.With (
                                workspaces: state.Update (state.Workspaces, updatedList),
                                projects: state.Update (state.Projects, updatedList),
                                workspaceUsers: state.Update (state.WorkspaceUsers, updatedList),
                                projectUsers: state.Update (state.ProjectUsers, updatedList),
                                clients: state.Update (state.Clients, updatedList),
                                tasks: state.Update (state.Tasks, updatedList),
                                tags: state.Update (state.Tags, updatedList),
                                timeEntries: state.UpdateTimeEntries (updatedList)
                            );
                }
            });
            return state;
        }

        static CommonData BuildLocalRelationships (AppState state, CommonData data)
        {
            // Build local relationships.
            // Object that comes from server needs to be
            // filled with local Ids.

            if (data is TimeEntryData) {
                var te = (TimeEntryData)data;
                te.UserId = state.User.Id;
                te.WorkspaceId = state.Workspaces.Values.Single (x => x.RemoteId == te.WorkspaceRemoteId).Id;
                if (te.ProjectRemoteId.HasValue &&
                        state.Projects.Any (x => x.Value.RemoteId == te.ProjectRemoteId.Value)) {
                    te.ProjectId = state.Projects.Single (x => x.Value.RemoteId == te.ProjectRemoteId.Value).Value.Id;
                }

                if (te.TaskRemoteId.HasValue &&
                        state.Tasks.Any (x => x.Value.RemoteId == te.TaskRemoteId.Value)) {
                    te.TaskId = state.Tasks.Single (x => x.Value.RemoteId == te.TaskRemoteId.Value).Value.Id;
                }
                return te;
            }

            if (data is ProjectData) {
                var pr = (ProjectData)data;
                pr.WorkspaceId = state.Workspaces.Values.Single (x => x.RemoteId == pr.WorkspaceRemoteId).Id;
                if (pr.ClientRemoteId.HasValue &&
                        state.Clients.Any (x => x.Value.RemoteId == pr.ClientRemoteId.Value)) {
                    pr.ClientId = state.Clients.Single (x => x.Value.RemoteId == pr.ClientRemoteId.Value).Value.Id;
                }
                return pr;
            }

            if (data is ClientData) {
                var cl = (ClientData)data;
                cl.WorkspaceId = state.Workspaces.Values.Single (x => x.RemoteId == cl.WorkspaceRemoteId).Id;
                return cl;
            }

            if (data is TaskData) {
                var ts = (TaskData)data;
                ts.WorkspaceId = state.Workspaces.Values.Single (x => x.RemoteId == ts.WorkspaceRemoteId).Id;
                if (state.Projects.Any (x => x.Value.RemoteId == ts.ProjectRemoteId)) {
                    ts.ProjectId = state.Projects.Single (x => x.Value.RemoteId == ts.ProjectRemoteId).Value.Id;
                }
                return ts;
            }

            if (data is TagData) {
                var t = (TagData)data;
                t.WorkspaceId = state.Workspaces.Values.Single (x => x.RemoteId == t.WorkspaceRemoteId).Id;
                return t;
            }

            if (data is UserData) {
                var u = (UserData)data;
                u.DefaultWorkspaceId = state.Workspaces.Values.Single (x => x.RemoteId == u.DefaultWorkspaceRemoteId).Id;
            }

            return data;
        }

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

