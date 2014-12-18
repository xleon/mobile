using System;
using System.Linq.Expressions;
using Toggl.Phoebe.Data.DataObjects;

namespace Toggl.Phoebe.Data.Models
{
    public class ProjectModel : Model<ProjectData>
    {
        private static string GetPropertyName<T> (Expression<Func<ProjectModel, T>> expr)
        {
            return expr.ToPropertyName ();
        }

        public static new readonly string PropertyId = Model<ProjectData>.PropertyId;
        public static readonly string PropertyName = GetPropertyName (m => m.Name);
        public static readonly string PropertyColor = GetPropertyName (m => m.Color);
        public static readonly string PropertyIsActive = GetPropertyName (m => m.IsActive);
        public static readonly string PropertyIsBillable = GetPropertyName (m => m.IsBillable);
        public static readonly string PropertyIsPrivate = GetPropertyName (m => m.IsPrivate);
        public static readonly string PropertyIsTemplate = GetPropertyName (m => m.IsTemplate);
        public static readonly string PropertyUseTasksEstimate = GetPropertyName (m => m.UseTasksEstimate);
        public static readonly string PropertyWorkspace = GetPropertyName (m => m.Workspace);
        public static readonly string PropertyClient = GetPropertyName (m => m.Client);

        public static readonly string[] HexColors = {
            "#4dc3ff", "#bc85e6", "#df7baa", "#f68d38", "#b27636",
            "#8ab734", "#14a88e", "#268bb5", "#6668b4", "#a4506c",
            "#67412c", "#3c6526", "#094558", "#bc2d07", "#999999"
        };

        public static readonly int DefaultColor = HexColors.Length - 1;

        public ProjectModel ()
        {
        }

        public ProjectModel (ProjectData data) : base (data)
        {
        }

        public ProjectModel (Guid id) : base (id)
        {
        }

        protected override ProjectData Duplicate (ProjectData data)
        {
            return new ProjectData (data);
        }

        protected override void OnBeforeSave ()
        {
            if (Data.WorkspaceId == Guid.Empty) {
                throw new ValidationException ("Workspace must be set for Project model.");
            }
        }

        protected override void DetectChangedProperties (ProjectData oldData, ProjectData newData)
        {
            base.DetectChangedProperties (oldData, newData);
            if (oldData.Name != newData.Name) {
                OnPropertyChanged (PropertyName);
            }
            if (oldData.Color != newData.Color) {
                OnPropertyChanged (PropertyColor);
            }
            if (oldData.IsActive != newData.IsActive) {
                OnPropertyChanged (PropertyIsActive);
            }
            if (oldData.IsBillable != newData.IsBillable) {
                OnPropertyChanged (PropertyIsBillable);
            }
            if (oldData.IsPrivate != newData.IsPrivate) {
                OnPropertyChanged (PropertyIsPrivate);
            }
            if (oldData.IsTemplate != newData.IsTemplate) {
                OnPropertyChanged (PropertyIsTemplate);
            }
            if (oldData.UseTasksEstimate != newData.UseTasksEstimate) {
                OnPropertyChanged (PropertyUseTasksEstimate);
            }
            if (oldData.WorkspaceId != newData.WorkspaceId || workspace.IsNewInstance) {
                OnPropertyChanged (PropertyWorkspace);
            }
            if (oldData.ClientId != newData.ClientId || client.IsNewInstance) {
                OnPropertyChanged (PropertyClient);
            }
        }

        public string Name
        {
            get {
                EnsureLoaded ();
                return Data.Name;
            } set {
                if (Name == value) {
                    return;
                }

                MutateData (data => data.Name = value);
            }
        }

        public int Color
        {
            get {
                EnsureLoaded ();
                return Data.Color;
            } set {
                // Make sure the value is in valid range:
                value = value % HexColors.Length;

                if (Color == value) {
                    return;
                }

                MutateData (data => data.Color = value);
            }
        }

        public string GetHexColor ()
        {
            return HexColors [Color % HexColors.Length];
        }

        public bool IsActive
        {
            get {
                EnsureLoaded ();
                return Data.IsActive;
            } set {
                if (IsActive == value) {
                    return;
                }

                MutateData (data => data.IsActive = value);
            }
        }

        public bool IsBillable
        {
            get {
                EnsureLoaded ();
                return Data.IsBillable;
            } set {
                if (IsBillable == value) {
                    return;
                }

                MutateData (data => data.IsBillable = value);
            }
        }

        public bool IsPrivate
        {
            get {
                EnsureLoaded ();
                return Data.IsPrivate;
            } set {
                if (IsPrivate == value) {
                    return;
                }

                MutateData (data => data.IsPrivate = value);
            }
        }

        public bool IsTemplate
        {
            get {
                EnsureLoaded ();
                return Data.IsTemplate;
            } set {
                if (IsTemplate == value) {
                    return;
                }

                MutateData (data => data.IsTemplate = value);
            }
        }

        public bool UseTasksEstimate
        {
            get {
                EnsureLoaded ();
                return Data.UseTasksEstimate;
            } set {
                if (UseTasksEstimate == value) {
                    return;
                }

                MutateData (data => data.UseTasksEstimate = value);
            }
        }

        private ForeignRelation<WorkspaceModel> workspace;
        private ForeignRelation<ClientModel> client;

        protected override void InitializeRelations ()
        {
            base.InitializeRelations ();

            workspace = new ForeignRelation<WorkspaceModel> () {
                ShouldLoad = EnsureLoaded,
                Factory = id => new WorkspaceModel (id),
                Changed = m => MutateData (data => data.WorkspaceId = m.Id),
            };

            client = new ForeignRelation<ClientModel> () {
                Required = false,
                ShouldLoad = EnsureLoaded,
                Factory = id => new ClientModel (id),
                Changed = m => MutateData (data => data.ClientId = GetOptionalId (m)),
            };
        }

        [ModelRelation]
        public WorkspaceModel Workspace
        {
            get { return workspace.Get (Data.WorkspaceId); }
            set { workspace.Set (value); }
        }

        [ModelRelation (Required = false)]
        public ClientModel Client
        {
            get { return client.Get (Data.ClientId); }
            set { client.Set (value); }
        }

        public static explicit operator ProjectModel (ProjectData data)
        {
            return data == null ? null : new ProjectModel (data);
        }

        public static implicit operator ProjectData (ProjectModel model)
        {
            return model.Data;
        }
    }
}
