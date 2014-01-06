using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace TogglDoodle.Models
{
    /* TODO: Things to test:
     * - Stopping another time entry works from any angle
     * - Restoring from database/json gets correct state (ie order of setting the properties shouldn't affect
     *   the outcome)
     * - Duration updating
     */
    public class TimeEntryModel : Model
    {
        // TODO: Move UserAgent to some better place
        private static readonly string UserAgent = "Toggl Mobile";
        private static readonly DateTime UnixStart = new DateTime (1970, 1, 1);

        public static long NextId {
            get { return Model.NextId<TimeEntryModel> (); }
        }

        public static void UpdateDurations ()
        {
            // TODO: Call this method periodically from some place
            var allEntries = Model.GetAllShared<TimeEntryModel> ();
            foreach (var entry in allEntries) {
                entry.UpdateDuration ();
            }
        }

        private readonly int workspaceRelationId;
        private readonly int projectRelationId;
        private readonly int taskRelationId;
        private readonly int userRelationId;
        private readonly string propertyIsShared;
        private readonly string propertyIsRunning;
        private readonly string propertyIsPersisted;

        public TimeEntryModel ()
        {
            workspaceRelationId = ForeignRelation (() => WorkspaceId, () => Workspace);
            projectRelationId = ForeignRelation (() => ProjectId, () => Project);
            taskRelationId = ForeignRelation (() => TaskId, () => Task);
            userRelationId = ForeignRelation (() => UserId, () => User);
            propertyIsShared = GetPropertyName (() => IsShared);
            propertyIsRunning = GetPropertyName (() => IsRunning);
            propertyIsPersisted = GetPropertyName (() => IsPersisted);
        }

        private string GetPropertyName<T> (Expression<Func<T>> expr)
        {
            return expr.ToPropertyName (this);
        }

        #region Data

        private string description;

        public string Description {
            get { return description; }
            set {
                if (description == value)
                    return;

                ChangePropertyAndNotify (() => Description, delegate {
                    description = value;
                });

                MarkDirty ();
            }
        }

        private bool billable;

        public bool IsBillable {
            get { return billable; }
            set {
                if (billable == value)
                    return;

                ChangePropertyAndNotify (() => IsBillable, delegate {
                    billable = value;
                });

                MarkDirty ();
            }
        }

        private DateTime startTime;

        public DateTime StartTime {
            get { return startTime; }
            set {
                if (startTime == value)
                    return;

                ChangePropertyAndNotify (() => StartTime, delegate {
                    startTime = value;
                });

                MarkDirty ();
            }
        }

        private DateTime? stopTime;

        public DateTime? StopTime {
            get { return stopTime; }
            set {
                if (stopTime == value)
                    return;

                ChangePropertyAndNotify (() => StopTime, delegate {
                    stopTime = value;
                });

                IsRunning = stopTime != null;

                MarkDirty ();
            }
        }

        private long duration;

        [SQLite.Ignore]
        public long Duration {
            get { return duration; }
            set {
                if (duration == value)
                    return;

                ChangePropertyAndNotify (() => Duration, delegate {
                    duration = value;
                });

                if (RawDuration < 0) {
                    RawDuration = (long)(value - (DateTime.UtcNow - UnixStart).TotalSeconds);
                } else {
                    RawDuration = value;
                }

                MarkDirty ();
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

        public long RawDuration {
            get { return rawDuration; }
            set {
                if (rawDuration == value)
                    return;

                ChangePropertyAndNotify (() => RawDuration, delegate {
                    rawDuration = value;
                });

                IsRunning = value < 0;
                UpdateDuration ();

                MarkDirty ();
            }
        }

        private string createdWith;

        public string CreatedWith {
            get { return createdWith; }
            set {
                if (createdWith == value)
                    return;

                ChangePropertyAndNotify (() => CreatedWith, delegate {
                    createdWith = value;
                });

                MarkDirty ();
            }
        }

        public ISet<string> Tags {
            get {
                // TODO: Implement Tags logic
                return null;
            }
        }

        private bool durationOnly;

        public bool DurationOnly {
            get { return durationOnly; }
            set {
                if (durationOnly == value)
                    return;

                ChangePropertyAndNotify (() => DurationOnly, delegate {
                    durationOnly = value;
                });

                MarkDirty ();
            }
        }

        private bool running;

        public bool IsRunning {
            get { return running; }
            set {
                if (running == value)
                    return;

                ChangePropertyAndNotify (() => IsRunning, delegate {
                    running = value;
                });

                if (IsRunning && RawDuration >= 0) {
                    RawDuration = (long)(RawDuration - (DateTime.UtcNow - UnixStart).TotalSeconds);
                } else if (!IsRunning && RawDuration < 0) {
                    RawDuration = (long)((DateTime.UtcNow - UnixStart).TotalSeconds - RawDuration);
                }

                MarkDirty ();
            }
        }

        #endregion

        #region Relations

        public long? WorkspaceId {
            get { return GetForeignId (workspaceRelationId); }
            set { SetForeignId (workspaceRelationId, value); }
        }

        [SQLite.Ignore]
        public WorkspaceModel Workspace {
            get { return GetForeignModel<WorkspaceModel> (workspaceRelationId); }
            set { SetForeignModel (workspaceRelationId, value); }
        }

        public long? ProjectId {
            get { return GetForeignId (projectRelationId); }
            set { SetForeignId (projectRelationId, value); }
        }

        [SQLite.Ignore]
        public ProjectModel Project {
            get { return GetForeignModel<ProjectModel> (projectRelationId); }
            set { SetForeignModel (projectRelationId, value); }
        }

        public long? TaskId {
            get { return GetForeignId (taskRelationId); }
            set { SetForeignId (taskRelationId, value); }
        }

        [SQLite.Ignore]
        public TaskModel Task {
            get { return GetForeignModel<TaskModel> (taskRelationId); }
            set { SetForeignModel (taskRelationId, value); }
        }

        public long? UserId {
            get { return GetForeignId (userRelationId); }
            set { SetForeignId (userRelationId, value); }
        }

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

            if (property == propertyIsShared
                || property == propertyIsRunning
                || property == propertyIsPersisted) {
                if (IsShared && IsRunning && IsPersisted) {
                    // TODO: Make sure that this is the only time entry running
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

            return Model.GetShared (new TimeEntryModel () {
                Id = TimeEntryModel.NextId,
                WorkspaceId = WorkspaceId,
                ProjectId = ProjectId,
                TaskId = TaskId,
                Description = Description,
                StartTime = DateTime.UtcNow,
                DurationOnly = DurationOnly,
                CreatedWith = TimeEntryModel.UserAgent,
//                Tags = Tags,
                IsBillable = IsBillable,
                IsRunning = true,
            });
        }

        #endregion

    }
}
