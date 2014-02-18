using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Newtonsoft.Json;
using Toggl.Phoebe.Net;
using XPlatUtils;

namespace Toggl.Phoebe.Data.Models
{
    /* TODO: Things to test:
     * - Stopping another time entry works from any angle
     * - Restoring from database/json gets correct state (ie order of setting the properties shouldn't affect
     *   the outcome)
     * - Duration updating
     */
    public class TimeEntryModel : Model
    {
        private static string GetPropertyName<T> (Expression<Func<TimeEntryModel, T>> expr)
        {
            return expr.ToPropertyName ();
        }

        internal static readonly string DefaultTag = "mobile";
        private static readonly DateTime UnixStart = new DateTime (1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
        private static bool UpdateScheduled = false;

        private static async void EnsureLiveDurations ()
        {
            lock (SyncRoot) {
                if (UpdateScheduled)
                    return;

                UpdateScheduled = true;
            }

            try {
                bool done = false;

                while (!done) {
                    done = true;

                    lock (SyncRoot) {
                        var allEntries = Model.Manager.Cached<TimeEntryModel> ().Where ((te) => te.IsRunning);
                        foreach (var entry in allEntries) {
                            entry.UpdateDuration ();
                            done = false;
                        }
                    }

                    if (!done)
                        await System.Threading.Tasks.Task.Delay (TimeSpan.FromMilliseconds (500))
                            .ConfigureAwait (continueOnCapturedContext: false);
                }
            } finally {
                lock (SyncRoot) {
                    UpdateScheduled = false;
                }
            }
        }

        public static TimeEntryModel StartNew ()
        {
            return StartNew (null, null, null);
        }

        public static TimeEntryModel StartNew (WorkspaceModel workspace)
        {
            return StartNew (workspace, null, null);
        }

        public static TimeEntryModel StartNew (ProjectModel project)
        {
            return StartNew (project.Workspace, project, null);
        }

        public static TimeEntryModel StartNew (TaskModel task)
        {
            return StartNew (task.Project.Workspace, task.Project, task);
        }

        private static TimeEntryModel StartNew (WorkspaceModel workspace, ProjectModel project, TaskModel task)
        {
            lock (SyncRoot) {
                var user = ServiceContainer.Resolve<AuthManager> ().User;

                workspace = workspace ?? user.DefaultWorkspace;
                if (workspace == null) {
                    throw new ArgumentNullException ("workspace", "A time entry must be started in a workspace.");
                }

                return Model.Update (new TimeEntryModel () {
                    Workspace = workspace,
                    Project = project,
                    Task = task,
                    User = user,
                    StartTime = DateTime.UtcNow,
                    DurationOnly = user.TrackingMode == TrackingMode.Continue,
                    StringTags = new List<string> () { DefaultTag },
                    IsRunning = true,
                    IsPersisted = true,
                });
            }
        }

        private readonly int workspaceRelationId;
        private readonly int projectRelationId;
        private readonly int taskRelationId;
        private readonly int userRelationId;
        private readonly TagsCollection tagsCollection;

        public TimeEntryModel ()
        {
            workspaceRelationId = ForeignRelation<WorkspaceModel> (PropertyWorkspaceId, PropertyWorkspace);
            projectRelationId = ForeignRelation<ProjectModel> (PropertyProjectId, PropertyProject);
            taskRelationId = ForeignRelation<TaskModel> (PropertyTaskId, PropertyTask);
            userRelationId = ForeignRelation<UserModel> (PropertyUserId, PropertyUser);
            tagsCollection = new TagsCollection (this);
        }

        private string GetPropertyName<T> (Expression<Func<T>> expr)
        {
            return expr.ToPropertyName (this);
        }

        protected override void Validate (ValidationContext ctx)
        {
            base.Validate (ctx);

            if (ctx.HasChanged (PropertyWorkspaceId)
                || ctx.HasChanged (PropertyIsBillable)) {

                ctx.ClearErrors (PropertyWorkspaceId);
                ctx.ClearErrors (PropertyWorkspace);

                if (WorkspaceId == null) {
                    ctx.AddError (PropertyWorkspaceId, "Time entry must be associated with a workspace.");
                } else if (Workspace == null) {
                    ctx.AddError (PropertyWorkspace, "Associated workspace could not be found.");
                }

                // Check premium feature usage
                if (IsBillable && Workspace != null && !Workspace.IsPremium) {
                    ctx.AddError (PropertyIsBillable, "Billable time entries can only exist in premium workspaces.");
                } else {
                    ctx.ClearErrors (PropertyIsBillable);
                }
            }

            if (ctx.HasChanged (PropertyUserId)) {
                ctx.ClearErrors (PropertyUserId);
                ctx.ClearErrors (PropertyUser);

                if (WorkspaceId == null) {
                    ctx.AddError (PropertyUserId, "Time entry must be associated with a user.");
                } else if (Workspace == null) {
                    ctx.AddError (PropertyUser, "Associated user could not be found.");
                }
            }
        }

        #region Data

        private string description;
        public static readonly string PropertyDescription = GetPropertyName ((m) => m.Description);

        [JsonProperty ("description")]
        public string Description {
            get {
                lock (SyncRoot) {
                    return description;
                }
            }
            set {
                lock (SyncRoot) {
                    if (description == value)
                        return;

                    ChangePropertyAndNotify (PropertyDescription, delegate {
                        description = value;
                    });
                }
            }
        }

        private bool billable;
        public static readonly string PropertyIsBillable = GetPropertyName ((m) => m.IsBillable);

        [JsonProperty ("billable")]
        public bool IsBillable {
            get {
                lock (SyncRoot) {
                    return billable;
                }
            }
            set {
                lock (SyncRoot) {
                    if (billable == value)
                        return;

                    ChangePropertyAndNotify (PropertyIsBillable, delegate {
                        billable = value;
                    });
                }
            }
        }

        private DateTime startTime;
        public static readonly string PropertyStartTime = GetPropertyName ((m) => m.StartTime);

        [JsonProperty ("start")]
        public DateTime StartTime {
            get {
                lock (SyncRoot) {
                    return startTime;
                }
            }
            set {
                value = value.ToUtc ();

                lock (SyncRoot) {
                    if (startTime == value)
                        return;

                    ChangePropertyAndNotify (PropertyStartTime, delegate {
                        startTime = value;
                    });
                }
            }
        }

        private DateTime? stopTime;
        public static readonly string PropertyStopTime = GetPropertyName ((m) => m.StopTime);

        [JsonProperty ("stop", NullValueHandling = NullValueHandling.Include)]
        public DateTime? StopTime {
            get {
                lock (SyncRoot) {
                    return stopTime;
                }
            }
            set {
                value = value.ToUtc ();
                lock (SyncRoot) {
                    if (stopTime == value)
                        return;

                    ChangePropertyAndNotify (PropertyStopTime, delegate {
                        stopTime = value;
                    });

                    IsRunning = stopTime == null;
                }
            }
        }

        private long duration;
        public static readonly string PropertyDuration = GetPropertyName ((m) => m.Duration);

        [DontDirty]
        [SQLite.Ignore]
        public long Duration {
            get {
                lock (SyncRoot) {
                    return duration;
                }
            }
            set {
                lock (SyncRoot) {
                    if (duration == value)
                        return;

                    ChangePropertyAndNotify (PropertyDuration, delegate {
                        duration = value;
                    });

                    if (RawDuration < 0) {
                        RawDuration = (long)(value - (DateTime.UtcNow - UnixStart).TotalSeconds);
                    } else {
                        RawDuration = value;
                    }
                }
            }
        }

        private void UpdateDuration ()
        {
            if (RawDuration < 0) {
                Duration = (long)((DateTime.UtcNow - UnixStart).TotalSeconds + RawDuration);
            } else {
                Duration = RawDuration;
            }
        }

        private long rawDuration;
        public static readonly string PropertyRawDuration = GetPropertyName ((m) => m.RawDuration);

        [JsonProperty ("duration")]
        public long RawDuration {
            get {
                lock (SyncRoot) {
                    return rawDuration;
                }
            }
            set {
                lock (SyncRoot) {
                    if (rawDuration == value)
                        return;

                    ChangePropertyAndNotify (PropertyRawDuration, delegate {
                        rawDuration = value;
                    });

                    IsRunning = value < 0;
                    UpdateDuration ();
                }
            }
        }

        private string createdWith;
        public static readonly string PropertyCreatedWith = GetPropertyName ((m) => m.CreatedWith);

        [JsonProperty ("created_with")]
        [SQLite.Ignore]
        /// <summary>
        /// Gets or sets the created with. Created with should be automatically set by <see cref="ITogglClient"/>
        /// implementation before sending data to server.
        /// </summary>
        /// <value>The created with string.</value>
        public string CreatedWith {
            get {
                lock (SyncRoot) {
                    return createdWith;
                }
            }
            set {
                lock (SyncRoot) {
                    if (createdWith == value)
                        return;

                    ChangePropertyAndNotify (PropertyCreatedWith, delegate {
                        createdWith = value;
                    });
                }
            }
        }

        private bool durationOnly;
        public static readonly string PropertyDurationOnly = GetPropertyName ((m) => m.DurationOnly);

        [JsonProperty ("duronly")]
        public bool DurationOnly {
            get {
                lock (SyncRoot) {
                    return durationOnly;
                }
            }
            set {
                lock (SyncRoot) {
                    if (durationOnly == value)
                        return;

                    ChangePropertyAndNotify (PropertyDurationOnly, delegate {
                        durationOnly = value;
                    });
                }
            }
        }

        private bool running;
        public static readonly string PropertyIsRunning = GetPropertyName ((m) => m.IsRunning);

        [DontDirty]
        public bool IsRunning {
            get {
                lock (SyncRoot) {
                    return running;
                }
            }
            set {
                lock (SyncRoot) {
                    if (running == value)
                        return;

                    ChangePropertyAndNotify (PropertyIsRunning, delegate {
                        running = value;
                    });

                    if (IsRunning)
                        EnsureLiveDurations ();

                    if (IsRunning && RawDuration >= 0) {
                        RawDuration = (long)(RawDuration - (DateTime.UtcNow - UnixStart).TotalSeconds);
                    } else if (!IsRunning && RawDuration < 0) {
                        RawDuration = (long)((DateTime.UtcNow - UnixStart).TotalSeconds + RawDuration);
                    }

                    if (IsRunning) {
                        StopTime = null;
                    } else {
                        StopTime = StartTime + TimeSpan.FromSeconds (RawDuration);
                    }
                }
            }
        }

        private List<string> stringTagsList;

        [JsonProperty ("tags")]
        private List<string> StringTags {
            get {
                lock (SyncRoot) {
                    if (stringTagsList != null)
                        return stringTagsList;
                    if (!IsShared)
                        return null;
                    return Tags.Select ((m) => m.To.Name).ToList ();
                }
            }
            set {
                lock (SyncRoot) {
                    if (IsShared && value != null) {
                        stringTagsList = null;
                        foreach (var inter in Tags.ToList()) {
                            if (!value.Remove (inter.To.Name)) {
                                inter.Delete ();
                            }
                        }
                        foreach (var tag in value) {
                            Tags.Add (tag);
                        }
                    } else if (value != null) {
                        stringTagsList = value.Where ((tag) => !String.IsNullOrWhiteSpace (tag)).ToList ();
                        if (stringTagsList.Count == 0)
                            stringTagsList = null;
                    } else {
                        stringTagsList = null;
                    }
                }
            }
        }

        #endregion

        #region Relations

        public static readonly string PropertyWorkspaceId = GetPropertyName ((m) => m.WorkspaceId);

        public Guid? WorkspaceId {
            get { return GetForeignId (workspaceRelationId); }
            set { SetForeignId (workspaceRelationId, value); }
        }

        public static readonly string PropertyWorkspace = GetPropertyName ((m) => m.Workspace);

        [DontDirty]
        [SQLite.Ignore]
        [JsonProperty ("wid"), JsonConverter (typeof(ForeignKeyJsonConverter))]
        public WorkspaceModel Workspace {
            get { return GetForeignModel<WorkspaceModel> (workspaceRelationId); }
            set { SetForeignModel (workspaceRelationId, value); }
        }

        public static readonly string PropertyProjectId = GetPropertyName ((m) => m.ProjectId);

        public Guid? ProjectId {
            get { return GetForeignId (projectRelationId); }
            set { SetForeignId (projectRelationId, value); }
        }

        public static readonly string PropertyProject = GetPropertyName ((m) => m.Project);

        [DontDirty]
        [SQLite.Ignore]
        [JsonProperty ("pid"), JsonConverter (typeof(ForeignKeyJsonConverter))]
        public ProjectModel Project {
            get { return GetForeignModel<ProjectModel> (projectRelationId); }
            set { SetForeignModel (projectRelationId, value); }
        }

        public static readonly string PropertyTaskId = GetPropertyName ((m) => m.TaskId);

        public Guid? TaskId {
            get { return GetForeignId (taskRelationId); }
            set { SetForeignId (taskRelationId, value); }
        }

        public static readonly string PropertyTask = GetPropertyName ((m) => m.Task);

        [DontDirty]
        [SQLite.Ignore]
        [JsonProperty ("tid"), JsonConverter (typeof(ForeignKeyJsonConverter))]
        public TaskModel Task {
            get { return GetForeignModel<TaskModel> (taskRelationId); }
            set { SetForeignModel (taskRelationId, value); }
        }

        public static readonly string PropertyUserId = GetPropertyName ((m) => m.UserId);

        public Guid? UserId {
            get { return GetForeignId (userRelationId); }
            set { SetForeignId (userRelationId, value); }
        }

        public static readonly string PropertyUser = GetPropertyName ((m) => m.User);

        [DontDirty]
        [SQLite.Ignore]
        [JsonProperty ("uid"), JsonConverter (typeof(ForeignKeyJsonConverter))]
        public UserModel User {
            get { return GetForeignModel<UserModel> (userRelationId); }
            set { SetForeignModel (userRelationId, value); }
        }

        [SQLite.Ignore]
        public TagsCollection Tags {
            get { return tagsCollection; }
        }

        #endregion

        #region Business logic

        protected override void OnPropertyChanged (string property)
        {
            base.OnPropertyChanged (property);

            // Make sure the string tags are converted into actual relations as soon as possible:
            if (property == PropertyIsShared
                || property == PropertyIsPersisted) {
                if (IsShared && IsPersisted && stringTagsList != null) {
                    StringTags = stringTagsList;
                }
            }

            if (property == PropertyIsShared
                || property == PropertyIsRunning
                || property == PropertyIsPersisted) {
                if (IsShared && IsRunning && IsPersisted) {
                    // Make sure that this is the only time entry running:
                    var entries = Model.Manager.Cached<TimeEntryModel> ().Where ((m) => m.UserId == UserId && m.IsRunning);
                    foreach (var entry in entries) {
                        if (entry == this)
                            continue;
                        entry.IsRunning = false;
                    }

                    // Double check the database as well:
                    entries = Model.Query<TimeEntryModel> (
                        (m) => m.UserId == UserId && m.IsRunning && m.Id != Id)
                        .NotDeleted ();
                    foreach (var entry in entries) {
                        entry.IsRunning = false;
                    }

                    EnsureLiveDurations ();
                }
            }
        }

        public override void Delete ()
        {
            lock (SyncRoot) {
                if (IsShared && IsPersisted && IsRunning)
                    Stop ();
                base.Delete ();
                Tags.Clear ();
            }
        }

        public void Stop ()
        {
            lock (SyncRoot) {
                if (!IsShared || !IsPersisted)
                    throw new InvalidOperationException ("Model needs to be the shared and persisted.");

                if (DurationOnly) {
                    IsRunning = false;
                } else {
                    StopTime = DateTime.UtcNow;
                }
            }
        }

        public TimeEntryModel Continue ()
        {
            lock (SyncRoot) {
                if (!IsShared || !IsPersisted)
                    throw new InvalidOperationException ("Model needs to be the shared and persisted.");

                // Time entry is already running, nothing to continue
                if (IsRunning)
                    return this;

                if (DurationOnly && StartTime.ToLocalTime ().Date == DateTime.Now.Date) {
                    IsRunning = true;
                    return this;
                }

                return Model.Update (new TimeEntryModel () {
                    WorkspaceId = WorkspaceId,
                    ProjectId = ProjectId,
                    TaskId = TaskId,
                    UserId = UserId,
                    Description = Description,
                    StartTime = DateTime.UtcNow,
                    DurationOnly = DurationOnly,
                    StringTags = StringTags,
                    IsBillable = IsBillable,
                    IsRunning = true,
                    IsPersisted = true,
                });
            }
        }

        #endregion

        public static TimeEntryModel FindRunning ()
        {
            lock (SyncRoot) {
                IEnumerable<TimeEntryModel> entries;
                entries = Model.Query<TimeEntryModel> ((te) => te.IsRunning)
                .NotDeleted ().ForCurrentUser ().ToList ();

                // Find currently running time entry:
                entries = Model.Manager.Cached<TimeEntryModel> ()
                    .Where ((te) => te.IsRunning && te.DeletedAt == null)
                    .ForCurrentUser ();
                return entries.FirstOrDefault ();
            }
        }

        public class TagsCollection : RelatedModelsCollection<TagModel, TimeEntryTagModel, TimeEntryModel, TagModel>
        {
            private readonly TimeEntryModel model;

            public TagsCollection (TimeEntryModel model) : base (model)
            {
                this.model = model;
            }

            private TagModel GetTagModel (string tag)
            {
                var tagModel = Model.Manager.Cached<TagModel> ()
                    .Where ((m) => m.WorkspaceId == model.WorkspaceId && m.Name == tag)
                    .FirstOrDefault ();
                if (tagModel == null) {
                    tagModel = Model.Query<TagModel> ((m) => m.WorkspaceId == model.WorkspaceId && m.Name == tag)
                        .FirstOrDefault ();
                }
                return tagModel;
            }

            public TimeEntryTagModel Add (string tag)
            {
                lock (SyncRoot) {
                    if (!model.WorkspaceId.HasValue)
                        throw new InvalidOperationException ("Cannot add a tag to a model with no workspace association");

                    var tagModel = GetTagModel (tag);
                    if (tagModel == null) {
                        tagModel = Model.Update (new TagModel () {
                            Name = tag,
                            WorkspaceId = model.WorkspaceId,
                            IsPersisted = true,
                        });
                    }

                    return Add (tagModel);
                }
            }

            public void Remove (string tag)
            {
                lock (SyncRoot) {
                    if (!model.WorkspaceId.HasValue)
                        throw new InvalidOperationException ("Cannot remove a tag to a model with no workspace association");

                    var tagModel = GetTagModel (tag);
                    if (tagModel != null)
                        Remove (tagModel);
                }
            }

            public bool HasNonDefault {
                get {
                    lock (SyncRoot) {
                        return this.Where ((m) => m.To.Name != TimeEntryModel.DefaultTag).Any ();
                    }
                }
            }
        }
    }
}
