using System;
using System.Linq.Expressions;
using Toggl.Phoebe.Data.DataObjects;

namespace Toggl.Phoebe.Data.Models
{
    public class TaskModel : Model<TaskData>
    {
        private static string GetPropertyName<T> (Expression<Func<TaskModel, T>> expr)
        {
            return expr.ToPropertyName ();
        }

        public static new readonly string PropertyId = Model<TaskData>.PropertyId;
        public static readonly string PropertyName = GetPropertyName (m => m.Name);
        public static readonly string PropertyIsActive = GetPropertyName (m => m.IsActive);
        public static readonly string PropertyEstimate = GetPropertyName (m => m.Estimate);
        public static readonly string PropertyWorkspace = GetPropertyName (m => m.Workspace);
        public static readonly string PropertyProject = GetPropertyName (m => m.Project);

        public TaskModel ()
        {
        }

        public TaskModel (TaskData data) : base (data)
        {
        }

        public TaskModel (Guid id) : base (id)
        {
        }

        protected override TaskData Duplicate (TaskData data)
        {
            return new TaskData (data);
        }

        protected override void OnBeforeSave ()
        {
            if (Data.WorkspaceId == Guid.Empty) {
                throw new ValidationException ("Workspace must be set for Task model.");
            }
            if (Data.ProjectId == Guid.Empty) {
                throw new ValidationException ("Project must be set for Task model.");
            }
        }

        protected override void DetectChangedProperties (TaskData oldData, TaskData newData)
        {
            base.DetectChangedProperties (oldData, newData);
            if (oldData.Name != newData.Name)
                OnPropertyChanged (PropertyName);
            if (oldData.IsActive != newData.IsActive)
                OnPropertyChanged (PropertyIsActive);
            if (oldData.Estimate != newData.Estimate)
                OnPropertyChanged (PropertyEstimate);
            if (oldData.WorkspaceId != newData.WorkspaceId || workspace.IsNewInstance)
                OnPropertyChanged (PropertyWorkspace);
            if (oldData.ProjectId != newData.ProjectId || project.IsNewInstance)
                OnPropertyChanged (PropertyProject);
        }

        public string Name {
            get {
                EnsureLoaded ();
                return Data.Name;
            }
            set {
                if (Name == value)
                    return;

                MutateData (data => data.Name = value);
            }
        }

        public bool IsActive {
            get {
                EnsureLoaded ();
                return Data.IsActive;
            }
            set {
                if (IsActive == value)
                    return;

                MutateData (data => data.IsActive = value);
            }
        }

        public long Estimate {
            get {
                EnsureLoaded ();
                return Data.Estimate;
            }
            set {
                if (Estimate == value)
                    return;

                MutateData (data => data.Estimate = value);
            }
        }

        private ForeignRelation<WorkspaceModel> workspace;
        private ForeignRelation<ProjectModel> project;

        protected override void InitializeRelations ()
        {
            base.InitializeRelations ();

            workspace = new ForeignRelation<WorkspaceModel> () {
                ShouldLoad = EnsureLoaded,
                Factory = id => new WorkspaceModel (id),
                Changed = m => MutateData (data => data.WorkspaceId = m.Id),
            };

            project = new ForeignRelation<ProjectModel> () {
                ShouldLoad = EnsureLoaded,
                Factory = id => new ProjectModel (id),
                Changed = m => MutateData (data => data.ProjectId = m.Id),
            };
        }

        [ModelRelation]
        public WorkspaceModel Workspace {
            get { return workspace.Get (Data.WorkspaceId); }
            set { workspace.Set (value); }
        }

        [ModelRelation]
        public ProjectModel Project {
            get { return project.Get (Data.ProjectId); }
            set { project.Set (value); }
        }

        public static explicit operator TaskModel (TaskData data)
        {
            if (data == null)
                return null;
            return new TaskModel (data);
        }

        public static implicit operator TaskData (TaskModel model)
        {
            return model.Data;
        }
    }
}
