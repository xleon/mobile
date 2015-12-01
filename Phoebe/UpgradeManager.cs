using System;
using Toggl.Phoebe.Analytics;
using Toggl.Phoebe.Data;
using Toggl.Phoebe.Logging;
using XPlatUtils;
using System.Threading.Tasks;

namespace Toggl.Phoebe
{
    public sealed class UpgradeManger
    {
        private const string Tag = "UpgradeManager";

        public async Task TryUpgrade ()
        {
            var settingsStore = ServiceContainer.Resolve<ISettingsStore> ();
            var platformInfo = ServiceContainer.Resolve<IPlatformUtils> ();
            var log = ServiceContainer.Resolve<ILogger> ();

            var oldVersion = settingsStore.LastAppVersion;
            var newVersion = platformInfo.AppVersion;
            var isFreshInstall = oldVersion == null;

            // User hasn't upgraded, do nothing.
            if (oldVersion == newVersion) {
                return;
            }

            log.Info (Tag, "App has been upgraded from '{0}' to '{1}'", oldVersion, newVersion);

            await UpgradeAlways ();
            ChooseExperiment (isFreshInstall);

            settingsStore.LastAppVersion = newVersion;
        }

        private async Task UpgradeAlways ()
        {
            var settingsStore = ServiceContainer.Resolve<ISettingsStore> ();
            var dataStore = ServiceContainer.Resolve<IDataStore> ();

            // Mark all non-dirty remote items as never having been modified:
            await dataStore.ResetAllModificationTimes ();

            // Reset sync last run
            settingsStore.SyncLastRun = null;
        }

        private void ChooseExperiment (bool isFreshInstall)
        {
            var experimentManager = ServiceContainer.Resolve<ExperimentManager> ();
            experimentManager.NextExperiment (isFreshInstall);
        }
    }
}
