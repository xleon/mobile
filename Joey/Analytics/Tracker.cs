using System;
using System.Collections.Generic;
using Android.Content;
using Android.Gms.Analytics;
using Toggl.Phoebe;
using Toggl.Phoebe.Analytics;

namespace Toggl.Joey.Analytics
{
    public class Tracker : BaseTracker
    {
        private readonly Dictionary<int, string> customDimensions = new Dictionary<int, string>();
        private readonly Android.Gms.Analytics.Tracker tracker;

        public Tracker (Context ctx)
        {
            var ga = GoogleAnalytics.GetInstance (ctx);
            #if DEBUG
            ga.SetDryRun (true);
            #endif

            tracker = ga.NewTracker (Build.GoogleAnalyticsId);
            tracker.SetSessionTimeout ((long)TimeSpan.FromMinutes (5).TotalSeconds);
            tracker.EnableAutoActivityTracking (false);
            tracker.EnableExceptionReporting (false);
        }

        protected override void StartNewSession ()
        {
            var builder = new HitBuilders.ScreenViewBuilder ();
            builder.SetNewSession ();
            SendHit (builder);
        }

        protected override void SendTiming (long elapsedMilliseconds, string category, string variable, string label)
        {
            SendHit (new HitBuilders.TimingBuilder ()
                     .SetValue (elapsedMilliseconds)
                     .SetCategory (category)
                     .SetVariable (variable)
                     .SetLabel (label));
        }

        protected override void SetCustomDimension (int idx, string value)
        {
            customDimensions [idx] = value;
        }

        public override string CurrentScreen
        {
            set {
                tracker.SetScreenName (value);
                SendHit (new HitBuilders.ScreenViewBuilder ());
            }
        }

        private void SendHit (HitBuilders.HitBuilder builder)
        {
            // Inject custom dimensions, if any have been set:
            foreach (var kvp in customDimensions) {
                builder.SetCustomDimension (kvp.Key, kvp.Value);
            }
            customDimensions.Clear ();

            tracker.Send (builder.Build ());
        }
    }
}
