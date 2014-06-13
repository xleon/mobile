using System;
using System.Linq.Expressions;
using Toggl.Phoebe.Data.DataObjects;

namespace Toggl.Phoebe.Data.NewModels
{
    public class TimeEntryModel : Model<TimeEntryData>
    {
        private static string GetPropertyName<T> (Expression<Func<TimeEntryModel, T>> expr)
        {
            return expr.ToPropertyName ();
        }

        public static new readonly string PropertyId = Model<TimeEntryData>.PropertyId;
        public static readonly string PropertyState = GetPropertyName (m => m.State);
        public static readonly string PropertyDescription = GetPropertyName (m => m.Description);
        public static readonly string PropertyStartTime = GetPropertyName (m => m.StartTime);
        public static readonly string PropertyStopTime = GetPropertyName (m => m.StopTime);
        public static readonly string PropertyDurationOnly = GetPropertyName (m => m.DurationOnly);
        public static readonly string PropertyIsBillable = GetPropertyName (m => m.IsBillable);
        public static readonly string PropertyUser = GetPropertyName (m => m.User);
        public static readonly string PropertyWorkspace = GetPropertyName (m => m.Workspace);
        public static readonly string PropertyProject = GetPropertyName (m => m.Project);
        public static readonly string PropertyTask = GetPropertyName (m => m.Task);

        public TimeEntryModel ()
        {
        }

        public TimeEntryModel (TimeEntryData data) : base (data)
        {
        }

        public TimeEntryModel (Guid id) : base (id)
        {
        }

        protected override TimeEntryData Duplicate (TimeEntryData data)
        {
            return new TimeEntryData (data);
        }

        protected override void OnBeforeSave ()
        {
            if (Data.UserId == Guid.Empty) {
                throw new ValidationException ("User must be set for TimeEntry model.");
            }
            if (Data.WorkspaceId == Guid.Empty) {
                throw new ValidationException ("Workspace must be set for TimeEntry model.");
            }
        }

        protected override void DetectChangedProperties (TimeEntryData oldData, TimeEntryData newData)
        {
            base.DetectChangedProperties (oldData, newData);
            if (oldData.State != newData.State)
                OnPropertyChanged (PropertyState);
            if (oldData.Description != newData.Description)
                OnPropertyChanged (PropertyDescription);
            if (oldData.StartTime != newData.StartTime)
                OnPropertyChanged (PropertyStartTime);
            if (oldData.StopTime != newData.StopTime)
                OnPropertyChanged (PropertyStopTime);
            if (oldData.DurationOnly != newData.DurationOnly)
                OnPropertyChanged (PropertyDurationOnly);
            if (oldData.IsBillable != newData.IsBillable)
                OnPropertyChanged (PropertyIsBillable);
            if (oldData.UserId != newData.UserId)
                OnPropertyChanged (PropertyUser);
            if (oldData.WorkspaceId != newData.WorkspaceId)
                OnPropertyChanged (PropertyWorkspace);
            if (oldData.ProjectId != newData.ProjectId)
                OnPropertyChanged (PropertyProject);
            if (oldData.TaskId != newData.TaskId)
                OnPropertyChanged (PropertyTask);
        }

        public TimeEntryState State {
            get {
                EnsureLoaded ();
                return Data.State;
            }
            set {
                if (State == value)
                    return;

                MutateData (data => {
                    // Adjust start-time to keep duration same when switching to running state
                    if (value == TimeEntryState.Running && data.StopTime.HasValue) {
                        var duration = GetDuration (data);
                        var now = Time.UtcNow;
                        data.StopTime = null;
                        data.StartTime = (now - duration).Truncate (TimeSpan.TicksPerSecond);
                    }

                    data.State = value;
                });
            }
        }

        public string Description {
            get {
                EnsureLoaded ();
                return Data.Description;
            }
            set {
                if (Description == value)
                    return;

                MutateData (data => data.Description = value);
            }
        }

        public DateTime StartTime {
            get {
                EnsureLoaded ();
                return Data.StartTime;
            }
            set {
                value.ToUtc ().Truncate (TimeSpan.TicksPerSecond);
                if (StartTime == value)
                    return;

                MutateData (data => {
                    var duration = GetDuration (data);

                    data.StartTime = value;

                    if (State != TimeEntryState.Running) {
                        if (data.StopTime.HasValue) {
                            data.StopTime = data.StartTime + duration;
                        } else {
                            var now = Time.UtcNow;

                            data.StopTime = data.StartTime.Date
                                    .AddHours (now.Hour)
                                    .AddMinutes (now.Minute)
                                    .AddSeconds (data.StartTime.Second);

                            if (data.StopTime < data.StartTime) {
                                data.StopTime = data.StartTime + duration;
                            }
                        }
                    }
                });
            }
        }

        public DateTime? StopTime {
            get {
                EnsureLoaded ();
                return Data.StopTime;
            }
            set {
                value.ToUtc ().Truncate (TimeSpan.TicksPerSecond);
                if (StopTime == value)
                    return;

                MutateData (data => data.StopTime = value);
            }
        }

        public bool DurationOnly {
            get {
                EnsureLoaded ();
                return Data.DurationOnly;
            }
            set {
                if (DurationOnly == value)
                    return;

                MutateData (data => data.DurationOnly = value);
            }
        }

        public bool IsBillable {
            get {
                EnsureLoaded ();
                return Data.IsBillable;
            }
            set {
                if (IsBillable == value)
                    return;

                MutateData (data => data.IsBillable = value);
            }
        }

        public TimeSpan GetDuration ()
        {
            return GetDuration (Data, Time.UtcNow);
        }

        private TimeSpan GetDuration (TimeEntryData data)
        {
            return GetDuration (data, Time.UtcNow);
        }

        private static TimeSpan GetDuration (TimeEntryData data, DateTime now)
        {
            if (data.StartTime == DateTime.MinValue) {
                return TimeSpan.Zero;
            }

            var duration = (data.StopTime ?? now) - data.StartTime;
            if (duration < TimeSpan.Zero) {
                duration = TimeSpan.Zero;
            }
            return duration;
        }

        public void SetDuration (TimeSpan value)
        {
            MutateData (data => SetDuration (data, value));
        }

        private static void SetDuration (TimeEntryData data, TimeSpan value)
        {
            var now = Time.UtcNow;

            if (data.State == TimeEntryState.Finished) {
                data.StopTime = data.StartTime + value;
            } else if (data.State == TimeEntryState.New) {
                if (value == TimeSpan.Zero) {
                    data.StartTime = DateTime.MinValue;
                    data.StopTime = null;
                } else if (data.StopTime.HasValue) {
                    data.StartTime = data.StopTime.Value - value;
                } else {
                    data.StartTime = now - value;
                    data.StopTime = now;
                }
            } else {
                data.StartTime = now - value;
            }

            data.StartTime = data.StartTime.Truncate (TimeSpan.TicksPerSecond);
            data.StopTime = data.StopTime.Truncate (TimeSpan.TicksPerSecond);
        }

        private ForeignRelation<UserModel> user;
        private ForeignRelation<WorkspaceModel> workspace;
        private ForeignRelation<ProjectModel> project;
        private ForeignRelation<TaskModel> task;

        protected override void InitializeRelations ()
        {
            base.InitializeRelations ();

            user = new ForeignRelation<UserModel> () {
                Factory = id => new UserModel (id),
                Changed = id => MutateData (data => data.UserId = id.Value),
            };

            workspace = new ForeignRelation<WorkspaceModel> () {
                Factory = id => new WorkspaceModel (id),
                Changed = id => MutateData (data => data.WorkspaceId = id.Value),
            };

            project = new ForeignRelation<ProjectModel> () {
                Required = false,
                Factory = id => new ProjectModel (id),
                // TODO: Update IsBillable flag
                /*
                    IsBillable = Project.IsBillable;
                 */
                Changed = id => MutateData (data => data.ProjectId = id),
            };

            task = new ForeignRelation<TaskModel> () {
                Required = false,
                Factory = id => new TaskModel (id),
                Changed = id => MutateData (data => data.TaskId = id),
            };
        }

        [ForeignRelation]
        public UserModel User {
            get { return user.Get (Data.UserId); }
            set { user.Set (value); }
        }

        [ForeignRelation]
        public WorkspaceModel Workspace {
            get { return workspace.Get (Data.WorkspaceId); }
            set { workspace.Set (value); }
        }

        [ForeignRelation (Required = false)]
        public ProjectModel Project {
            get { return project.Get (Data.ProjectId); }
            set { project.Set (value); }
        }

        [ForeignRelation (Required = false)]
        public TaskModel Task {
            get { return task.Get (Data.TaskId); }
            set { task.Set (value); }
        }

        public static implicit operator TimeEntryModel (TimeEntryData data)
        {
            return new TimeEntryModel (data);
        }

        public static implicit operator TimeEntryData (TimeEntryModel model)
        {
            return model.Data;
        }
    }
}
