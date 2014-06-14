using System;
using System.Linq.Expressions;
using Toggl.Phoebe.Data.DataObjects;

namespace Toggl.Phoebe.Data.NewModels
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
            if (oldData.Name != newData.Name)
                OnPropertyChanged (PropertyName);
            if (oldData.WorkspaceId != newData.WorkspaceId || workspace.IsNewInstance)
                OnPropertyChanged (PropertyWorkspace);
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

        private ForeignRelation<WorkspaceModel> workspace;

        protected override void InitializeRelations ()
        {
            base.InitializeRelations ();

            workspace = new ForeignRelation<WorkspaceModel> () {
                ShouldLoad = EnsureLoaded,
                Factory = id => new WorkspaceModel (id),
                Changed = id => MutateData (data => data.WorkspaceId = id.Value),
            };
        }

        [ForeignRelation]
        public WorkspaceModel Workspace {
            get { return workspace.Get (Data.WorkspaceId); }
            set { workspace.Set (value); }
        }

        public static implicit operator TagModel (TagData data)
        {
            return new TagModel (data);
        }

        public static implicit operator TagData (TagModel model)
        {
            return model.Data;
        }
    }
}
