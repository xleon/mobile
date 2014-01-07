using System;

namespace Toggl.Phoebe.Models
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

        private DayOfWeek startOfWeek;

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

        #endregion

    }
}
