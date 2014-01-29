using System;
using System.Linq;
using System.Linq.Expressions;
using Newtonsoft.Json;

namespace Toggl.Phoebe.Data.Models
{
    public class TagModel : Model
    {
        private static string GetPropertyName<T> (Expression<Func<TagModel, T>> expr)
        {
            return expr.ToPropertyName ();
        }

        private readonly int workspaceRelationId;
        private readonly RelatedModelsCollection<TimeEntryModel, TimeEntryTagModel, TimeEntryModel, TagModel> timeEntriesCollection;

        public TagModel ()
        {
            workspaceRelationId = ForeignRelation<WorkspaceModel> (PropertyWorkspaceId, PropertyWorkspace);
            timeEntriesCollection = new RelatedModelsCollection<TimeEntryModel, TimeEntryTagModel, TimeEntryModel, TagModel> (this);
        }

        protected override void Validate (ValidationContext ctx)
        {
            base.Validate (ctx);

            if (ctx.HasChanged (PropertyName)) {
                if (String.IsNullOrWhiteSpace (Name)) {
                    ctx.AddError (PropertyName, "Cannot be empty.");
                } else {
                    var duplicates = Model.Query<TagModel> ((t) => t.Name == Name && t.Id != Id).Count ();
                    if (duplicates != 0) {
                        ctx.AddError (PropertyName, "Must be unique.");
                    }
                }
            }
        }

        public override void Delete ()
        {
            base.Delete ();
            TimeEntries.Clear ();
        }

        #region Data

        private string name;
        public static readonly string PropertyName = GetPropertyName ((m) => m.Name);

        [JsonProperty ("name")]
        public string Name {
            get { return name; }
            set {
                if (name == value)
                    return;

                ChangePropertyAndNotify (PropertyName, delegate {
                    name = value;
                });
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

        [SQLite.Ignore]
        [JsonProperty ("wid"), JsonConverter (typeof(ForeignKeyJsonConverter))]
        public WorkspaceModel Workspace {
            get { return GetForeignModel<WorkspaceModel> (workspaceRelationId); }
            set { SetForeignModel (workspaceRelationId, value); }
        }

        [SQLite.Ignore]
        public RelatedModelsCollection<TimeEntryModel, TimeEntryTagModel, TimeEntryModel, TagModel> TimeEntries {
            get { return timeEntriesCollection; }
        }

        #endregion

    }
}
