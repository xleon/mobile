using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Linq;
using Newtonsoft.Json;

namespace Toggl.Phoebe.Data
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
        // TODO: Move UserAgent to some better place
        private static readonly string UserAgent = "Toggl Mobile";
        private static readonly DateTime UnixStart = new DateTime (1970, 1, 1);

        public static void UpdateDurations ()
        {
            // TODO: Call this method periodically from some place
            var allEntries = Model.GetCached<TimeEntryModel> ();
            foreach (var entry in allEntries) {
                entry.UpdateDuration ();
            }
        }

        private readonly int workspaceRelationId;
        private readonly int projectRelationId;
        private readonly int taskRelationId;
        private readonly int userRelationId;

        public TimeEntryModel ()
        {
            workspaceRelationId = ForeignRelation<WorkspaceModel> (PropertyWorkspaceId, PropertyWorkspace);
            projectRelationId = ForeignRelation<ProjectModel> (PropertyProjectId, PropertyProject);
            taskRelationId = ForeignRelation<TaskModel> (PropertyTaskId, PropertyTask);
            userRelationId = ForeignRelation<UserModel> (PropertyUserId, PropertyUser);
        }

        private string GetPropertyName<T> (Expression<Func<T>> expr)
        {
            return expr.ToPropertyName (this);
        }

        #region Data

        private string description;
        public static readonly string PropertyDescription = GetPropertyName ((m) => m.Description);

        [JsonProperty ("description")]
        public string Description {
            get { return description; }
            set {
                if (description == value)
                    return;

                ChangePropertyAndNotify (PropertyDescription, delegate {
                    description = value;
                });
            }
        }

        private bool billable;
        public static readonly string PropertyIsBillable = GetPropertyName ((m) => m.IsBillable);

        [JsonProperty ("billable")]
        public bool IsBillable {
            get { return billable; }
            set {
                if (billable == value)
                    return;

                ChangePropertyAndNotify (PropertyIsBillable, delegate {
                    billable = value;
                });
            }
        }

        private DateTime startTime;
        public static readonly string PropertyStartTime = GetPropertyName ((m) => m.StartTime);

        [JsonProperty ("start")]
        public DateTime StartTime {
            get { return startTime; }
            set {
                if (startTime == value)
                    return;

                ChangePropertyAndNotify (PropertyStartTime, delegate {
                    startTime = value;
                });
            }
        }

        private DateTime? stopTime;
        public static readonly string PropertyStopTime = GetPropertyName ((m) => m.StopTime);

        [JsonProperty ("stop", NullValueHandling = NullValueHandling.Include)]
        public DateTime? StopTime {
            get { return stopTime; }
            set {
                if (stopTime == value)
                    return;

                ChangePropertyAndNotify (PropertyStopTime, delegate {
                    stopTime = value;
                });

                IsRunning = stopTime != null;
            }
        }

        private long duration;
        public static readonly string PropertyDuration = GetPropertyName ((m) => m.Duration);

        [SQLite.Ignore]
        public long Duration {
            get { return duration; }
            set {
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

        private void UpdateDuration ()
        {
            if (RawDuration < 0) {
                Duration = (long)((DateTime.UtcNow - UnixStart).TotalSeconds - RawDuration);
            } else {
                Duration = RawDuration;
            }
        }

        private long rawDuration;
        public static readonly string PropertyRawDuration = GetPropertyName ((m) => m.RawDuration);

        [JsonProperty ("duration")]
        public long RawDuration {
            get { return rawDuration; }
            set {
                if (rawDuration == value)
                    return;

                ChangePropertyAndNotify (PropertyRawDuration, delegate {
                    rawDuration = value;
                });

                IsRunning = value < 0;
                UpdateDuration ();
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
            get { return createdWith; }
            set {
                if (createdWith == value)
                    return;

                ChangePropertyAndNotify (PropertyCreatedWith, delegate {
                    createdWith = value;
                });
            }
        }

        public ISet<string> Tags {
            get {
                // TODO: Implement Tags logic
                return null;
            }
        }

        private bool durationOnly;
        public static readonly string PropertyDurationOnly = GetPropertyName ((m) => m.DurationOnly);

        [JsonProperty ("duronly")]
        public bool DurationOnly {
            get { return durationOnly; }
            set {
                if (durationOnly == value)
                    return;

                ChangePropertyAndNotify (PropertyDurationOnly, delegate {
                    durationOnly = value;
                });
            }
        }

        private bool running;
        public static readonly string PropertyIsRunning = GetPropertyName ((m) => m.IsRunning);

        public bool IsRunning {
            get { return running; }
            set {
                if (running == value)
                    return;

                ChangePropertyAndNotify (PropertyIsRunning, delegate {
                    running = value;
                });

                if (IsRunning && RawDuration >= 0) {
                    RawDuration = (long)(RawDuration - (DateTime.UtcNow - UnixStart).TotalSeconds);
                } else if (!IsRunning && RawDuration < 0) {
                    RawDuration = (long)((DateTime.UtcNow - UnixStart).TotalSeconds - RawDuration);
                }
            }
        }

        #endregion

        #region Relations

        public static readonly string PropertyWorkspaceId = GetPropertyName ((m) => m.WorkspaceId);

        [JsonProperty ("wid")]
        public Guid? WorkspaceId {
            get { return GetForeignId (workspaceRelationId); }
            set { SetForeignId (workspaceRelationId, value); }
        }

        public static readonly string PropertyWorkspace = GetPropertyName ((m) => m.Workspace);

        [DontDirty]
        [SQLite.Ignore]
        public WorkspaceModel Workspace {
            get { return GetForeignModel<WorkspaceModel> (workspaceRelationId); }
            set { SetForeignModel (workspaceRelationId, value); }
        }

        public static readonly string PropertyProjectId = GetPropertyName ((m) => m.ProjectId);

        [JsonProperty ("pid")]
        public Guid? ProjectId {
            get { return GetForeignId (projectRelationId); }
            set { SetForeignId (projectRelationId, value); }
        }

        public static readonly string PropertyProject = GetPropertyName ((m) => m.Project);

        [DontDirty]
        [SQLite.Ignore]
        public ProjectModel Project {
            get { return GetForeignModel<ProjectModel> (projectRelationId); }
            set { SetForeignModel (projectRelationId, value); }
        }

        public static readonly string PropertyTaskId = GetPropertyName ((m) => m.TaskId);

        [JsonProperty ("tid")]
        public Guid? TaskId {
            get { return GetForeignId (taskRelationId); }
            set { SetForeignId (taskRelationId, value); }
        }

        public static readonly string PropertyTask = GetPropertyName ((m) => m.Task);

        [DontDirty]
        [SQLite.Ignore]
        public TaskModel Task {
            get { return GetForeignModel<TaskModel> (taskRelationId); }
            set { SetForeignModel (taskRelationId, value); }
        }

        public static readonly string PropertyUserId = GetPropertyName ((m) => m.UserId);

        [JsonProperty ("uid")]
        public Guid? UserId {
            get { return GetForeignId (userRelationId); }
            set { SetForeignId (userRelationId, value); }
        }

        public static readonly string PropertyUser = GetPropertyName ((m) => m.User);

        [DontDirty]
        [SQLite.Ignore]
        public UserModel User {
            get { return GetForeignModel<UserModel> (userRelationId); }
            set { SetForeignModel (userRelationId, value); }
        }

        #endregion

        #region Business logic

        protected override void OnPropertyChanged (string property)
        {
            base.OnPropertyChanged (property);

            if (property == PropertyIsShared
                || property == PropertyIsRunning
                || property == PropertyIsPersisted) {
                if (IsShared && IsRunning && IsPersisted) {
                    // Make sure that this is the only time entry running:
                    var entries = Model.GetCached<TimeEntryModel> ().Where ((m) => m.UserId == UserId && m.IsRunning);
                    foreach (var entry in entries) {
                        if (entry == this)
                            continue;
                        entry.IsRunning = false;
                    }

                    // Double check the database as well:
                    entries = Model.Query<TimeEntryModel> ((m) => m.UserId == UserId && m.IsRunning);
                    foreach (var entry in entries) {
                        if (entry == this)
                            continue;
                        entry.IsRunning = false;
                    }
                }
            }
        }

        public void Stop ()
        {
            if (!IsShared || !IsPersisted)
                throw new InvalidOperationException ("Model needs to be the shared and persisted.");

            if (DurationOnly) {
                IsRunning = false;
            } else {
                StopTime = DateTime.UtcNow;
            }
        }

        public TimeEntryModel Continue ()
        {
            if (!IsShared || !IsPersisted)
                throw new InvalidOperationException ("Model needs to be the shared and persisted.");

            if (DurationOnly && StartTime.ToLocalTime ().Date == DateTime.Now.Date) {
                IsRunning = true;
                return this;
            }

            return Model.Update (new TimeEntryModel () {
                WorkspaceId = WorkspaceId,
                ProjectId = ProjectId,
                TaskId = TaskId,
                Description = Description,
                StartTime = DateTime.UtcNow,
                DurationOnly = DurationOnly,
//                Tags = Tags,
                IsBillable = IsBillable,
                IsRunning = true,
            });
        }

        #endregion
    }
}
