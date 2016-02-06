using System;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Toggl.Phoebe.Data.DataObjects;
using XPlatUtils;

namespace Toggl.Phoebe.Data.Models
{
    public class TagModel : Model<TagData>
    {
        private static string GetPropertyName<T> (Expression<Func<TagModel, T>> expr)
        {
            return expr.ToPropertyName ();
        }

        public static new readonly string PropertyId = Model<TagData>.PropertyId;
        public static readonly string PropertyName = GetPropertyName (m => m.Name);
        public static readonly string PropertyWorkspace = GetPropertyName (m => m.Workspace);

        public TagModel ()
        {
        }

        public TagModel (TagData data) : base (data)
        {
        }

        public TagModel (Guid id) : base (id)
        {
        }

        protected override TagData Duplicate (TagData data)
        {
            return new TagData (data);
        }

        protected override void OnBeforeSave ()
        {
            if (Data.WorkspaceId == Guid.Empty) {
                throw new ValidationException ("Workspace must be set for Tag model.");
            }
        }

        protected override void DetectChangedProperties (TagData oldData, TagData newData)
        {
            base.DetectChangedProperties (oldData, newData);
            if (oldData.Name != newData.Name) {
                OnPropertyChanged (PropertyName);
            }
            if (oldData.WorkspaceId != newData.WorkspaceId || workspace.IsNewInstance) {
                OnPropertyChanged (PropertyWorkspace);
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

        private ForeignRelation<WorkspaceModel> workspace;

        protected override void InitializeRelations ()
        {
            base.InitializeRelations ();

            workspace = new ForeignRelation<WorkspaceModel> () {
                ShouldLoad = EnsureLoaded,
                Factory = id => new WorkspaceModel (id),
                Changed = m => MutateData (data => data.WorkspaceId = m.Id),
            };
        }

        [ModelRelation]
        public WorkspaceModel Workspace
        {
            get { return workspace.Get (Data.WorkspaceId); }
            set { workspace.Set (value); }
        }

        public static explicit operator TagModel (TagData data)
        {
            if (data == null) {
                return null;
            }
            return new TagModel (data);
        }

        public static implicit operator TagData (TagModel model)
        {
            return model.Data;
        }

        #region Static methods


        public async static Task<TagData> AddTag (Guid workspaceId, string name)
        {
            var newTag = new TagData {
                WorkspaceId = workspaceId,
                Name = name
            };

            MarkDirty (newTag);
            var dataStore = ServiceContainer.Resolve<IDataStore> ();
            return await dataStore.PutAsync (newTag);
        }


        #endregion
    }
}
