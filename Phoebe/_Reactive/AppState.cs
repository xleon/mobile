using System;
using System.Linq;
using System.Collections.Generic;
using Toggl.Phoebe._Data;
using Toggl.Phoebe._Data.Models;
using XPlatUtils;
using Toggl.Phoebe.Logging;
using System.Reflection;
using System.Linq.Expressions;

namespace Toggl.Phoebe._Reactive
{
    public interface IWithDictionary<T>
    {
        T WithDictionary (IReadOnlyDictionary<string, object> dic);
    }

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

    public class PropertyCompositeReducer<T> : Reducer<T>, IReducer
        where T : IWithDictionary<T>
    {
        readonly List<Tuple<PropertyInfo, IReducer>> reducers = new List<Tuple<PropertyInfo, IReducer>> ();

        public PropertyCompositeReducer<T> Add<TPart> (
            Expression<Func<T,TPart>> selector,
            Func<TPart, DataMsg, DataSyncMsg<TPart>> reducer)
        {
            return Add (selector, new Reducer<TPart> (reducer));
        }

        public PropertyCompositeReducer<T> Add<TPart> (Expression<Func<T,TPart>> selector, Reducer<TPart> reducer)
        {
            var memberExpr = selector.Body as MemberExpression;
            var member = memberExpr.Member as PropertyInfo;

            if (memberExpr == null)
                throw new ArgumentException (string.Format (
                                                 "Expression '{0}' should be a property.",
                                                 selector.ToString()));
            if (member == null)
                throw new ArgumentException (string.Format (
                                                 "Expression '{0}' should be a constant expression",
                                                 selector.ToString()));

            reducers.Add (Tuple.Create (member, (IReducer)reducer));
            return this;
        }

        public override DataSyncMsg<T> Reduce (T state, DataMsg msg)
        {
            var syncData = new List<ICommonData> ();
            var serverRequests = new List<ServerRequest> ();
            var dic = new Dictionary<string, object> ();

            foreach (var reducer in reducers) {
                var propValue = reducer.Item1.GetValue (state);
                var res = reducer.Item2.Reduce (propValue, msg);

                dic.Add (reducer.Item1.Name, res.State);
                syncData.AddRange (res.SyncData);
                serverRequests.AddRange (res.ServerRequests);
            }

            return new DataSyncMsg<T> (state.WithDictionary (dic), syncData, serverRequests);
        }

        DataSyncMsg<object> IReducer.Reduce (object state, DataMsg msg)
        {
            return Reduce ((T)state, msg).Cast<object> ();
        }
    }

    public class AppState : IWithDictionary<AppState>
    {
        public TimerState TimerState { get; private set; }

        public AppState (
            TimerState timerState)
        {
            TimerState = timerState;
        }

        public AppState WithDictionary (IReadOnlyDictionary<string, object> dic)
        {
            return new AppState (
                       dic.ContainsKey (nameof (TimerState)) ? (TimerState)dic[nameof (TimerState)] : this.TimerState);
        }

        public static AppState Init ()
        {
            var userData = new UserData ();
            var credStore = ServiceContainer.Resolve<Data.ISettingsStore> ();
            try {
                var userId = credStore.UserId;
                if (userId.HasValue) {
                    var dataStore = ServiceContainer.Resolve<ISyncDataStore> ();
                    userData = dataStore.Table<UserData> ().Single (x => x.Id == userId.Value);
                }
            } catch (ArgumentException) {
                // When data is corrupt and cannot find user
                credStore.UserId = null;
            }

            var timerState =
                new TimerState (
                authResult: Net.AuthResult.None,
                downloadResult: DownloadResult.Empty,
                user: userData,
                activeEntry: ActiveEntryInfo.Empty,
                workspaces: new Dictionary<Guid, WorkspaceData> (),
                projects: new Dictionary<Guid, ProjectData> (),
                workspaceUsers: new Dictionary<Guid, WorkspaceUserData> (),
                projectUsers: new Dictionary<Guid, ProjectUserData> (),
                clients: new Dictionary<Guid, ClientData> (),
                tasks: new Dictionary<Guid, TaskData> (),
                tags: new Dictionary<Guid, TagData> (),
                timeEntries: new Dictionary<Guid, RichTimeEntry> ());

            return new AppState (timerState);
        }
    }

    public class RichTimeEntry
    {
        public TimeEntryInfo Info { get; private set; }
        public ITimeEntryData Data { get; private set; }

        public RichTimeEntry (ITimeEntryData data, TimeEntryInfo info)
        {
            Data = (ITimeEntryData)data.Clone ();
            Info = info;
        }

        public RichTimeEntry (TimerState timerState, ITimeEntryData data)
        : this (data, timerState.LoadTimeEntryInfo (data))
        {
        }
    }

    public class ActiveEntryInfo
    {
        public Guid Id { get; private set; }
        public bool StartedByFAB { get; private set; }

        public static ActiveEntryInfo Empty
        {
            get { return new ActiveEntryInfo (Guid.Empty, false); }
        }

        public ActiveEntryInfo (Guid id, bool startedByFAB)
        {
            Id = id;
            StartedByFAB = startedByFAB;
        }
    }

    public class DownloadResult
    {
        public bool IsSyncing { get; private set; }
        public bool HasMore { get; private set; }
        public bool HadErrors { get; private set; }
        public DateTime DownloadFrom { get; private set; }
        public DateTime NextDownloadFrom { get; private set; }

        public static DownloadResult Empty
        {
            get {
                // Set initial pagination Date to the beginning of the next day.
                // So, we can include all entries created -Today-.
                var downloadFrom = Time.UtcNow.Date.AddDays (1);
                return new DownloadResult (false, true, false, downloadFrom, downloadFrom);
            }
        }

        public DownloadResult (
            bool isSyncing, bool hasMore, bool hadErrors,
            DateTime downloadFrom, DateTime nextDownloadFrom)
        {
            IsSyncing = isSyncing;
            HasMore = hasMore;
            HadErrors = hadErrors;
            DownloadFrom = downloadFrom;
            NextDownloadFrom = nextDownloadFrom;
        }

        public DownloadResult With (
            bool? isSyncing = null,
            bool? hasMore = null,
            bool? hadErrors = null,
            DateTime? downloadFrom = null,
            DateTime? nextDownloadFrom = null)
        {
            return new DownloadResult (
                       isSyncing.HasValue ? isSyncing.Value : this.IsSyncing,
                       hasMore.HasValue ? hasMore.Value : this.HasMore,
                       hadErrors.HasValue ? hadErrors.Value : this.HadErrors,
                       downloadFrom.HasValue ? downloadFrom.Value : this.DownloadFrom,
                       nextDownloadFrom.HasValue ? nextDownloadFrom.Value : this.NextDownloadFrom);
        }
    }

    public class TimerState
    {
        public Net.AuthResult AuthResult { get; private set; }
        public DownloadResult DownloadResult { get; private set; }

        public UserData User { get; private set; }
        public ActiveEntryInfo ActiveEntry { get; private set; }
        public IReadOnlyDictionary<Guid, WorkspaceData> Workspaces { get; private set; }
        public IReadOnlyDictionary<Guid, ProjectData> Projects { get; private set; }
        public IReadOnlyDictionary<Guid, WorkspaceUserData> WorkspaceUsers { get; private set; }
        public IReadOnlyDictionary<Guid, ProjectUserData> ProjectUsers { get; private set; }
        public IReadOnlyDictionary<Guid, ClientData> Clients { get; private set; }
        public IReadOnlyDictionary<Guid, TaskData> Tasks { get; private set; }
        public IReadOnlyDictionary<Guid, TagData> Tags { get; private set; }
        public IReadOnlyDictionary<Guid, RichTimeEntry> TimeEntries { get; private set; }

        public TimerState (
            Net.AuthResult authResult,
            DownloadResult downloadResult,
            UserData user,
            ActiveEntryInfo activeEntry,
            IReadOnlyDictionary<Guid, WorkspaceData> workspaces,
            IReadOnlyDictionary<Guid, ProjectData> projects,
            IReadOnlyDictionary<Guid, WorkspaceUserData> workspaceUsers,
            IReadOnlyDictionary<Guid, ProjectUserData> projectUsers,
            IReadOnlyDictionary<Guid, ClientData> clients,
            IReadOnlyDictionary<Guid, TaskData> tasks,
            IReadOnlyDictionary<Guid, TagData> tags,
            IReadOnlyDictionary<Guid, RichTimeEntry> timeEntries)
        {
            AuthResult = authResult;
            DownloadResult = downloadResult;
            User = user;
            ActiveEntry = activeEntry;
            Workspaces = workspaces;
            Projects = projects;
            WorkspaceUsers = workspaceUsers;
            ProjectUsers = projectUsers;
            Clients = clients;
            Tasks = tasks;
            Tags = tags;
            TimeEntries = timeEntries;
        }

        public TimerState With (
            Net.AuthResult? authResult = null,
            DownloadResult downloadResult = null,
            UserData user = null,
            ActiveEntryInfo activeEntry = null,
            IReadOnlyDictionary<Guid, WorkspaceData> workspaces = null,
            IReadOnlyDictionary<Guid, ProjectData> projects = null,
            IReadOnlyDictionary<Guid, WorkspaceUserData> workspaceUsers = null,
            IReadOnlyDictionary<Guid, ProjectUserData> projectUsers = null,
            IReadOnlyDictionary<Guid, ClientData> clients = null,
            IReadOnlyDictionary<Guid, TaskData> tasks = null,
            IReadOnlyDictionary<Guid, TagData> tags = null,
            IReadOnlyDictionary<Guid, RichTimeEntry> timeEntries = null)
        {
            return new TimerState (
                       authResult ?? AuthResult,
                       downloadResult ?? DownloadResult,
                       user ?? User,
                       activeEntry ?? ActiveEntry,
                       workspaces ?? Workspaces,
                       projects ?? Projects,
                       workspaceUsers ?? WorkspaceUsers,
                       projectUsers ?? ProjectUsers,
                       clients ?? Clients,
                       tasks ?? Tasks,
                       tags ?? Tags,
                       timeEntries ?? TimeEntries);
        }

        /// <summary>
        /// This doesn't check ModifiedAt or DeletedAt, so call it
        /// always after putting items first in the database
        /// </summary>
        public IReadOnlyDictionary<Guid, T> Update<T> (
            IReadOnlyDictionary<Guid, T> oldItems, IEnumerable<ICommonData> newItems)
        where T : CommonData
        {
            var dic = oldItems.ToDictionary (x => x.Key, x => x.Value);
            foreach (var newItem in newItems.OfType<T> ()) {
                if (newItem.DeletedAt == null) {
                    if (dic.ContainsKey (newItem.Id)) {
                        dic [newItem.Id] = newItem;
                    } else {
                        dic.Add (newItem.Id, newItem);
                    }
                } else {
                    if (dic.ContainsKey (newItem.Id)) {
                        dic.Remove (newItem.Id);
                    }
                }
            }
            return dic;
        }

        /// <summary>
        /// This doesn't check ModifiedAt or DeletedAt, so call it
        /// always after putting items first in the database
        /// </summary>
        public IReadOnlyDictionary<Guid, RichTimeEntry> UpdateTimeEntries (
            IEnumerable<ICommonData> newItems)
        {
            var dic = TimeEntries.ToDictionary (x => x.Key, x => x.Value);
            foreach (var newItem in newItems.OfType<ITimeEntryData> ()) {
                if (newItem.DeletedAt == null) {
                    if (dic.ContainsKey (newItem.Id)) {
                        dic [newItem.Id] = new RichTimeEntry (
                            newItem, LoadTimeEntryInfo (newItem));
                    } else {
                        dic.Add (newItem.Id, new RichTimeEntry (
                                     newItem, LoadTimeEntryInfo (newItem)));
                    }
                } else {
                    if (dic.ContainsKey (newItem.Id)) {
                        dic.Remove (newItem.Id);
                    }
                }
            }
            return dic;
        }

        public TimeEntryInfo LoadTimeEntryInfo (ITimeEntryData teData)
        {
            var workspaceData = teData.WorkspaceId != Guid.Empty ? Workspaces[teData.WorkspaceId] : new WorkspaceData ();
            var projectData = teData.ProjectId != Guid.Empty ? Projects[teData.ProjectId] : new ProjectData ();
            var clientData = projectData.ClientId != Guid.Empty ? Clients[projectData.ClientId] : new ClientData ();
            var taskData = teData.TaskId != Guid.Empty ? Tasks[teData.TaskId] : new TaskData ();
            var color = (projectData.Id != Guid.Empty) ? projectData.Color : -1;
            var tagsData =
                teData.Tags.Select (
                    x => Tags.Values.SingleOrDefault (y => y.WorkspaceId == teData.WorkspaceId && y.Name == x))
                // TODO: Throw exception if tag was not found?
                .Where (x => x != null)
                .ToList ();

            return new TimeEntryInfo (
                       workspaceData,
                       projectData,
                       clientData,
                       taskData,
                       tagsData,
                       color);
        }

        public IEnumerable<ProjectData> GetUserAccessibleProjects (Guid userId)
        {
            return Projects.Values.Where (
                       p => p.IsActive && (p.IsPrivate || ProjectUsers.Values.Any (x => x.ProjectId == p.Id && x.UserId == userId)))
                   .OrderBy (p => p.Name);
        }

        public TimeEntryData GetTimeEntryDraft ()
        {
            var userId = User.Id;
            var workspaceId = User.DefaultWorkspaceId;
            var remoteWorkspaceId = User.DefaultWorkspaceRemoteId;
            var durationOnly = User.TrackingMode == TrackingMode.Continue;

            // Create new draft object
            return new TimeEntryData {
                State = TimeEntryState.New,
                UserId = userId,
                WorkspaceId = workspaceId,
                WorkspaceRemoteId = remoteWorkspaceId,
                DurationOnly = durationOnly,
            };
        }
    }
}

