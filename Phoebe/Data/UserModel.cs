using System;
using Newtonsoft.Json;

namespace Toggl.Phoebe.Data
{
    public class UserModel : Model
    {
        public static long NextId {
            get { return Model.NextId<UserModel> (); }
        }

        private readonly int defaultWorkspaceRelationId;

        public UserModel ()
        {
            defaultWorkspaceRelationId = ForeignRelation (() => DefaultWorkspaceId, () => DefaultWorkspace);
        }

        #region Data

        private string name;

        [JsonProperty ("fullname")]
        public string Name {
            get { return name; }
            set {
                if (name == value)
                    return;

                ChangePropertyAndNotify (() => Name, delegate {
                    name = value;
                });
            }
        }

        private string email;

        [JsonProperty ("email")]
        public string Email {
            get { return email; }
            set {
                if (email == value)
                    return;

                ChangePropertyAndNotify (() => Email, delegate {
                    email = value;
                });
            }
        }

        private string password;

        [JsonProperty ("password", NullValueHandling = NullValueHandling.Include)]
        [SQLite.Ignore]
        public string Password {
            get { return password; }
            set {
                if (password == value)
                    return;

                ChangePropertyAndNotify (() => Password, delegate {
                    password = value;
                });
            }
        }

        private string apiToken;

        [JsonProperty ("api_token", NullValueHandling = NullValueHandling.Ignore)]
        [SQLite.Ignore]
        public string ApiToken {
            get { return apiToken; }
            set {
                if (apiToken == value)
                    return;

                ChangePropertyAndNotify (() => ApiToken, delegate {
                    apiToken = value;
                });
            }
        }

        private DayOfWeek startOfWeek;

        [JsonProperty ("beginning_of_week")]
        public DayOfWeek StartOfWeek {
            get { return startOfWeek; }
            set {
                if (startOfWeek == value)
                    return;

                ChangePropertyAndNotify (() => StartOfWeek, delegate {
                    startOfWeek = value;
                });
            }
        }

        private string dateFormat;

        [JsonProperty ("date_format")]
        public string DateFormat {
            get { return dateFormat; }
            set {
                if (dateFormat == value)
                    return;

                ChangePropertyAndNotify (() => DateFormat, delegate {
                    dateFormat = value;
                });
            }
        }

        private string timeFormat;

        [JsonProperty ("timeofday_format")]
        public string TimeFormat {
            get { return timeFormat; }
            set {
                if (timeFormat == value)
                    return;

                ChangePropertyAndNotify (() => TimeFormat, delegate {
                    timeFormat = value;
                });
            }
        }

        private string imageUrl;

        [JsonProperty ("image_url")]
        public string ImageUrl {
            get { return imageUrl; }
            set {
                if (imageUrl == value)
                    return;

                ChangePropertyAndNotify (() => ImageUrl, delegate {
                    imageUrl = value;
                });
            }
        }

        private string locale;

        [JsonProperty ("language")]
        public string Locale {
            get { return locale; }
            set {
                if (locale == value)
                    return;

                ChangePropertyAndNotify (() => Locale, delegate {
                    locale = value;
                });
            }
        }

        private string timezone;

        [JsonProperty ("timezone")]
        public string Timezone {
            get { return timezone; }
            set {
                if (timezone == value)
                    return;

                ChangePropertyAndNotify (() => Timezone, delegate {
                    timezone = value;
                });
            }
        }

        private bool sendProductEmails;

        [JsonProperty ("send_product_emails")]
        public bool SendProductEmails {
            get { return sendProductEmails; }
            set {
                if (sendProductEmails == value)
                    return;

                ChangePropertyAndNotify (() => SendProductEmails, delegate {
                    sendProductEmails = value;
                });
            }
        }

        private bool sendTimerNotifications;

        [JsonProperty ("send_timer_notifications")]
        public bool SendTimerNotifications {
            get { return sendTimerNotifications; }
            set {
                if (sendTimerNotifications == value)
                    return;

                ChangePropertyAndNotify (() => SendTimerNotifications, delegate {
                    sendTimerNotifications = value;
                });
            }
        }

        private bool sendWeeklyReport;

        [JsonProperty ("send_weekly_report")]
        public bool SendWeeklyReport {
            get { return sendWeeklyReport; }
            set {
                if (sendWeeklyReport == value)
                    return;

                ChangePropertyAndNotify (() => SendWeeklyReport, delegate {
                    sendWeeklyReport = value;
                });
            }
        }

        private TrackingMode trackingMode;

        public TrackingMode TrackingMode {
            get { return trackingMode; }
            set {
                if (trackingMode == value)
                    return;

                ChangePropertyAndNotify (() => TrackingMode, delegate {
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

        [JsonProperty ("created_with")]
        [SQLite.Ignore]
        public string CreatedWith {
            get { return createdWith; }
            set {
                if (createdWith == value)
                    return;

                ChangePropertyAndNotify (() => CreatedWith, delegate {
                    createdWith = value;
                });
            }
        }

        #endregion

        #region Relations

        public long? DefaultWorkspaceId {
            get { return GetForeignId (defaultWorkspaceRelationId); }
            set { SetForeignId (defaultWorkspaceRelationId, value); }
        }

        [DontDirty]
        [SQLite.Ignore]
        public WorkspaceModel DefaultWorkspace {
            get { return GetForeignModel<WorkspaceModel> (defaultWorkspaceRelationId); }
            set { SetForeignModel (defaultWorkspaceRelationId, value); }
        }

        public IModelQuery<TimeEntryModel> TimeEntries {
            get { return Model.Query<TimeEntryModel> ((m) => m.UserId == Id); }
        }

        #endregion

    }
}
