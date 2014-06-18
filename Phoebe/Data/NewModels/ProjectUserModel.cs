using System;
using System.Linq.Expressions;
using Toggl.Phoebe.Data.DataObjects;

namespace Toggl.Phoebe.Data.NewModels
{
    public class ProjectUserModel : Model<ProjectUserData>
    {
        private static string GetPropertyName<T> (Expression<Func<ProjectUserModel, T>> expr)
        {
            return expr.ToPropertyName ();
        }

        public static new readonly string PropertyId = Model<ProjectUserData>.PropertyId;
        public static readonly string PropertyIsManager = GetPropertyName (m => m.IsManager);
        public static readonly string PropertyHourlyRate = GetPropertyName (m => m.HourlyRate);
        public static readonly string PropertyProject = GetPropertyName (m => m.Project);
        public static readonly string PropertyUser = GetPropertyName (m => m.User);

        public ProjectUserModel ()
        {
        }

        public ProjectUserModel (ProjectUserData data) : base (data)
        {
        }

        public ProjectUserModel (Guid id) : base (id)
        {
        }

        protected override ProjectUserData Duplicate (ProjectUserData data)
        {
            return new ProjectUserData (data);
        }

        protected override void OnBeforeSave ()
        {
            if (Data.ProjectId == Guid.Empty) {
                throw new ValidationException ("Project must be set for ProjectUser model.");
            }
            if (Data.UserId == Guid.Empty) {
                throw new ValidationException ("User must be set for ProjectUser model.");
            }
        }

        protected override void DetectChangedProperties (ProjectUserData oldData, ProjectUserData newData)
        {
            base.DetectChangedProperties (oldData, newData);
            if (oldData.IsManager != newData.IsManager)
                OnPropertyChanged (PropertyIsManager);
            if (oldData.HourlyRate != newData.HourlyRate)
                OnPropertyChanged (PropertyHourlyRate);
            if (oldData.ProjectId != newData.ProjectId || project.IsNewInstance)
                OnPropertyChanged (PropertyProject);
            if (oldData.UserId != newData.UserId || user.IsNewInstance)
                OnPropertyChanged (PropertyUser);
        }

        public bool IsManager {
            get {
                EnsureLoaded ();
                return Data.IsManager;
            }
            set {
                if (IsManager == value)
                    return;

                MutateData (data => data.IsManager = value);
            }
        }

        public int HourlyRate {
            get {
                EnsureLoaded ();
                return Data.HourlyRate;
            }
            set {
                if (HourlyRate == value)
                    return;

                MutateData (data => data.HourlyRate = value);
            }
        }

        private ForeignRelation<ProjectModel> project;
        private ForeignRelation<UserModel> user;

        protected override void InitializeRelations ()
        {
            base.InitializeRelations ();

            project = new ForeignRelation<ProjectModel> () {
                ShouldLoad = EnsureLoaded,
                Factory = id => new ProjectModel (id),
                Changed = m => MutateData (data => data.ProjectId = m.Id),
            };

            user = new ForeignRelation<UserModel> () {
                ShouldLoad = EnsureLoaded,
                Factory = id => new UserModel (id),
                Changed = m => MutateData (data => data.UserId = m.Id),
            };
        }

        [ModelRelation]
        public ProjectModel Project {
            get { return project.Get (Data.ProjectId); }
            set { project.Set (value); }
        }

        [ModelRelation]
        public UserModel User {
            get { return user.Get (Data.UserId); }
            set { user.Set (value); }
        }

        public static implicit operator ProjectUserModel (ProjectUserData data)
        {
            return new ProjectUserModel (data);
        }

        public static implicit operator ProjectUserData (ProjectUserModel model)
        {
            return model.Data;
        }
    }
}
