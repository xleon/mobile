using System;
using Toggl.Phoebe;
using Toggl.Phoebe.Logging;

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
            // TODO: Add user settings
        }
    }
}
