using System;
using System.Linq.Expressions;
using Newtonsoft.Json;

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

        public UserModel ()
        {
            defaultWorkspaceRelationId = ForeignRelation<WorkspaceModel> (PropertyDefaultWorkspaceId, PropertyDefaultWorkspace);
            workspacesCollection = new RelatedModelsCollection<WorkspaceModel, WorkspaceUserModel, WorkspaceModel, UserModel> (this);
        }

        protected override void Validate (ValidationContext ctx)
        {
            base.Validate (ctx);

            if (ctx.HasChanged (PropertyName)) {
                if (String.IsNullOrWhiteSpace (Name)) {
                    ctx.AddError (PropertyName, "User name cannot be empty.");
                }
            }

            if (ctx.HasChanged (PropertyEmail)) {
                if (String.IsNullOrWhiteSpace (Email)) {
                    ctx.AddError (PropertyName, "User email cannot be empty.");
                } else if (!Email.Contains ("@")) {
                    ctx.AddError (PropertyName, "Invalid email address");
                }
            }

            if (ctx.HasChanged (PropertyPassword)) {
                if (String.IsNullOrEmpty (Password) && RemoteId != null) {
                    // User doesn't have to enter anything to keep old password
                } else if (String.IsNullOrWhiteSpace (Password)) {
                    ctx.AddError (PropertyName, "User password cannot be empty.");
                } else if (Password.Length <= 5) {
                    ctx.AddError (PropertyName, "Password must be at least 5 characters.");
                }
            }

            // TODO: More validation for User model

            if (ctx.HasChanged (PropertyDefaultWorkspaceId)) {
                ctx.ClearErrors (PropertyDefaultWorkspaceId);
                ctx.ClearErrors (PropertyDefaultWorkspace);

                if (RemoteId == null) {
                    // No need to specify default workspace when signing up
                } else if (DefaultWorkspaceId == null) {
                    ctx.AddError (PropertyDefaultWorkspaceId, "User must have a default workspace.");
                } else if (DefaultWorkspace == null) {
                    ctx.AddError (PropertyDefaultWorkspace, "Associated default workspace could not be found.");
                }
            }

        }

        #region Data

        private string name;
        public static readonly string PropertyName = GetPropertyName ((m) => m.Name);

        [JsonProperty ("fullname")]
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

        private string email;
        public static readonly string PropertyEmail = GetPropertyName ((m) => m.Email);

        [JsonProperty ("email")]
        public string Email {
            get { return email; }
            set {
                if (email == value)
                    return;

                ChangePropertyAndNotify (PropertyEmail, delegate {
                    email = value;
                });
            }
        }

        private string password;
        public static readonly string PropertyPassword = GetPropertyName ((m) => m.Password);

        [JsonProperty ("password", NullValueHandling = NullValueHandling.Include)]
        [SQLite.Ignore]
        public string Password {
            get { return password; }
            set {
                if (password == value)
                    return;

                ChangePropertyAndNotify (PropertyPassword, delegate {
                    password = value;
                });
            }
        }

        private string apiToken;
        public static readonly string PropertyApiToken = GetPropertyName ((m) => m.ApiToken);

        [DontDirty]
        [JsonProperty ("api_token", NullValueHandling = NullValueHandling.Ignore)]
        [SQLite.Ignore]
        public string ApiToken {
            get { return apiToken; }
            set {
                if (apiToken == value)
                    return;

                ChangePropertyAndNotify (PropertyApiToken, delegate {
                    apiToken = value;
                });
            }
        }

        private DayOfWeek startOfWeek;
        public static readonly string PropertyStartOfWeek = GetPropertyName ((m) => m.StartOfWeek);

        [JsonProperty ("beginning_of_week")]
        public DayOfWeek StartOfWeek {
            get { return startOfWeek; }
            set {
                if (startOfWeek == value)
                    return;

                ChangePropertyAndNotify (PropertyStartOfWeek, delegate {
                    startOfWeek = value;
                });
            }
        }

        private string dateFormat;
        public static readonly string PropertyDateFormat = GetPropertyName ((m) => m.DateFormat);

        [JsonProperty ("date_format")]
        public string DateFormat {
            get { return dateFormat; }
            set {
                if (dateFormat == value)
                    return;

                ChangePropertyAndNotify (PropertyDateFormat, delegate {
                    dateFormat = value;
                });
            }
        }

        private string timeFormat;
        public static readonly string PropertyTimeFormat = GetPropertyName ((m) => m.TimeFormat);

        [JsonProperty ("timeofday_format")]
        public string TimeFormat {
            get { return timeFormat; }
            set {
                if (timeFormat == value)
                    return;

                ChangePropertyAndNotify (PropertyTimeFormat, delegate {
                    timeFormat = value;
                });
            }
        }

        private string imageUrl;
        public static readonly string PropertyImageUrl = GetPropertyName ((m) => m.ImageUrl);

        [JsonProperty ("image_url")]
        public string ImageUrl {
            get { return imageUrl; }
            set {
                if (imageUrl == value)
                    return;

                ChangePropertyAndNotify (PropertyImageUrl, delegate {
                    imageUrl = value;
                });
            }
        }

        private string locale;
        public static readonly string PropertyLocale = GetPropertyName ((m) => m.Locale);

        [JsonProperty ("language")]
        public string Locale {
            get { return locale; }
            set {
                if (locale == value)
                    return;

                ChangePropertyAndNotify (PropertyLocale, delegate {
                    locale = value;
                });
            }
        }

        private string timezone;
        public static readonly string PropertyTimezone = GetPropertyName ((m) => m.Timezone);

        [JsonProperty ("timezone")]
        public string Timezone {
            get { return timezone; }
            set {
                if (timezone == value)
                    return;

                ChangePropertyAndNotify (PropertyTimezone, delegate {
                    timezone = value;
                });
            }
        }

        private bool sendProductEmails;
        public static readonly string PropertySendProductEmails = GetPropertyName ((m) => m.SendProductEmails);

        [JsonProperty ("send_product_emails")]
        public bool SendProductEmails {
            get { return sendProductEmails; }
            set {
                if (sendProductEmails == value)
                    return;

                ChangePropertyAndNotify (PropertySendProductEmails, delegate {
                    sendProductEmails = value;
                });
            }
        }

        private bool sendTimerNotifications;
        public static readonly string PropertySendTimerNotifications = GetPropertyName ((m) => m.SendTimerNotifications);

        [JsonProperty ("send_timer_notifications")]
        public bool SendTimerNotifications {
            get { return sendTimerNotifications; }
            set {
                if (sendTimerNotifications == value)
                    return;

                ChangePropertyAndNotify (PropertySendTimerNotifications, delegate {
                    sendTimerNotifications = value;
                });
            }
        }

        private bool sendWeeklyReport;
        public static readonly string PropertySendWeeklyReport = GetPropertyName ((m) => m.SendWeeklyReport);

        [JsonProperty ("send_weekly_report")]
        public bool SendWeeklyReport {
            get { return sendWeeklyReport; }
            set {
                if (sendWeeklyReport == value)
                    return;

                ChangePropertyAndNotify (PropertySendWeeklyReport, delegate {
                    sendWeeklyReport = value;
                });
            }
        }

        private TrackingMode trackingMode;
        public static readonly string PropertyTrackingMode = GetPropertyName ((m) => m.TrackingMode);

        public TrackingMode TrackingMode {
            get { return trackingMode; }
            set {
                if (trackingMode == value)
                    return;

                ChangePropertyAndNotify (PropertyTrackingMode, delegate {
                    trackingMode = value;
                });
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
            get { return createdWith; }
            set {
                if (createdWith == value)
                    return;

                ChangePropertyAndNotify (PropertyCreatedWith, delegate {
                    createdWith = value;
                });
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

        #endregion

    }
}
