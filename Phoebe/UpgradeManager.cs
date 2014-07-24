using System;
using Toggl.Phoebe.Data;
using XPlatUtils;

namespace Toggl.Phoebe
{
    public class UpgradeManger
    {
        public void TryUpgrade ()
        {
            var settingsStore = ServiceContainer.Resolve<ISettingsStore> ();
            var platformInfo = ServiceContainer.Resolve<IPlatformInfo> ();

            var oldVersion = settingsStore.LastAppVersion;
            var newVersion = platformInfo.AppVersion;

            // User hasn't upgraded, do nothing.
            if (oldVersion == newVersion)
                return;

            UpgradeAlaways ();

            settingsStore.LastAppVersion = newVersion;
        }

        private void UpgradeAlaways ()
        {
            var settingsStore = ServiceContainer.Resolve<ISettingsStore> ();
            var dataStore = ServiceContainer.Resolve<IDataStore> ();

            // Mark all non-dirty remote items as never having been modified:
            dataStore.ResetAllModificationTimes ().Wait ();

            // Reset sync last run
            settingsStore.SyncLastRun = null;
        }
    }
}
