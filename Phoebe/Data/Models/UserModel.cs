using System;
using System.Linq;
using System.Linq.Expressions;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace Toggl.Phoebe.Data.Models
{
    public class UserModel : Model
    {
        private static string GetPropertyName<T> (Expression<Func<UserModel, T>> expr)
        {
            return expr.ToPropertyName ();
        }

        private readonly int defaultWorkspaceRelationId;
        private readonly RelatedModelsCollection<WorkspaceModel, WorkspaceUserModel, WorkspaceModel, UserModel> workspacesCollection;
        private readonly RelatedModelsCollection<ProjectModel, ProjectUserModel, ProjectModel, UserModel> projectsCollection;

        public UserModel ()
        {
            defaultWorkspaceRelationId = ForeignRelation<WorkspaceModel> (PropertyDefaultWorkspaceId, PropertyDefaultWorkspace);
            workspacesCollection = new RelatedModelsCollection<WorkspaceModel, WorkspaceUserModel, WorkspaceModel, UserModel> (this);
            projectsCollection = new RelatedModelsCollection<ProjectModel, ProjectUserModel, ProjectModel, UserModel> (this);
        }

        public IEnumerable<ProjectModel> GetAvailableProjects (WorkspaceModel workspace = null)
        {
            lock (SyncRoot) {
                workspace = workspace ?? DefaultWorkspace;
                if (workspace == null) {
                    throw new ArgumentNullException ("workspace", "Must specify a workspace, when user has no default one.");
                }

                IEnumerable<ProjectModel> projects;
                projects = workspace.Projects.NotDeleted ().Where ((m) => m.IsActive && m.IsPrivate != true);
                projects = projects.Union (Projects.Select ((m) => m.From).Where ((m) => m.IsActive && m.WorkspaceId == workspace.Id));
                return projects.OrderBy ((m) => m.Name).ToList ();
            }
        }

        public IEnumerable<ProjectModel> GetAllAvailableProjects ()
        {
            lock (SyncRoot) {
                var projectIds = Model.Query<ProjectUserModel> (m => m.ToId == Id && m.FromId != null)
                    .NotDeleted ()
                    .Select (m => m.FromId)
                    .ToList ();

                return Model.Query<ProjectModel> (m => m.IsActive)
                    .Where (m => m.IsPrivate == false || projectIds.Contains (m.Id))
                    .NotDeleted ()
                    .OrderBy (m => m.Name)
                    .ToList ();
            }
        }

        #region Data

        private string name;
        public static readonly string PropertyName = GetPropertyName ((m) => m.Name);

        [JsonProperty ("fullname")]
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

        private string email;
        public static readonly string PropertyEmail = GetPropertyName ((m) => m.Email);

        [JsonProperty ("email")]
        public string Email {
            get {
                lock (SyncRoot) {
                    return email;
                }
            }
            set {
                lock (SyncRoot) {
                    if (email == value)
                        return;

                    ChangePropertyAndNotify (PropertyEmail, delegate {
                        email = value;
                    });
                }
            }
        }

        private string password;
        public static readonly string PropertyPassword = GetPropertyName ((m) => m.Password);

        [JsonProperty ("password", NullValueHandling = NullValueHandling.Include)]
        [SQLite.Ignore]
        public string Password {
            get {
                lock (SyncRoot) {
                    return password;
                }
            }
            set {
                lock (SyncRoot) {
                    if (password == value)
                        return;

                    ChangePropertyAndNotify (PropertyPassword, delegate {
                        password = value;
                    });
                }
            }
        }

        private string googleAccessToken;
        public static readonly string PropertyGoogleAccessToken = GetPropertyName ((m) => m.GoogleAccessToken);

        [JsonProperty ("google_access_token", NullValueHandling = NullValueHandling.Ignore)]
        [SQLite.Ignore]
        public string GoogleAccessToken {
            get {
                lock (SyncRoot) {
                    return googleAccessToken;
                }
            }
            set {
                lock (SyncRoot) {
                    if (googleAccessToken == value)
                        return;

                    ChangePropertyAndNotify (PropertyGoogleAccessToken, delegate {
                        googleAccessToken = value;
                    });
                }
            }
        }

        private string apiToken;
        public static readonly string PropertyApiToken = GetPropertyName ((m) => m.ApiToken);

        [DontDirty]
        [JsonProperty ("api_token", NullValueHandling = NullValueHandling.Ignore)]
        [SQLite.Ignore]
        public string ApiToken {
            get {
                lock (SyncRoot) {
                    return apiToken;
                }
            }
            set {
                lock (SyncRoot) {
                    if (apiToken == value)
                        return;

                    ChangePropertyAndNotify (PropertyApiToken, delegate {
                        apiToken = value;
                    });
                }
            }
        }

        private DayOfWeek startOfWeek;
        public static readonly string PropertyStartOfWeek = GetPropertyName ((m) => m.StartOfWeek);

        [JsonProperty ("beginning_of_week")]
        public DayOfWeek StartOfWeek {
            get {
                lock (SyncRoot) {
                    return startOfWeek;
                }
            }
            set {
                lock (SyncRoot) {
                    if (startOfWeek == value)
                        return;

                    ChangePropertyAndNotify (PropertyStartOfWeek, delegate {
                        startOfWeek = value;
                    });
                }
            }
        }

        private string dateFormat;
        public static readonly string PropertyDateFormat = GetPropertyName ((m) => m.DateFormat);

        [JsonProperty ("date_format")]
        public string DateFormat {
            get {
                lock (SyncRoot) {
                    return dateFormat;
                }
            }
            set {
                lock (SyncRoot) {
                    if (dateFormat == value)
                        return;

                    ChangePropertyAndNotify (PropertyDateFormat, delegate {
                        dateFormat = value;
                    });
                }
            }
        }

        private string timeFormat;
        public static readonly string PropertyTimeFormat = GetPropertyName ((m) => m.TimeFormat);

        [JsonProperty ("timeofday_format")]
        public string TimeFormat {
            get {
                lock (SyncRoot) {
                    return timeFormat;
                }
            }
            set {
                lock (SyncRoot) {
                    if (timeFormat == value)
                        return;

                    ChangePropertyAndNotify (PropertyTimeFormat, delegate {
                        timeFormat = value;
                    });
                }
            }
        }

        private string imageUrl;
        public static readonly string PropertyImageUrl = GetPropertyName ((m) => m.ImageUrl);

        [JsonProperty ("image_url")]
        public string ImageUrl {
            get {
                lock (SyncRoot) {
                    return imageUrl;
                }
            }
            set {
                lock (SyncRoot) {
                    if (imageUrl == value)
                        return;

                    ChangePropertyAndNotify (PropertyImageUrl, delegate {
                        imageUrl = value;
                    });
                }
            }
        }

        private string locale;
        public static readonly string PropertyLocale = GetPropertyName ((m) => m.Locale);

        [JsonProperty ("language")]
        public string Locale {
            get {
                lock (SyncRoot) {
                    return locale;
                }
            }
            set {
                lock (SyncRoot) {
                    if (locale == value)
                        return;

                    ChangePropertyAndNotify (PropertyLocale, delegate {
                        locale = value;
                    });
                }
            }
        }

        private string timezone;
        public static readonly string PropertyTimezone = GetPropertyName ((m) => m.Timezone);

        [JsonProperty ("timezone")]
        public string Timezone {
            get {
                lock (SyncRoot) {
                    return timezone;
                }
            }
            set {
                lock (SyncRoot) {
                    if (timezone == value)
                        return;

                    ChangePropertyAndNotify (PropertyTimezone, delegate {
                        timezone = value;
                    });
                }
            }
        }

        private bool sendProductEmails;
        public static readonly string PropertySendProductEmails = GetPropertyName ((m) => m.SendProductEmails);

        [JsonProperty ("send_product_emails")]
        public bool SendProductEmails {
            get {
                lock (SyncRoot) {
                    return sendProductEmails;
                }
            }
            set {
                lock (SyncRoot) {
                    if (sendProductEmails == value)
                        return;

                    ChangePropertyAndNotify (PropertySendProductEmails, delegate {
                        sendProductEmails = value;
                    });
                }
            }
        }

        private bool sendTimerNotifications;
        public static readonly string PropertySendTimerNotifications = GetPropertyName ((m) => m.SendTimerNotifications);

        [JsonProperty ("send_timer_notifications")]
        public bool SendTimerNotifications {
            get {
                lock (SyncRoot) {
                    return sendTimerNotifications;
                }
            }
            set {
                lock (SyncRoot) {
                    if (sendTimerNotifications == value)
                        return;

                    ChangePropertyAndNotify (PropertySendTimerNotifications, delegate {
                        sendTimerNotifications = value;
                    });
                }
            }
        }

        private bool sendWeeklyReport;
        public static readonly string PropertySendWeeklyReport = GetPropertyName ((m) => m.SendWeeklyReport);

        [JsonProperty ("send_weekly_report")]
        public bool SendWeeklyReport {
            get {
                lock (SyncRoot) {
                    return sendWeeklyReport;
                }
            }
            set {
                lock (SyncRoot) {
                    if (sendWeeklyReport == value)
                        return;

                    ChangePropertyAndNotify (PropertySendWeeklyReport, delegate {
                        sendWeeklyReport = value;
                    });
                }
            }
        }

        private TrackingMode trackingMode;
        public static readonly string PropertyTrackingMode = GetPropertyName ((m) => m.TrackingMode);

        public TrackingMode TrackingMode {
            get {
                lock (SyncRoot) {
                    return trackingMode;
                }
            }
            set {
                lock (SyncRoot) {
                    if (trackingMode == value)
                        return;

                    ChangePropertyAndNotify (PropertyTrackingMode, delegate {
                        trackingMode = value;
                    });
                }
            }
        }

        [JsonProperty ("store_start_and_stop_time")]
        private bool StoreStartAndStopTime {
            get { return TrackingMode == TrackingMode.StartNew; }
            set { TrackingMode = value ? TrackingMode.StartNew : TrackingMode.Continue; }
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
            get {
                lock (SyncRoot) {
                    return createdWith;
                }
            }
            set {
                lock (SyncRoot) {
                    if (createdWith == value)
                        return;

                    ChangePropertyAndNotify (PropertyCreatedWith, delegate {
                        createdWith = value;
                    });
                }
            }
        }

        #endregion

        #region Relations

        public static readonly string PropertyDefaultWorkspaceId = GetPropertyName ((m) => m.DefaultWorkspaceId);

        public Guid? DefaultWorkspaceId {
            get { return GetForeignId (defaultWorkspaceRelationId); }
            set { SetForeignId (defaultWorkspaceRelationId, value); }
        }

        public static readonly string PropertyDefaultWorkspace = GetPropertyName ((m) => m.DefaultWorkspace);

        [DontDirty]
        [SQLite.Ignore]
        [JsonProperty ("default_wid"), JsonConverter (typeof(ForeignKeyJsonConverter))]
        public WorkspaceModel DefaultWorkspace {
            get { return GetForeignModel<WorkspaceModel> (defaultWorkspaceRelationId); }
            set { SetForeignModel (defaultWorkspaceRelationId, value); }
        }

        public IModelQuery<TimeEntryModel> TimeEntries {
            get { return Model.Query<TimeEntryModel> ((m) => m.UserId == Id); }
        }

        public RelatedModelsCollection<WorkspaceModel, WorkspaceUserModel, WorkspaceModel, UserModel> Workspaces {
            get { return workspacesCollection; }
        }

        public RelatedModelsCollection<ProjectModel, ProjectUserModel, ProjectModel, UserModel> Projects {
            get { return projectsCollection; }
        }

        #endregion
    }
}
