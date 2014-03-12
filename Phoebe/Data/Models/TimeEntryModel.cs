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

        private static readonly string LogTag = "TimeEntryModel";
        internal static readonly string DefaultTag = "mobile";
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

                    SetStartTime (value);

                    if (LogicEnabled) {
                        if (State != TimeEntryState.Running) {
                            StopTime = startTime + duration;
                        }
                    }
                }
            }
        }

        private void SetStartTime (DateTime value)
        {
            value = value.Truncate (TimeSpan.TicksPerSecond);
            if (startTime == value)
                return;

            ChangePropertyAndNotify (PropertyStartTime, delegate {
                startTime = value;
            });
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
                if (StartTime == DateTime.MinValue) {
                    return TimeSpan.Zero;
                }

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
                } else if (State == TimeEntryState.New) {
                    if (value == TimeSpan.Zero) {
                        SetStartTime (DateTime.MinValue);
                        StopTime = null;
                    } else if (StopTime.HasValue) {
                        SetStartTime (StopTime.Value - value);
                    } else {
                        SetStartTime (now - value);
                        StopTime = now;
                    }
                } else {
                    SetStartTime (now - value);
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
                    if (IsShared && value != null && Workspace != null) {
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
                || property == PropertyIsPersisted
                || property == PropertyWorkspaceId) {
                if (IsShared && IsPersisted && stringTagsList != null && Workspace != null) {
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
                        try {
                            entry.Stop ();
                        } catch (InvalidOperationException ex) {
                            var log = ServiceContainer.Resolve<Logger> ();
                            log.Debug (LogTag, ex, "Failed to stop time entry in memory.");
                        }
                    }

                    // Double check the database as well:
                    entries = Model.Query<TimeEntryModel> (
                        (m) => m.UserId == UserId && m.State == TimeEntryState.Running && m.Id != Id)
                        .NotDeleted ();
                    foreach (var entry in entries) {
                        try {
                            entry.Stop ();
                        } catch (InvalidOperationException ex) {
                            var log = ServiceContainer.Resolve<Logger> ();
                            log.Debug (LogTag, ex, "Failed to stop time entry from store.");
                        }
                    }
                }
            }
        }

        public override void Delete ()
        {
            lock (SyncRoot) {
                if (IsShared && IsPersisted && State == TimeEntryState.Running) {
                    try {
                        Stop ();
                    } catch (InvalidOperationException ex) {
                        var log = ServiceContainer.Resolve<Logger> ();
                        log.Debug (LogTag, ex, "Failed to stop time entry before deleting it.");
                    }
                }
                base.Delete ();
            }
        }

        /// <summary>
        /// Stores the draft time entry in model store as a running time entry.
        /// </summary>
        public void Start ()
        {
            lock (SyncRoot) {
                if (!IsShared || !IsPersisted)
                    throw new InvalidOperationException ("Model needs to be the shared and persisted.");
                if (State != TimeEntryState.New)
                    throw new InvalidOperationException (String.Format ("Cannot start a time entry in {0} state.", State));
                if (StartTime != DateTime.MinValue || StopTime.HasValue)
                    throw new InvalidOperationException ("Cannot start tracking time entry with start/stop time set already.");

                if (Task != null) {
                    Project = Task.Project;
                }
                if (Project != null) {
                    Workspace = Project.Workspace;
                }
                if (Workspace == null && User != null) {
                    Workspace = User.DefaultWorkspace;
                }
                if (Workspace == null) {
                    throw new InvalidOperationException ("Workspace (or user default workspace) must be set.");
                }

                Tags.Add (DefaultTag);
                State = TimeEntryState.Running;
                StartTime = DateTime.UtcNow;
                StopTime = null;
            }
        }

        /// <summary>
        /// Stores the draft time entry in model store as a finished time entry.
        /// </summary>
        public void Store ()
        {
            lock (SyncRoot) {
                if (!IsShared || !IsPersisted)
                    throw new InvalidOperationException ("Model needs to be the shared and persisted.");
                if (State != TimeEntryState.New)
                    throw new InvalidOperationException (String.Format ("Cannot store a time entry in {0} state.", State));
                if (StartTime == DateTime.MinValue || StopTime == null)
                    throw new InvalidOperationException ("Cannot store time entry with start/stop time not set.");

                if (Task != null) {
                    Project = Task.Project;
                }
                if (Project != null) {
                    Workspace = Project.Workspace;
                }
                if (Workspace == null && User != null) {
                    Workspace = User.DefaultWorkspace;
                }
                if (Workspace == null) {
                    throw new InvalidOperationException ("Workspace (or user default workspace) must be set.");
                }

                Tags.Add (DefaultTag);
                State = TimeEntryState.Finished;
            }
        }

        /// <summary>
        /// Marks the currently running time entry as finished.
        /// </summary>
        public void Stop ()
        {
            lock (SyncRoot) {
                if (!IsShared || !IsPersisted)
                    throw new InvalidOperationException ("Model needs to be the shared and persisted.");

                if (State != TimeEntryState.Running)
                    throw new InvalidOperationException (String.Format ("Cannot stop a time entry in {0} state.", State));

                StopTime = DateTime.UtcNow;
                State = TimeEntryState.Finished;
            }
        }

        /// <summary>
        /// Continues the finished time entry, either by creating a new time entry or restarting the current one.
        /// </summary>
        public TimeEntryModel Continue ()
        {
            lock (SyncRoot) {
                if (!IsShared)
                    throw new InvalidOperationException ("Model needs to be the shared.");

                // Validate the current state
                switch (State) {
                case TimeEntryState.Running:
                    IsPersisted = true;
                    return this;
                case TimeEntryState.Finished:
                    break;
                default:
                    throw new InvalidOperationException (String.Format ("Cannot continue a time entry in {0} state.", State));
                }

                if (DurationOnly && StartTime.ToLocalTime ().Date == DateTime.Now.Date) {
                    if (RemoteId == null) {
                        IsPersisted = true;
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
                var user = ServiceContainer.Resolve<AuthManager> ().User;
                if (user == null)
                    return null;

                var model = Model.Manager.Cached<TimeEntryModel> ()
                    .FirstOrDefault ((m) => m.State == TimeEntryState.New && m.DeletedAt == null && m.User == user);

                if (model == null) {
                    model = Model.Query<TimeEntryModel> ((m) => m.State == TimeEntryState.New && m.DeletedAt == null && m.UserId == user.Id)
                        .ToList ()
                        .FirstOrDefault ((m) => m.State == TimeEntryState.New && m.DeletedAt == null && m.User == user);
                }

                if (model == null) {
                    // Create new draft:
                    model = Model.Update (new TimeEntryModel () {
                        State = TimeEntryState.New,
                        User = user,
                        Workspace = user.DefaultWorkspace,
                        DurationOnly = user.TrackingMode == TrackingMode.Continue,
                        StringTags = new List<string> () { DefaultTag },
                        IsPersisted = true,
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
                        // Since we're auto creating this, we want to be sure that there wouldn't be any conflicts
                        // with server-side data. Having modified at as something in the very past allows server data
                        // to take precedence.
                        tagModel.ModifiedAt = DateTime.MinValue;
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
