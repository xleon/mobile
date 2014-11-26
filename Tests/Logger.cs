using System;
using Toggl.Phoebe.Logging;

namespace Toggl.Phoebe.Tests
{
    public class Logger : BaseLogger
    {
        public Logger ()
        {
        }

        protected override void AddExtraMetadata (Bugsnag.Data.Metadata md)
        {
            throw new NotImplementedException ();
        }
    }
}
