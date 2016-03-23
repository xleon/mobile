using System;
using System.Linq;
using System.Collections.Generic;
using Toggl.Phoebe._Data;
using Toggl.Phoebe._Data.Models;
using Toggl.Phoebe._Helpers;
using Toggl.Phoebe.Logging;
using XPlatUtils;

namespace Toggl.Phoebe._Reactive
{
    public class AppState
    {
		public SettingsState Settings { get; private set; }
        public Net.AuthResult AuthResult { get; private set; }
        public DownloadResult DownloadResult { get; private set; }

        public UserData User { get; private set; }
        public IReadOnlyDictionary<Guid, WorkspaceData> Workspaces { get; private set; }
        public IReadOnlyDictionary<Guid, ProjectData> Projects { get; private set; }
        public IReadOnlyDictionary<Guid, WorkspaceUserData> WorkspaceUsers { get; private set; }
        public IReadOnlyDictionary<Guid, ProjectUserData> ProjectUsers { get; private set; }
        public IReadOnlyDictionary<Guid, ClientData> Clients { get; private set; }
        public IReadOnlyDictionary<Guid, TaskData> Tasks { get; private set; }
        public IReadOnlyDictionary<Guid, TagData> Tags { get; private set; }
        public IReadOnlyDictionary<Guid, RichTimeEntry> TimeEntries { get; private set; }

        // AppState instances are immutable snapshots, so it's safe to use a cache for ActiveEntry
        private RichTimeEntry _activeEntryCache = null;
        public RichTimeEntry ActiveEntry
        {
            get {
                if (_activeEntryCache == null) {
                    _activeEntryCache = new RichTimeEntry (this, new TimeEntryData ());
                    if (TimeEntries.Count > 0)
                        _activeEntryCache = TimeEntries.Values.SingleOrDefault (
                            x => x.Data.State == TimeEntryState.Running) ?? _activeEntryCache;
                }
                return _activeEntryCache;
            }
        }

        AppState (
            SettingsState settings,
            Net.AuthResult authResult,
            DownloadResult downloadResult,
            UserData user,
            IReadOnlyDictionary<Guid, WorkspaceData> workspaces,
            IReadOnlyDictionary<Guid, ProjectData> projects,
            IReadOnlyDictionary<Guid, WorkspaceUserData> workspaceUsers,
            IReadOnlyDictionary<Guid, ProjectUserData> projectUsers,
            IReadOnlyDictionary<Guid, ClientData> clients,
            IReadOnlyDictionary<Guid, TaskData> tasks,
            IReadOnlyDictionary<Guid, TagData> tags,
            IReadOnlyDictionary<Guid, RichTimeEntry> timeEntries)
        {
            Settings = settings;
            AuthResult = authResult;
            DownloadResult = downloadResult;
            User = user;
            Workspaces = workspaces;
            Projects = projects;
            WorkspaceUsers = workspaceUsers;
            ProjectUsers = projectUsers;
            Clients = clients;
            Tasks = tasks;
            Tags = tags;
            TimeEntries = timeEntries;
        }

        public AppState With (
            SettingsState settings = null,
            Net.AuthResult? authResult = null,
            DownloadResult downloadResult = null,
            UserData user = null,
            IReadOnlyDictionary<Guid, WorkspaceData> workspaces = null,
            IReadOnlyDictionary<Guid, ProjectData> projects = null,
            IReadOnlyDictionary<Guid, WorkspaceUserData> workspaceUsers = null,
            IReadOnlyDictionary<Guid, ProjectUserData> projectUsers = null,
            IReadOnlyDictionary<Guid, ClientData> clients = null,
            IReadOnlyDictionary<Guid, TaskData> tasks = null,
            IReadOnlyDictionary<Guid, TagData> tags = null,
            IReadOnlyDictionary<Guid, RichTimeEntry> timeEntries = null)
        {
            return new AppState (
                settings ?? Settings,
                authResult ?? AuthResult,
                downloadResult ?? DownloadResult,
                user ?? User,
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

        public static AppState Init ()
        {
			var userData = new UserData ();
            var settings = SettingsState.Init ();
            try {
                if (settings.UserId != Guid.Empty) {
                    var dataStore = ServiceContainer.Resolve<ISyncDataStore> ();
                    userData = dataStore.Table<UserData> ().Single (x => x.Id == settings.UserId);
                }
            } catch (Exception ex) {
                var logger = ServiceContainer.Resolve<ILogger> ();
                logger.Error (typeof (AppState).Name, ex, "UserId in settings not found in db: {0}", ex.Message);
                // When data is corrupt and cannot find user
                settings = settings.With (userId: Guid.Empty);
            }

            return new AppState (
                settings: settings,
                authResult: Net.AuthResult.None,
                downloadResult: DownloadResult.Empty,
                user: userData,
                workspaces: new Dictionary<Guid, WorkspaceData> (),
                projects: new Dictionary<Guid, ProjectData> (),
                workspaceUsers: new Dictionary<Guid, WorkspaceUserData> (),
                projectUsers: new Dictionary<Guid, ProjectUserData> (),
                clients: new Dictionary<Guid, ClientData> (),
                tasks: new Dictionary<Guid, TaskData> (),
                tags: new Dictionary<Guid, TagData> (),
                timeEntries: new Dictionary<Guid, RichTimeEntry> ());
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

        public RichTimeEntry (AppState appState, ITimeEntryData data)
            : this (data, appState.LoadTimeEntryInfo (data))
        {
        }

        public override int GetHashCode ()
        {
            return Data.GetHashCode ();
        }

        public override bool Equals (object obj)
        {
            if (ReferenceEquals (this, obj)) {
                return true;
            }
            else {
                // Quick way to compare time entries
                var other = obj as RichTimeEntry;
                return other != null &&
                       Data.Id == other.Data.Id &&
                       Data.ModifiedAt == other.Data.ModifiedAt &&
                       Data.DeletedAt == other.Data.DeletedAt;
            }
        }
    }

    public class ActiveEntryInfo
    {
        public Guid Id { get; private set; }

        public static ActiveEntryInfo Empty
        {
            get { return new ActiveEntryInfo (Guid.Empty); }
        }

        public ActiveEntryInfo (Guid id)
        {
            Id = id;
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
                       isSyncing.HasValue ? isSyncing.Value : IsSyncing,
                       hasMore.HasValue ? hasMore.Value : HasMore,
                       hadErrors.HasValue ? hadErrors.Value : HadErrors,
                       downloadFrom.HasValue ? downloadFrom.Value : DownloadFrom,
                       nextDownloadFrom.HasValue ? nextDownloadFrom.Value : NextDownloadFrom);
        }
    }

    public class SettingsState
    {
        // TODO RX: Check these correspond to new _Helpers.Settings class properties
        public Guid UserId { get; private set; }
        public DateTime SyncLastRun { get; private set; }
        public bool UseDefaultTag { get; private set; }
        public string LastAppVersion { get; private set; }
        public string ExperimentId { get; private set; }
        public int LastReportZoom { get; private set; }
        public bool GroupedEntries { get; private set; }
        public string ProjectSort { get; private set; }

        SettingsState (
            Guid userId,
            DateTime syncLastRun,
            bool useDefaultTag,
            string lastAppVersion,
            int lastReportZoom,
            bool groupedEntries,
            string projectSort)
        {
            UserId = userId;
            SyncLastRun = syncLastRun;
            UseDefaultTag = useDefaultTag;
            LastAppVersion = lastAppVersion;
            LastReportZoom = lastReportZoom;
            GroupedEntries = groupedEntries;
            ProjectSort = projectSort;
        }

        public static SettingsState Init ()
        {
            return new SettingsState (
                Settings.UserId,
                Settings.SyncLastRun,
                Settings.UseDefaultTag,
                Settings.LastAppVersion,
                Settings.LastReportZoom,
                Settings.GroupedEntries,
                Settings.ProjectSort
            );
        }

        private T updateNullable<T> (T? value, T @default, Func<T,T> update)
        where T : struct
        {
            return value.HasValue ? update (value.Value) : @default;
        }

        private T updateReference<T> (T value, T @default, Func<T,T> update)
            where T : class
        {
            return value != null ? update (value) : @default;
        }

        public SettingsState With (
            Guid? userId = null,
            DateTime? syncLastRun = null,
            bool? useDefaultTag = null,
            string lastAppVersion = null,
            int? lastReportZoom = null,
            bool? groupedEntries = null,
            string projectSort = null)
        {
            return new SettingsState (
                updateNullable (userId, UserId, x => Settings.UserId = x),
                updateNullable (syncLastRun, SyncLastRun, x => Settings.SyncLastRun = x),
                updateNullable (useDefaultTag, UseDefaultTag, x => Settings.UseDefaultTag = x),
                updateReference (lastAppVersion, LastAppVersion, x => Settings.LastAppVersion = x),
                updateNullable (lastReportZoom, LastReportZoom, x => Settings.LastReportZoom = x),
                updateNullable (groupedEntries, GroupedEntries, x => Settings.GroupedEntries = x),
                updateReference (projectSort, ProjectSort, x => Settings.ProjectSort = x)
            );
        }
    }
}

