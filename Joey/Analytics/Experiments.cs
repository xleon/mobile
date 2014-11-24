using System;
using Toggl.Phoebe.Analytics;
using XPlatUtils;
using Toggl.Joey.Data;

namespace Toggl.Joey.Analytics
{
    public static class Experiments
    {
        public static Experiment ShowIdleNotification = new Experiment()
        {
            // Show timer not running notification by default
            Id = "show_idle_notif_default",
            FreshInstallOnly = true,
            SetUp = () => ServiceContainer.Resolve<SettingsStore>().IdleNotification = true,
        };

        public static Experiment SkipProjectChoice = new Experiment()
        {
            // Disable the project choice list after starting a new time entry by default
            Id = "skip_project_choice_default",
            FreshInstallOnly = true,
            SetUp = () => ServiceContainer.Resolve<SettingsStore>().ChooseProjectForNew = false,
        };
    }
}
