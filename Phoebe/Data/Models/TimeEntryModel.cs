using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Newtonsoft.Json;
using Toggl.Phoebe.Net;
using XPlatUtils;

namespace Toggl.Phoebe.Data.Models
{
    public class TimeEntryModel : Model
    {
        private static string GetPropertyName<T> (Expression<Func<TimeEntryModel, T>> expr)
        {
            return expr.ToPropertyName ();
        }

        internal static readonly string DefaultTag = "mobile";

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
                    State = TimeEntryState.Running,
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

        private bool LogicEnabled {
            get { return IsShared && !IsMerging; }
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
                value = value.ToUtc ().Truncate (TimeSpan.TicksPerSecond);

                lock (SyncRoot) {
                    if (startTime == value)
                        return;

                    var duration = GetDuration ();

                    ChangePropertyAndNotify (PropertyStartTime, delegate {
                        startTime = value;
                    });

                    if (LogicEnabled) {
                        if (State == TimeEntryState.Finished && StopTime.HasValue) {
                            SetDuration (duration);
                        }
                    }
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
                value = value.ToUtc ().Truncate (TimeSpan.TicksPerSecond);

                lock (SyncRoot) {
                    if (stopTime == value)
                        return;

                    ChangePropertyAndNotify (PropertyStopTime, delegate {
                        stopTime = value;
                    });
                }
            }
        }

        public TimeSpan GetDuration ()
        {
            return GetDuration (DateTime.UtcNow);
        }

        private TimeSpan GetDuration (DateTime now)
        {
            lock (SyncRoot) {
                var duration = (StopTime ?? now) - StartTime;
                if (duration < TimeSpan.Zero) {
                    duration = TimeSpan.Zero;
                }
                return duration;
            }
        }

        public void SetDuration (TimeSpan value)
        {
            lock (SyncRoot) {
                var now = DateTime.UtcNow;

                if (State == TimeEntryState.Finished) {
                    StopTime = StartTime + value;
                } else if (State == TimeEntryState.New && StopTime.HasValue) {
                    StartTime = StopTime.Value - value;
                } else {
                    StartTime = now - value;
                }
            }
        }

        [JsonProperty ("duration")]
        private long EncodedDuration {
            get {
                lock (SyncRoot) {
                    var now = DateTime.UtcNow;

                    var duration = (long)GetDuration (now).TotalSeconds;
                    if (State == TimeEntryState.Running) {
                        return (long)(duration - now.ToUnix ().TotalSeconds);
                    } else {
                        return duration;
                    }
                }
            }
            set {
                lock (SyncRoot) {
                    if (value < 0) {
                        State = TimeEntryState.Running;
                        SetDuration (DateTime.UtcNow.ToUnix () + TimeSpan.FromSeconds (value));
                    } else {
                        State = TimeEntryState.Finished;
                        SetDuration (TimeSpan.FromSeconds (value));
                    }
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

        private TimeEntryState state = TimeEntryState.New;
        public static readonly string PropertyState = GetPropertyName ((m) => m.State);

        public TimeEntryState State {
            get {
                lock (SyncRoot) {
                    return state;
                }
            }
            set {
                lock (SyncRoot) {
                    if (state == value)
                        return;

                    ChangePropertyAndNotify (PropertyState, delegate {
                        state = value;
                    });
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
                || property == PropertyState
                || property == PropertyIsPersisted) {
                if (IsShared && State == TimeEntryState.Running && IsPersisted) {
                    // Make sure that this is the only time entry running:
                    var entries = Model.Manager.Cached<TimeEntryModel> ().Where ((m) => m.UserId == UserId && m.State == TimeEntryState.Running);
                    foreach (var entry in entries) {
                        if (entry == this)
                            continue;
                        entry.Stop ();
                    }

                    // Double check the database as well:
                    entries = Model.Query<TimeEntryModel> (
                        (m) => m.UserId == UserId && m.State == TimeEntryState.Running && m.Id != Id)
                        .NotDeleted ();
                    foreach (var entry in entries) {
                        entry.Stop ();
                    }
                }
            }
        }

        public override void Delete ()
        {
            lock (SyncRoot) {
                if (IsShared && IsPersisted && State == TimeEntryState.Running)
                    Stop ();
                base.Delete ();
            }
        }

        public void Stop ()
        {
            lock (SyncRoot) {
                if (!IsShared || !IsPersisted)
                    throw new InvalidOperationException ("Model needs to be the shared and persisted.");

                StopTime = DateTime.UtcNow;
                State = TimeEntryState.Finished;
            }
        }

        public TimeEntryModel Continue ()
        {
            lock (SyncRoot) {
                if (!IsShared || !IsPersisted)
                    throw new InvalidOperationException ("Model needs to be the shared and persisted.");

                // Time entry is already running, nothing to continue
                if (State == TimeEntryState.Running)
                    return this;

                if (DurationOnly && StartTime.ToLocalTime ().Date == DateTime.Now.Date) {
                    if (RemoteId == null) {
                        StartTime = DateTime.UtcNow - GetDuration ();
                        StopTime = null;
                        State = TimeEntryState.Running;
                        return this;
                    }
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
                    State = TimeEntryState.Running,
                    IsPersisted = true,
                });
            }
        }

        #endregion

        public static TimeEntryModel FindRunning ()
        {
            lock (SyncRoot) {
                IEnumerable<TimeEntryModel> entries;
                entries = Model.Query<TimeEntryModel> ((te) => te.State == TimeEntryState.Running)
                    .NotDeleted ().ForCurrentUser ().ToList ();

                // Find currently running time entry:
                entries = Model.Manager.Cached<TimeEntryModel> ()
                    .Where ((te) => te.State == TimeEntryState.Running && te.DeletedAt == null && te.IsPersisted == true)
                    .ForCurrentUser ();
                return entries.FirstOrDefault ();
            }
        }

        public static TimeEntryModel GetDraft ()
        {
            lock (SyncRoot) {
                var model = Model.Manager.Cached<TimeEntryModel> ()
                    .FirstOrDefault ((m) => m.State == TimeEntryState.New);

                if (model == null) {
                    // Create new draft:
                    model = Model.Update (new TimeEntryModel () {
                    });
                }

                return model;
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
