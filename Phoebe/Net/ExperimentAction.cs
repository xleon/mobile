using System;
using System.Threading.Tasks;
using Toggl.Phoebe.Data.Json;
using Toggl.Phoebe.Logging;
using XPlatUtils;

namespace Toggl.Phoebe.Net
{
    public class ExperimentAction
    {
        private const string Tag = "ExperimentAction";

        public int ExperimentId { get; set; }

        public string ActionKey { get; set; }

        public string ActionValue { get; set; }

        public async Task<bool> Send ()
        {
            var client = ServiceContainer.Resolve<ITogglClient> ();

            try {
                var json = MakeActionJson();
                await client.CreateExperimentAction (json).ConfigureAwait (false);

            } catch (Exception ex) {

                var log = ServiceContainer.Resolve<ILogger> ();
                if (ex.IsNetworkFailure() ) {
                    log.Info (Tag, ex, "Failed to send obm action.");
                } else {
                    log.Warning (Tag, ex, "Failed to send obm action.");
                }
                return false;
            }
            return true;
        }

        private ActionJson MakeActionJson ()
        {
            return new ActionJson() {
                ExperimentId = ExperimentId,
                Key = ActionKey,
                Value = ActionValue
            };
        }
    }
}

