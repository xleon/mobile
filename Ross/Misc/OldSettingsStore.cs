using System;
using System.Linq.Expressions;
using Foundation;
using Toggl.Phoebe;
using Toggl.Phoebe.Misc;

namespace Toggl.Ross
{
    public class OldSettingsStore : IOldSettingsStore
    {
        private const string PhoebeUserIdKey = "phoebeUserId";
        private const string PhoebeApiTokenKey = "phoebeApiToken";
        private const string PhoebeSyncLastRunKey = "phoebeSyncLastRun";
        private const string PhoebeUseDefaultTagKey = "phoebeUseDefaultTag";
        private const string PhoebeLastAppVersionKey = "phoebeLastAppVersion";
        private const string PhoebeExperimentIdKey = "phoebeExperimentId";
        private const string PhoebeLastReportZoomKey = "lastReportZoomKey";
        private const string PhoebeGroupedEntriesKey = "groupedEntriesKey";
        private const string RossChooseProjectForNewKey = "rossChooseProjectForNew";
        private const string RossReadDurOnlyNoticeKey = "rossReadDurOnlyNotice";
        private const string RossIgnoreSyncErrorsUntilKey = "rossIgnoreSyncErrorsUntil";
        private const string PhoebeProjectSortKey = "projectSortKey";
        private const string PhoebeIsStagingKey = "isStagingKey";
        private const string PhoebeShowWelcomeKey = "showWelcomeKey";
        private const string PhoebeIsOfflineKey = "isOfflineKey";

        private static string GetPropertyName<T>(Expression<Func<OldSettingsStore, T>> expr)
        {
            return expr.ToPropertyName();
        }

        protected Guid? GetGuid(string key)
        {
            var val = (string)(NSString)NSUserDefaults.StandardUserDefaults[key];
            if (string.IsNullOrEmpty(val))
            {
                return null;
            }
            return Guid.Parse(val);
        }

        protected void SetGuid(string key, Guid? value)
        {
            if (value != null)
            {
                NSUserDefaults.StandardUserDefaults[key] = (NSString)value.Value.ToString();
            }
            else
            {
                NSUserDefaults.StandardUserDefaults.RemoveObject(key);
            }
            NSUserDefaults.StandardUserDefaults.Synchronize();
        }

        protected string GetString(string key)
        {
            return (string)(NSString)NSUserDefaults.StandardUserDefaults[key];
        }

        protected void SetString(string key, string value)
        {
            if (value != null)
            {
                NSUserDefaults.StandardUserDefaults[key] = (NSString)value;
            }
            else
            {
                NSUserDefaults.StandardUserDefaults.RemoveObject(key);
            }
            NSUserDefaults.StandardUserDefaults.Synchronize();
        }

        protected int? GetInt(string key)
        {
            var raw = NSUserDefaults.StandardUserDefaults[key];
            if (raw == null)
            {
                return null;
            }
            return (int)(NSNumber)raw;
        }

        protected void SetInt(string key, int? value)
        {
            if (value != null)
            {
                NSUserDefaults.StandardUserDefaults[key] = (NSNumber)value.Value;
            }
            else
            {
                NSUserDefaults.StandardUserDefaults.RemoveObject(key);
            }
            NSUserDefaults.StandardUserDefaults.Synchronize();
        }

        protected DateTime? GetDateTime(string key)
        {
            var raw = NSUserDefaults.StandardUserDefaults[key];
            if (raw == null)
            {
                return null;
            }
            return DateTime.FromBinary((long)(NSNumber)raw);
        }

        protected void SetDateTime(string key, DateTime? value)
        {
            if (value != null)
            {
                NSUserDefaults.StandardUserDefaults[key] = (NSNumber)value.Value.ToBinary();
            }
            else
            {
                NSUserDefaults.StandardUserDefaults.RemoveObject(key);
            }
            NSUserDefaults.StandardUserDefaults.Synchronize();
        }

        public static readonly string PropertyUserId = GetPropertyName(s => s.UserId);

        public Guid? UserId
        {
            get { return GetGuid(PhoebeUserIdKey); }
        }

        public static readonly string PropertyApiToken = GetPropertyName(s => s.ApiToken);

        public string ApiToken
        {
            get { return GetString(PhoebeApiTokenKey); }
        }

        public static readonly string PropertySyncLastRun = GetPropertyName(s => s.SyncLastRun);

        public DateTime? SyncLastRun
        {
            get { return GetDateTime(PhoebeSyncLastRunKey); }
        }

        public static readonly string PropertyUseDefaultTag = GetPropertyName(s => s.UseDefaultTag);

        public bool UseDefaultTag
        {
            get { return (GetInt(PhoebeUseDefaultTagKey) ?? 1) == 1; }
        }

        public bool GroupedTimeEntries
        {
            get { return GetInt(PhoebeGroupedEntriesKey) == 1; }
        }

        public static readonly string PropertyLastAppVersion = GetPropertyName(s => s.LastAppVersion);

        public string LastAppVersion
        {
            get { return GetString(PhoebeLastAppVersionKey); }
        }

        public static readonly string PropertyExperimentId = GetPropertyName(s => s.ExperimentId);

        public string ExperimentId
        {
            get { return GetString(PhoebeExperimentIdKey); }
        }

        public static readonly string PropertyChooseProjectForNew = GetPropertyName(s => s.ChooseProjectForNew);

        public bool ChooseProjectForNew
        {
            get { return GetInt(RossChooseProjectForNewKey) == 1; }
        }

        public static readonly string PropertyReadDurOnlyNotice = GetPropertyName(s => s.ReadDurOnlyNotice);

        public bool ReadDurOnlyNotice
        {
            get { return GetInt(RossReadDurOnlyNoticeKey) == 1; }
        }

        public static readonly string PropertyIgnoreSyncErrorsUntil = GetPropertyName(s => s.IgnoreSyncErrorsUntil);

        public DateTime? IgnoreSyncErrorsUntil
        {
            get { return GetDateTime(RossIgnoreSyncErrorsUntilKey); }
        }

        public static readonly string PropertyLastReportZoomViewed = GetPropertyName(s => s.LastReportZoomViewed);

        public int? LastReportZoomViewed
        {
            get { return GetInt(PhoebeLastReportZoomKey); }
        }

        public static readonly string PropertyProjectSort = GetPropertyName(s => s.SortProjectsBy);

        public string SortProjectsBy
        {
            get { return (GetString(PhoebeProjectSortKey)); }
        }

        public static readonly string PropertyIsStagingMode = GetPropertyName(s => s.IsStagingMode);

        public bool IsStagingMode
        {
            get { return GetInt(PhoebeIsStagingKey) == 1; }
        }

        public static readonly string PropertyShowWelcome = GetPropertyName(s => s.ShowWelcome);

        public bool ShowWelcome
        {
            get { return (GetInt(PhoebeShowWelcomeKey) ?? 1) == 1; }
        }

        public bool OfflineMode
        {
            get
            {
                return false;
            }
        }

        public bool RossReadDurOnlyNotice
        {
            get
            {
                return ReadDurOnlyNotice;
            }
        }

        #region Android dummy properties
        public int ReportsCurrentItem
        {
            get
            {
                return 0;
            }
        }

        public string GcmRegistrationId
        {
            get
            {
                return string.Empty;
            }
        }

        public string GcmAppVersion
        {
            get
            {
                return string.Empty;
            }
        }

        public bool IdleNotification
        {
            get
            {
                return false;
            }
        }

        public bool ShowNotification
        {
            get
            {
                return false;
            }
        }
        #endregion
    }
}