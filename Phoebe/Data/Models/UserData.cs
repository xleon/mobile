using System;
using SQLite.Net.Attributes;

namespace Toggl.Phoebe.Data.Models
{
    public enum TrackingMode {
        Continue,
        StartNew
    }

    public interface IUserData : ICommonData
    {
        string Name { get; }
        string Email { get; }
        DayOfWeek StartOfWeek { get; }
        string DateFormat { get; }
        string TimeFormat { get; }
        string ImageUrl { get; }
        string Locale { get; }
        string Timezone { get; }
        bool SendProductEmails { get; }
        bool SendTimerNotifications { get; }
        bool SendWeeklyReport { get; }
        TrackingMode TrackingMode { get; }
        DurationFormat DurationFormat { get; }
        bool ExperimentIncluded { get; }
        int ExperimentNumber { get; }
        long DefaultWorkspaceRemoteId { get; }
        Guid DefaultWorkspaceId { get; }
        string GoogleAccessToken { get; }
        string ApiToken { get; }
        IUserData With (Action<UserData> transform);
    }

    [Table ("UserModel")]
    public class UserData : CommonData, IUserData
    {
        public UserData ()
        {
        }

        protected UserData (UserData other) : base (other)
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
            DefaultWorkspaceRemoteId = other.DefaultWorkspaceRemoteId;
            DurationFormat = other.DurationFormat;
            ExperimentIncluded = other.ExperimentIncluded;
            ExperimentNumber = other.ExperimentNumber;
            GoogleAccessToken = other.GoogleAccessToken;
            ApiToken = other.ApiToken;
        }

        public override object Clone ()
        {
            return new UserData (this);
        }

        public IUserData With (Action<UserData> transform)
        {
            return base.With (transform);
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

        public DurationFormat DurationFormat { get; set; }

        public bool ExperimentIncluded { get; set; }

        public int ExperimentNumber { get; set; }

        public long DefaultWorkspaceRemoteId { get; set; }

        public Guid DefaultWorkspaceId { get; set; }

        public string GoogleAccessToken { get; set; }

        public string ApiToken { get; set; }
    }
}
