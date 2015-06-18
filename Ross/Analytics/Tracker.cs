
using System.Collections.Generic;
using GoogleAnalytics.iOS;
using Toggl.Phoebe;
using Toggl.Phoebe.Analytics;
using Xamarin;

namespace Toggl.Ross.Analytics
{
    public class Tracker : BaseTracker
    {
        private readonly Dictionary<int, string> customDimensions = new Dictionary<int, string>();
        private readonly IGAITracker tracker;

        public Tracker ()
        {
            #if DEBUG
            GAI.SharedInstance.DryRun = true;
            #endif

            #if DEBUG
            Insights.Initialize (Insights.DebugModeKey);
            #else
            Insights.Initialize (Build.XamInsightsApiKey);
            #endif

            tracker = GAI.SharedInstance.GetTracker (Build.GoogleAnalyticsId);
        }

        protected override void StartNewSession ()
        {
            var builder = GAIDictionaryBuilder.CreateScreenView ();
            builder.Set ("start", GAIConstants.SessionControl);
            SendHit (builder);
        }

        protected override void SendTiming (long elapsedMilliseconds, string category, string variable, string label)
        {
            SendHit (GAIDictionaryBuilder.CreateTiming (category, elapsedMilliseconds, variable, label));
            if (Insights.IsInitialized) {
                Insights.Track("AppStartupTime", new Dictionary<string, string>  {{"ElapsedTime", elapsedMilliseconds.ToString() + "ms"}});
            }
        }

        protected override void SendEvent (string category, string action, string label, long value)
        {
            SendHit (GAIDictionaryBuilder.CreateEvent (category, action, label, value));
        }

        protected override void SetCustomDimension (int idx, string value)
        {
            customDimensions [idx] = value;
        }

        public override string CurrentScreen
        {
            set {
                tracker.Set (GAIConstants.ScreenName, value);
                SendHit (GAIDictionaryBuilder.CreateScreenView ());
            }
        }

        private void SendHit (GAIDictionaryBuilder builder)
        {
            // Inject custom dimensions, if any have been set:
            foreach (var kvp in customDimensions) {
                builder.Set (kvp.Value, GAIFields.CustomDimension ((uint)kvp.Key));
            }
            customDimensions.Clear ();

            tracker.Send (builder.Build ());
        }
    }
}
