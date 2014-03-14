using System;
using System.Linq.Expressions;
using Newtonsoft.Json;

namespace Toggl.Phoebe.Data.Models
{
    public class ProjectModel : Model
    {
        private static string GetPropertyName<T> (Expression<Func<ProjectModel, T>> expr)
        {
            return expr.ToPropertyName ();
        }

        private readonly int workspaceRelationId;
        private readonly int clientRelationId;
        private readonly RelatedModelsCollection<UserModel, ProjectUserModel, ProjectModel, UserModel> usersCollection;

        public ProjectModel ()
        {
            workspaceRelationId = ForeignRelation<WorkspaceModel> (PropertyWorkspaceId, PropertyWorkspace);
            clientRelationId = ForeignRelation<ClientModel> (PropertyClientId, PropertyClient);
            usersCollection = new RelatedModelsCollection<UserModel, ProjectUserModel, ProjectModel, UserModel> (this);
        }

        #region Data

        private bool active;
        public static readonly string PropertyIsActive = GetPropertyName ((m) => m.IsActive);

        [JsonProperty ("active")]
        public bool IsActive {
            get {
                lock (SyncRoot) {
                    return active;
                }
            }
            set {
                lock (SyncRoot) {

                    if (active == value)
                        return;

                    ChangePropertyAndNotify (PropertyIsActive, delegate {
                        active = value;
                    });
                }
            }
        }

        private bool priv;
        public static readonly string PropertyIsPrivate = GetPropertyName ((m) => m.IsPrivate);

        [JsonProperty ("is_private")]
        public bool IsPrivate {
            get {
                lock (SyncRoot) {
                    return priv;
                }
            }
            set {
                lock (SyncRoot) {

                    if (priv == value)
                        return;

                    ChangePropertyAndNotify (PropertyIsPrivate, delegate {
                        priv = value;
                    });
                }
            }
        }

        private bool taskEstimate;
        public static readonly string PropertyUseTasksEstimate = GetPropertyName ((m) => m.UseTasksEstimate);

        [JsonProperty ("auto_estimates")]
        public bool UseTasksEstimate {
            get {
                lock (SyncRoot) {
                    return taskEstimate;
                }
            }
            set {
                lock (SyncRoot) {
                    if (taskEstimate == value)
                        return;

                    ChangePropertyAndNotify (PropertyUseTasksEstimate, delegate {
                        taskEstimate = value;
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

        private int color;
        public static readonly string PropertyColor = GetPropertyName ((m) => m.Color);

        public int Color {
            get {
                lock (SyncRoot) {
                    return color;
                }
            }
            set {
                lock (SyncRoot) {
                    if (color == value)
                        return;

                    ChangePropertyAndNotify (PropertyColor, delegate {
                        color = value;
                    });
                }
            }
        }

        private static string[] HexColorsIndex = new string[] {
            "#4dc3ff", "#bc85e6", "#df7baa", "#f68d38", "#b27636",
            "#8ab734", "#14a88e", "#268bb5", "#6668b4", "#a4506c",
            "#67412c", "#3c6526", "#094558", "#bc2d07", "#999999"
        };

        [JsonProperty ("color")]
        private String ColorString {
            get { 
                return Color.ToString ();
            }
            set {
                try {
                    Color = Convert.ToInt32 (value) % HexColorsIndex.Length;
                } catch {
                    Color = HexColorsIndex.Length - 1; //Default color
                }
            }
        }

        public String GetHexColor ()
        {
            return HexColorsIndex [Color];
        }

        private bool template;
        public static readonly string PropertyIsTemplate = GetPropertyName ((m) => m.IsTemplate);

        [JsonProperty ("template")]
        public bool IsTemplate {
            get {
                lock (SyncRoot) {
                    return template;
                }
            }
            set {
                lock (SyncRoot) {
                    if (template == value)
                        return;

                    ChangePropertyAndNotify (PropertyIsTemplate, delegate {
                        template = value;
                    });
                }
            }
        }

        private string name;
        public static readonly string PropertyName = GetPropertyName ((m) => m.Name);

        [JsonProperty ("name")]
        public string Name {
            get {
                lock (SyncRoot) {
                    return name;
                }
            }
            set {
                lock (SyncRoot) {
                    if (name == value)
                        return;

                    ChangePropertyAndNotify (PropertyName, delegate {
                        name = value;
                    });
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

        [SQLite.Ignore]
        [JsonProperty ("wid"), JsonConverter (typeof(ForeignKeyJsonConverter))]
        public WorkspaceModel Workspace {
            get { return GetForeignModel<WorkspaceModel> (workspaceRelationId); }
            set { SetForeignModel (workspaceRelationId, value); }
        }

        public static readonly string PropertyClientId = GetPropertyName ((m) => m.ClientId);

        public Guid? ClientId {
            get { return GetForeignId (clientRelationId); }
            set { SetForeignId (clientRelationId, value); }
        }

        public static readonly string PropertyClient = GetPropertyName ((m) => m.Client);

        [SQLite.Ignore]
        [JsonProperty ("cid"), JsonConverter (typeof(ForeignKeyJsonConverter))]
        public ClientModel Client {
            get { return GetForeignModel<ClientModel> (clientRelationId); }
            set { SetForeignModel (clientRelationId, value); }
        }

        public IModelQuery<TaskModel> Tasks {
            get { return Model.Query<TaskModel> ((m) => m.ProjectId == Id); }
        }

        public IModelQuery<TimeEntryModel> TimeEntries {
            get { return Model.Query<TimeEntryModel> ((m) => m.ProjectId == Id); }
        }

        public RelatedModelsCollection<UserModel, ProjectUserModel, ProjectModel, UserModel> Users {
            get { return usersCollection; }
        }

        #endregion
    }
}
