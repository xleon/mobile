using System;

namespace Toggl.Phoebe
{
    public static class Build
    {
#warning Please fill in build settings and make git assume this file is unchanged.
        #region Phoebe build config

        public static readonly Uri ApiUrl = new Uri ("https://toggl.com/api/");
        public static readonly Uri ReportsApiUrl = new Uri ("https://toggl.com/reports/api/");
        public static readonly Uri PrivacyPolicyUrl = new Uri ("https://toggl.com/privacy");
        public static readonly Uri TermsOfServiceUrl = new Uri ("https://toggl.com/terms");

        #endregion

        #region Joey build configuration

        #if __ANDROID__
        public static readonly string AppIdentifier = "TogglJoey";
        public static readonly string GcmSenderId = "";
        public static readonly string BugsnagApiKey = "";
        public static readonly string GooglePlayUrl = "https://play.google.com/store/apps/details?id=com.toggl.timer";
        public static readonly string GoogleAnalyticsId = "";
        #endif
        #endregion

        #region Ross build configuration

        #if __IOS__
        public static readonly string AppStoreUrl = "itms-apps://itunes.com/apps/toggl/toggltimer";
        public static readonly string AppIdentifier = "TogglRoss";
        public static readonly string BugsnagApiKey = "";
        public static readonly string GoogleAnalyticsId = "";
        public static readonly string GoogleOAuthClientId = "";
        #endif
        #endregion
    }
}