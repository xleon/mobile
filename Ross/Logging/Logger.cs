using System;
using Toggl.Phoebe;
using Toggl.Phoebe.Logging;
using XPlatUtils;
using Toggl.Ross.Data;

namespace Toggl.Ross.Logging
{
    public class Logger : BaseLogger
    {
        public Logger () : base ()
        {
        }

        public Logger (LogLevel threshold) : base (threshold)
        {
        }

        protected override void AddExtraMetadata (Bugsnag.Data.Metadata md)
        {
            var settings = ServiceContainer.Resolve<SettingsStore> ();
            md.AddToTab ("State", "Experiment", settings.ExperimentId);
            md.AddToTab ("State", "Read duration only notice", settings.ReadDurOnlyNotice ? "Yes" : "No");

            md.AddToTab ("Settings", "Show projects for new", settings.ChooseProjectForNew ? "Yes" : "No");
            md.AddToTab ("Settings", "Add default tag", settings.UseDefaultTag ? "Yes" : "No");
        }
    }
}
