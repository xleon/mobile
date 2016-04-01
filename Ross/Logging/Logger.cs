using Toggl.Phoebe;
using Toggl.Phoebe.Logging;

namespace Toggl.Ross.Logging
{
    public class Logger : BaseLogger
    {
        public Logger ()
        {
        }

        public Logger (LogLevel threshold) : base (threshold)
        {
        }

        protected override void AddExtraMetadata (Metadata md)
        {
            // TODO: RX Better way to do that!

            var settings = Phoebe.Reactive.StoreManager.Singleton.AppState.Settings;
            md.AddToTab ("State", "Experiment", OBMExperimentManager.ExperimentNumber);
            md.AddToTab ("State", "Push registered", string.IsNullOrWhiteSpace (settings.GcmRegistrationId) ? "No" : "Yes");

            md.AddToTab ("Settings", "Show projects for new", settings.ChooseProjectForNew ? "Yes" : "No");
            md.AddToTab ("Settings", "Idle notifications", settings.IdleNotification ? "Yes" : "No");
            md.AddToTab ("Settings", "Add default tag", settings.UseDefaultTag ? "Yes" : "No");
            md.AddToTab ("Settings", "Is Grouped", settings.GroupedEntries ? "Yes" : "No");
        }
    }
}
