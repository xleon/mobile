using System;
using System.Linq;
using System.Collections.Generic;
using Toggl.Phoebe.Data;
using Toggl.Phoebe.Data.Models;
using Toggl.Phoebe.Helpers;
using Toggl.Phoebe.Logging;
using Toggl.Phoebe.Net;
using XPlatUtils;

namespace Toggl.Phoebe.Reactive
{
    public class AppState
    {
        public SettingsState Settings { get; private set; }
        public RequestInfo RequestInfo { get; private set; }

        public IUserData User { get; private set; }
        public IReadOnlyDictionary<Guid, IWorkspaceData> Workspaces { get; private set; }
        public IReadOnlyDictionary<Guid, IProjectData> Projects { get; private set; }
        public IReadOnlyDictionary<Guid, IWorkspaceUserData> WorkspaceUsers { get; private set; }
        public IReadOnlyDictionary<Guid, IProjectUserData> ProjectUsers { get; private set; }
        public IReadOnlyDictionary<Guid, IClientData> Clients { get; private set; }
        public IReadOnlyDictionary<Guid, ITaskData> Tasks { get; private set; }
        public IReadOnlyDictionary<Guid, ITagData> Tags { get; private set; }
        public IReadOnlyDictionary<Guid, RichTimeEntry> TimeEntries { get; private set; }

        // AppState instances are immutable snapshots, so it's safe to use a cache for ActiveEntry
        private RichTimeEntry _activeEntryCache = null;
        public RichTimeEntry ActiveEntry
        {
            get
            {
                if (_activeEntryCache == null)
                {
                    _activeEntryCache = new RichTimeEntry(new TimeEntryData(), this);
                    if (TimeEntries.Count > 0)
                        _activeEntryCache = TimeEntries.Values.SingleOrDefault(
                                                x => x.Data.State == TimeEntryState.Running) ?? _activeEntryCache;
                }
                return _activeEntryCache;
            }
        }

        AppState(
            SettingsState settings,
            RequestInfo requestInfo,
            IUserData user,
            IReadOnlyDictionary<Guid, IWorkspaceData> workspaces,
            IReadOnlyDictionary<Guid, IProjectData> projects,
            IReadOnlyDictionary<Guid, IWorkspaceUserData> workspaceUsers,
            IReadOnlyDictionary<Guid, IProjectUserData> projectUsers,
            IReadOnlyDictionary<Guid, IClientData> clients,
            IReadOnlyDictionary<Guid, ITaskData> tasks,
            IReadOnlyDictionary<Guid, ITagData> tags,
            IReadOnlyDictionary<Guid, RichTimeEntry> timeEntries)
        {
            Settings = settings;
            RequestInfo = requestInfo;
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

        public AppState With(
            SettingsState settings = null,
            RequestInfo requestInfo = null,
            IUserData user = null,
            IReadOnlyDictionary<Guid, IWorkspaceData> workspaces = null,
            IReadOnlyDictionary<Guid, IProjectData> projects = null,
            IReadOnlyDictionary<Guid, IWorkspaceUserData> workspaceUsers = null,
            IReadOnlyDictionary<Guid, IProjectUserData> projectUsers = null,
            IReadOnlyDictionary<Guid, IClientData> clients = null,
            IReadOnlyDictionary<Guid, ITaskData> tasks = null,
            IReadOnlyDictionary<Guid, ITagData> tags = null,
            IReadOnlyDictionary<Guid, RichTimeEntry> timeEntries = null)
        {
            return new AppState(
                       settings ?? Settings,
                       requestInfo ?? RequestInfo,
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
        where T : ICommonData
        {
            var dic = oldItems.ToDictionary(x => x.Key, x => x.Value);
            foreach (var newItem in newItems.OfType<T> ())
            {
                if (newItem.DeletedAt == null)
                {
                    if (dic.ContainsKey(newItem.Id))
                    {
                        dic [newItem.Id] = newItem;
                    }
                    else
                    {
                        dic.Add(newItem.Id, newItem);
                    }
                }
                else
                {
                    if (dic.ContainsKey(newItem.Id))
                    {
                        dic.Remove(newItem.Id);
                    }
                }
            }
            return dic;
        }

        /// <summary>
        /// This doesn't check ModifiedAt or DeletedAt, so call it
        /// always after putting items first in the database
        /// </summary>
        public IReadOnlyDictionary<Guid, RichTimeEntry> UpdateTimeEntries(
            IEnumerable<ICommonData> newItems)
        {
            var dic = TimeEntries.ToDictionary(x => x.Key, x => x.Value);
            foreach (var newItem in newItems.OfType<ITimeEntryData> ())
            {
                if (newItem.DeletedAt == null)
                {
                    if (dic.ContainsKey(newItem.Id))
                    {
                        dic [newItem.Id] = new RichTimeEntry(
                            newItem, LoadTimeEntryInfo(newItem));
                    }
                    else
                    {
                        dic.Add(newItem.Id, new RichTimeEntry(
                                    newItem, LoadTimeEntryInfo(newItem)));
                    }
                }
                else
                {
                    if (dic.ContainsKey(newItem.Id))
                    {
                        dic.Remove(newItem.Id);
                    }
                }
            }
            return dic;
        }

        public TimeEntryInfo LoadTimeEntryInfo(ITimeEntryData teData)
        {
            var workspaceData = teData.WorkspaceId != Guid.Empty ? Workspaces[teData.WorkspaceId] : new WorkspaceData();
            var projectData = teData.ProjectId != Guid.Empty ? Projects[teData.ProjectId] : new ProjectData();
            var clientData = projectData.ClientId != Guid.Empty ? Clients[projectData.ClientId] : new ClientData();
            var taskData = teData.TaskId != Guid.Empty ? Tasks[teData.TaskId] : new TaskData();
            var color = (projectData.Id != Guid.Empty) ? projectData.Color : -1;
            var tagsData =
                teData.TagIds.Select(x => Tags.Values.SingleOrDefault(y => y.Id == x))
                // TODO: Throw exception if tag was not found?
                .Where(x => x != null)
                .ToList();

            return new TimeEntryInfo(
                       workspaceData,
                       projectData,
                       clientData,
                       taskData,
                       tagsData,
                       color);
        }

        public IEnumerable<IProjectData> GetUserAccessibleProjects(Guid userId)
        {
            return Projects.Values.Where(
                       p => p.IsActive &&
                       (p.IsPrivate || ProjectUsers.Values.Any(x => x.ProjectId == p.Id && x.UserId == userId)))
                   .OrderBy(p => p.Name);
        }

        public ITimeEntryData GetTimeEntryDraft()
        {
            var userId = User.Id;
            var workspaceId = User.DefaultWorkspaceId;
            var remoteWorkspaceId = User.DefaultWorkspaceRemoteId;
            var durationOnly = User.TrackingMode == TrackingMode.Continue;

            // Create new draft object
            return TimeEntryData.Create(x =>
            {
                x.UserId = userId;
                x.WorkspaceId = workspaceId;
                x.WorkspaceRemoteId = remoteWorkspaceId;
                x.DurationOnly = durationOnly;
            });
        }

        public static AppState Init()
        {
            var userData = new UserData();
            var settings = SettingsState.Init();
            var projects = new Dictionary<Guid, IProjectData> ();
            var projectUsers = new Dictionary<Guid, IProjectUserData> ();
            var workspaces = new Dictionary<Guid, IWorkspaceData> ();
            var workspaceUserData = new Dictionary<Guid, IWorkspaceUserData> ();
            var clients = new Dictionary<Guid, IClientData> ();
            var tasks = new Dictionary<Guid, ITaskData> ();
            var tags = new Dictionary<Guid, ITagData> ();

            try
            {
                if (settings.UserId != Guid.Empty)
                {
                    var dataStore = ServiceContainer.Resolve<ISyncDataStore> ();
                    userData = dataStore.Table<UserData> ().Single(x => x.Id == settings.UserId);
                    dataStore.Table<WorkspaceData> ().ForEach(x => workspaces.Add(x.Id, x));
                    dataStore.Table<WorkspaceUserData> ().ForEach(x => workspaceUserData.Add(x.Id, x));
                    dataStore.Table<ProjectData> ().ForEach(x => projects.Add(x.Id, x));
                    dataStore.Table<ProjectUserData> ().ForEach(x => projectUsers.Add(x.Id, x));
                    dataStore.Table<ClientData> ().ForEach(x => clients.Add(x.Id, x));
                    dataStore.Table<TaskData> ().ForEach(x => tasks.Add(x.Id, x));
                    dataStore.Table<TagData> ().ForEach (x => tags.Add (x.Id, x));
                }
            }
            catch (Exception ex)
            {
                var logger = ServiceContainer.Resolve<ILogger> ();
                logger.Error(typeof(AppState).Name, ex, "UserId in settings not found in db: {0}", ex.Message);
                // When data is corrupt and cannot find user
                settings = settings.With(userId: Guid.Empty);
            }

            return new AppState(
                       settings: settings,
                       requestInfo: RequestInfo.Empty,
                       user: userData,
                       workspaces: workspaces,
                       projects: projects,
                       workspaceUsers: workspaceUserData,
                       projectUsers: projectUsers,
                       clients: clients,
                       tasks: tasks,
                       tags: tags,
                       timeEntries: new Dictionary<Guid, RichTimeEntry> ());
        }
    }

    public class RichTimeEntry
    {
        public TimeEntryInfo Info { get; private set; }
        public ITimeEntryData Data { get; private set; }

        public RichTimeEntry(ITimeEntryData data, TimeEntryInfo info)
        {
            Data = data;
            Info = info;
        }

        public RichTimeEntry(ITimeEntryData data, AppState appState)
        : this(data, appState.LoadTimeEntryInfo(data))
        {
        }

        public override int GetHashCode()
        {
            return Data.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(this, obj))
            {
                return true;
            }
            else
            {
                // Quick way to compare time entries
                var other = obj as RichTimeEntry;
                return other != null &&
                       Data.Id == other.Data.Id &&
                       Data.ModifiedAt == other.Data.ModifiedAt &&
                       Data.DeletedAt == other.Data.DeletedAt;
            }
        }
    }

    public class RequestInfo
    {
        /// <summary>
        /// Request types in process
        /// </summary>
        public IReadOnlyList<ServerRequest> Running { get; private set; }

        /// <summary>
        /// Are there more entries available to download from the server?
        /// </summary>
        public bool HasMoreEntries { get; private set; }

        /// <summary>
        /// True if last request completed with errors
        /// </summary>
        public bool HadErrors { get; private set; }

        /// <summary>
        /// Date used by DownloadEntries
        /// </summary>
        public DateTime DownloadFrom { get; private set; }

        /// <summary>
        /// What date to use next time DownloadEntries is run
        /// </summary>
        public DateTime NextDownloadFrom { get; private set; }

        /// <summary>
        /// Last time changes were requested from the server
        /// </summary>
        public DateTime GetChangesLastRun { get; private set; }

        /// <summary>
        /// Result of authentication request
        /// </summary>
        public AuthResult AuthResult { get; private set; }

        public static RequestInfo Empty
        {
            get
            {
                // Set initial pagination Date to the beginning of the next day.
                // So, we can include all entries created -Today-.
                var downloadFrom = Time.UtcNow.Date.AddDays(1);

                // Initial Date for GetChanges: the last 5 days.
                var getChangesLastRun = Time.UtcNow.AddDays(-5);

                return new RequestInfo(
                           new List<ServerRequest> (), true, false,
                           downloadFrom, downloadFrom,
                           getChangesLastRun, AuthResult.None);
            }
        }

        public RequestInfo(
            IReadOnlyList<ServerRequest> running, bool hasMore, bool hadErrors,
            DateTime downloadFrom, DateTime nextDownloadFrom,
            DateTime getChangesLastRun, AuthResult authResult)
        {
            Running = running;
            HasMoreEntries = hasMore;
            HadErrors = hadErrors;
            DownloadFrom = downloadFrom;
            NextDownloadFrom = nextDownloadFrom;
            GetChangesLastRun = getChangesLastRun;
            AuthResult = authResult;
        }

        public RequestInfo With(
            IReadOnlyList<ServerRequest> running = null,
            bool? hasMore = null,
            bool? hadErrors = null,
            DateTime? downloadFrom = null,
            DateTime? nextDownloadFrom = null,
            DateTime? getChangesLastRun = null,
            AuthResult? authResult = null)
        {
            return new RequestInfo(
                       running != null ? running : Running,
                       hasMore.HasValue ? hasMore.Value : HasMoreEntries,
                       hadErrors.HasValue ? hadErrors.Value : HadErrors,
                       downloadFrom.HasValue ? downloadFrom.Value : DownloadFrom,
                       nextDownloadFrom.HasValue ? nextDownloadFrom.Value : NextDownloadFrom,
                       getChangesLastRun.HasValue ? getChangesLastRun.Value : GetChangesLastRun,
                       authResult.HasValue ? authResult.Value : AuthResult);
        }
    }

    public class SettingsState
    {
        // Common Default values
        private static readonly Guid UserIdDefault = Guid.Empty;
        private static readonly DateTime GetChangesLastRunDefault = DateTime.MinValue;
        private static readonly bool UseDefaultTagDefault = true;
        private static readonly string LastAppVersionDefault = string.Empty;
        private static readonly int LastReportZoomDefault = (int)ZoomLevel.Week;
        private static readonly bool GroupedEntriesDefault = false;
        private static readonly bool ChooseProjectForNewDefault = false;
        private static readonly int ReportsCurrentItemDefault = 0;
        private static readonly string ProjectSortDefault = "Clients";
        private static readonly string InstallIdDefault = string.Empty;
        // iOS only Default values
        private static readonly string RossPreferredStartViewDefault = string.Empty;
        private static readonly bool RossReadDurOnlyNoticeDefault = false;
        private static readonly DateTime RossIgnoreSyncErrorsUntilDefault = DateTime.MinValue;
        // Android only Default values
        private static readonly string GcmRegistrationIdDefault = string.Empty;
        private static readonly string GcmAppVersionDefault = string.Empty;
        private static readonly bool IdleNotificationDefault = true;
        private static readonly bool ShowNotificationDefault = true;
        private static readonly bool ShowWelcomeDefault = false;

        // Common values
        public Guid UserId {get; private set; }
        public DateTime GetChangesLastRun { get; private set; }
        public bool UseDefaultTag { get; private set; }
        public string LastAppVersion { get; private set; }
        public int LastReportZoom { get; private set; }
        public bool GroupedEntries { get; private set; }
        public bool ChooseProjectForNew { get; private set; }
        public int ReportsCurrentItem { get; private set; }
        public string ProjectSort { get; private set; }
        public string InstallId  { get; private set; }
        // iOS only  values
        public string RossPreferredStartView { get; private set; }
        public bool RossReadDurOnlyNotice { get; private set; }
        public DateTime RossIgnoreSyncErrorsUntil { get; private set; }
        // Android only  values
        public string GcmRegistrationId { get; private set; }
        public string GcmAppVersion { get; private set; }
        public bool IdleNotification { get; private set; }
        public bool ShowNotification { get; private set; }
        public bool ShowWelcome { get; private set; }

        public static SettingsState Init()
        {
            // If saved is empty, return default.
            if (Settings.SerializedSettings == string.Empty)
            {
                var settings = new SettingsState();
                settings.UserId = UserIdDefault;
                settings.GetChangesLastRun = GetChangesLastRunDefault;
                settings.UseDefaultTag = UseDefaultTagDefault;
                settings.LastAppVersion = LastAppVersionDefault;
                settings.LastReportZoom = LastReportZoomDefault;
                settings.GroupedEntries = GroupedEntriesDefault;
                settings.ChooseProjectForNew = ChooseProjectForNewDefault;
                settings.ReportsCurrentItem = ReportsCurrentItemDefault;
                settings.ProjectSort = ProjectSortDefault;
                settings.InstallId = InstallIdDefault;
                // iOS only  values
                settings.RossPreferredStartView = RossPreferredStartViewDefault;
                settings.RossReadDurOnlyNotice = RossReadDurOnlyNoticeDefault;
                settings.RossIgnoreSyncErrorsUntil = RossIgnoreSyncErrorsUntilDefault;
                // Android only  values
                settings.GcmRegistrationId = GcmRegistrationIdDefault;
                settings.GcmAppVersion = GcmAppVersionDefault;
                settings.IdleNotification = IdleNotificationDefault;
                settings.ShowNotification = ShowNotificationDefault;
                settings.ShowNotification = ShowWelcomeDefault;
                return settings;
            }
            return Newtonsoft.Json.JsonConvert.DeserializeObject<SettingsState> (Settings.SerializedSettings,
                    Settings.GetNonPublicPropertiesResolverSettings());
        }

        private T updateNullable<T> (T? value, T @default, Func<T, T> update)
        where T : struct
        {
            return value.HasValue ? update(value.Value) : @default;
        }

        private T updateReference<T> (T value, T @default, Func<T, T> update)
        where T : class
        {
            return value != null ? update(value) : @default;
        }

        public SettingsState With(
            Guid? userId = null,
            DateTime? getChangesLastRun = null,
            bool? useTag = null,
            string lastAppVersion = null,
            int? lastReportZoom = null,
            bool? groupedEntries = null,
            bool? chooseProjectForNew = null,
            int? reportsCurrentItem = null,
            string projectSort = null,
            string installId = null,
            // iOS only  values
            string rossPreferredStartView = null,
            bool? rossReadDurOnlyNotice = null,
            DateTime? rossIgnoreSyncErrorsUntil = null,
            // Android only  values
            string gcmRegistrationId = null,
            string gcmAppVersion = null,
            bool? idleNotification = null,
            bool? showNotification = null,
            bool? showWelcome = null)
        {
            var copy = Init();
            updateNullable(userId, copy.UserId, x => copy.UserId = x);
            updateNullable(getChangesLastRun, copy.GetChangesLastRun, x => copy.GetChangesLastRun = x);
            updateNullable(useTag, copy.UseDefaultTag, x => copy.UseDefaultTag = x);
            updateReference(lastAppVersion, copy.LastAppVersion, x => copy.LastAppVersion = x);
            updateNullable(lastReportZoom, copy.LastReportZoom, x => copy.LastReportZoom = x);
            updateNullable(groupedEntries, copy.GroupedEntries, x => copy.GroupedEntries = x);
            updateNullable(chooseProjectForNew, copy.ChooseProjectForNew, x => copy.ChooseProjectForNew = x);
            updateNullable(reportsCurrentItem, copy.ReportsCurrentItem, x => copy.ReportsCurrentItem = x);
            updateReference(projectSort, copy.ProjectSort, x => copy.ProjectSort = x);
            updateReference(installId, copy.InstallId, x => copy.InstallId = x);
            // iOS only  values
            updateReference(rossPreferredStartView, copy.RossPreferredStartView, x => copy.RossPreferredStartView = x);
            updateNullable(rossReadDurOnlyNotice, copy.RossReadDurOnlyNotice, x => copy.RossReadDurOnlyNotice = x);
            updateNullable(rossIgnoreSyncErrorsUntil, copy.RossIgnoreSyncErrorsUntil, x => copy.RossIgnoreSyncErrorsUntil = x);
            // Android only  values
            updateReference(gcmRegistrationId, copy.GcmRegistrationId, x => copy.GcmRegistrationId = x);
            updateReference(gcmAppVersion, copy.GcmAppVersion, x => copy.GcmAppVersion = x);
            updateNullable(idleNotification, copy.IdleNotification, x => copy.IdleNotification = x);
            updateNullable(showNotification, copy.ShowNotification, x => copy.ShowNotification = x);
            updateNullable(showWelcome, copy.ShowWelcome, x => copy.ShowWelcome = x);

            // Save new copy serialized
            Settings.SerializedSettings = Newtonsoft.Json.JsonConvert.SerializeObject(copy);
            return copy;
        }
    }
}

