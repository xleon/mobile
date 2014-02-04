using System;

namespace Toggl.Phoebe
{
    public static class Build
    {
        #warning Please fill in build settings and make git assume this file is unchanged.
        #region Phoebe build config

        public static readonly Uri ApiUrl = new Uri ("https://toggl.com/api/");

        #endregion

        #region Joey build configuration

        #if __ANDROID__
        public static readonly string AppIdentifier = "TogglJoey";
        public static readonly string GcmSenderId = "";
        #endif
        #endregion

        #region Ross build configuration

        #if __IOS__
        public static readonly string AppIdentifier = "TogglRoss";
        #endif
        #endregion

    }
}
