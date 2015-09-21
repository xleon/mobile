using System.Collections.Generic;
using Google.Analytics;
using Toggl.Phoebe.Analytics;
using Xamarin;

namespace Toggl.Ross.Analytics
{
    public class Tracker : BaseTracker
    {
        private readonly Dictionary<int, string> customDimensions = new Dictionary<int, string>();

        public Tracker ()
        {
            #if DEBUG
            Gai.SharedInstance.DryRun = true;
            #endif
        }

        protected override void StartNewSession ()
        {
            var builder = DictionaryBuilder.CreateScreenView ();
            builder.Set ("start", GaiConstants.SessionControl);
            SendHit (builder);
        }

        protected override void SendTiming (long elapsedMilliseconds, string category, string variable, string label = null)
        {
            SendHit (DictionaryBuilder.CreateTiming (category, elapsedMilliseconds, variable, label));
            if (Insights.IsInitialized) {
                Insights.Track ("AppStartupTime", new Dictionary<string, string>  {{"ElapsedTime", elapsedMilliseconds + "ms"}});
            }
        }

        protected override void SendEvent (string category, string action, string label = null, long value = 0)
        {
            SendHit (DictionaryBuilder.CreateEvent (category, action, label, value));
        }

        protected override void SetCustomDimension (int idx, string value)
        {
            customDimensions [idx] = value;
        }

        public override string CurrentScreen
        {
            set {
                Gai.SharedInstance.DefaultTracker.Set (GaiConstants.ScreenName, value);
                SendHit (DictionaryBuilder.CreateScreenView ());
            }
        }

        private void SendHit (DictionaryBuilder builder)
        {
            // Inject custom dimensions, if any have been set:
            foreach (var kvp in customDimensions) {
                builder.Set (kvp.Value, Fields.CustomDimension ((uint)kvp.Key));
            }
            customDimensions.Clear ();

            Gai.SharedInstance.DefaultTracker.Send (builder.Build ());
        }
    }
}
