using System;
using System.Linq.Expressions;
using Android.Content;
using Toggl.Phoebe;
using Toggl.Phoebe.Misc;

namespace Toggl.Joey.Data
{
    /// <summary>
    /// Settings store for storing Android runtime settings. It also stores settings for Phoebe, but the
    /// <see cref="IOldSettingsStore"/> interface is implemented explicitly.
    /// </summary>
    public class OldSettingsStore : IOldSettingsStore
    {
        private const string PreferenceName = "togglSettings";
        private const string PhoebeUserIdKey = "phoebeUserId";
        private const string PhoebeApiTokenKey = "phoebeApiToken";
        private const string PhoebeSyncLastRunKey = "phoebeSyncLastRun";
        private const string PhoebeUseDefaultTagKey = "phoebeUseDefaultTag";
        private const string PhoebeLastAppVersionKey = "phoebeLastAppVersion";
        private const string PhoebeExperimentIdKey = "phoebeExperimentId";
        private const string PhoebeLastReportZoomKey = "lastReportZoomKey";
        private const string PhoebeGroupedEntriesKey = "groupedEntriesKey";
        private const string JoeyGcmRegistrationIdKey = "joeyGcmRegistrationId";
        private const string IdleNotificationKey = "idleNotification";
        private const string ChooseProjectForNewKey = "chooseProjectForNewKey";
        private const string ReportsCurrentItemKey = "reportsCurrentItem";
        private const string JoeyShowNotificationKey = "disableNotificationKey";
        private const string PhoebeProjectSortKey = "projectSortKey";
        private const string PhoebeIsStagingKey = "isStagingKey";
        private const string PhoebeShowWelcomeKey = "showWelcomeKey";

        private static string GetPropertyName<T> (Expression<Func<OldSettingsStore, T>> expr)
        {
            return expr.ToPropertyName();
        }

        private readonly ISharedPreferences prefs;

        public OldSettingsStore(Context ctx)
        {
            prefs = ctx.GetSharedPreferences(PreferenceName, FileCreationMode.Private);
        }

        protected Guid? GetGuid(string key)
        {
            var val = prefs.GetString(key, null);
            if (string.IsNullOrEmpty(val))
            {
                return null;
            }
            return Guid.Parse(val);
        }

        protected string GetString(string key)
        {
            return prefs.GetString(key, null);
        }

        protected int? GetInt(string key)
        {
            if (!prefs.Contains(key))
            {
                return null;
            }
            else
            {
                return prefs.GetInt(key, 0);
            }
        }

        protected DateTime? GetDateTime(string key)
        {
            if (!prefs.Contains(key))
            {
                return null;
            }
            else
            {
                return DateTime.FromBinary(prefs.GetLong(key, 0)).ToUtc();
            }
        }

        public static readonly string PropertyGcmRegistrationId = GetPropertyName(s => s.GcmRegistrationId);

        public string GcmRegistrationId
        {
            get { return GetString(JoeyGcmRegistrationIdKey); }
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

        public static readonly string PropertyExperimentId = GetPropertyName(s => s.ExperimentId);

        public string ExperimentId
        {
            get { return GetString(PhoebeExperimentIdKey); }
        }

        public static readonly string PropertyIdleNotification = GetPropertyName(s => s.IdleNotification);

        public bool IdleNotification
        {
            get { return GetInt(IdleNotificationKey) == 1; }
        }

        public static readonly string PropertyChooseProjectForNew = GetPropertyName(s => s.ChooseProjectForNew);

        public bool ChooseProjectForNew
        {
            get { return GetInt(ChooseProjectForNewKey) == 1; }
        }

        public static readonly string PropertyUseDefaultTag = GetPropertyName(s => s.UseDefaultTag);

        public bool UseDefaultTag
        {
            get { return (GetInt(PhoebeUseDefaultTagKey) ?? 1) == 1; }
        }

        public static readonly string PropertyLastAppVersion = GetPropertyName(s => s.LastAppVersion);

        public string LastAppVersion
        {
            get { return GetString(PhoebeLastAppVersionKey); }
        }

        public static readonly string PropertyLastReportZoomViewed = GetPropertyName(s => s.LastReportZoomViewed);

        public int? LastReportZoomViewed
        {
            get { return GetInt(PhoebeLastReportZoomKey); }
        }

        public static readonly string PropertyReportsCurrentItem = GetPropertyName(s => s.ReportsCurrentItem);

        public int ReportsCurrentItem
        {
            get { return GetInt(ReportsCurrentItemKey).HasValue ? GetInt(ReportsCurrentItemKey).Value : 0; }
        }

        public static readonly string PropertyGroupedTimeEntries = GetPropertyName(s => s.GroupedTimeEntries);

        public bool GroupedTimeEntries
        {
            get { return GetInt(PhoebeGroupedEntriesKey) == 1; }
        }

        public static readonly string PropertyShowNotification = GetPropertyName(s => s.ShowNotification);

        public bool ShowNotification
        {
            get { return (GetInt(JoeyShowNotificationKey) ?? 1) == 1; }
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

        #region Ross dummy properties
        public bool RossReadDurOnlyNotice
        {
            get
            {
                return false;
            }
        }
        #endregion

    }
}
