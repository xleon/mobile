using Toggl.Phoebe.Analytics;
using XPlatUtils;
using Toggl.Ross.Data;

namespace Toggl.Ross.Analytics
{
    public static class Experiments
    {
        public static Experiment SkipProjectChoice = new Experiment()
        {
            // Disable the project choice list after starting a new time entry by default
            Id = "skip_project_choice_default",
            FreshInstallOnly = true,
            SetUp = () => ServiceContainer.Resolve<SettingsStore>().ChooseProjectForNew = false,
        };
    }
}

