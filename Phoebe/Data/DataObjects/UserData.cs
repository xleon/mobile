using System;
using SQLite;

namespace Toggl.Phoebe.Data.DataObjects
{
    [Table ("User")]
    public class UserData : CommonData
    {
        public UserData ()
        {
        }

        public UserData (UserData other) : base (other)
        {
            Name = other.Name;
            Email = other.Email;
            StartOfWeek = other.StartOfWeek;
            DateFormat = other.DateFormat;
            TimeFormat = other.TimeFormat;
            ImageUrl = other.ImageUrl;
            Locale = other.Locale;
            Timezone = other.Timezone;
            SendProductEmails = other.SendProductEmails;
            SendTimerNotifications = other.SendTimerNotifications;
            SendWeeklyReport = other.SendWeeklyReport;
            TrackingMode = other.TrackingMode;
            DefaultWorkspaceId = other.DefaultWorkspaceId;
        }

        public string Name { get; set; }

        public string Email { get; set; }

        public DayOfWeek StartOfWeek { get; set; }

        public string DateFormat { get; set; }

        public string TimeFormat { get; set; }

        public string ImageUrl { get; set; }

        public string Locale { get; set; }

        public string Timezone { get; set; }

        public bool SendProductEmails { get; set; }

        public bool SendTimerNotifications { get; set; }

        public bool SendWeeklyReport { get; set; }

        public TrackingMode TrackingMode { get; set; }

        public Guid DefaultWorkspaceId { get; set; }
    }
}
