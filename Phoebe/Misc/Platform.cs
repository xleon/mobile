using System;
using XPlatUtils;

namespace Toggl.Phoebe
{
    internal static class Platform
    {
        public static string AppIdentifier
        {
            get { return ServiceContainer.Resolve<IPlatformUtils> ().AppIdentifier; }
        }

        public static string AppVersion
        {
            get { return ServiceContainer.Resolve<IPlatformUtils> ().AppVersion; }
        }

        public static string DefaultCreatedWith
        {
            get { return String.Format ("{0}/{1}", AppIdentifier, AppVersion); }
        }
    }
}
