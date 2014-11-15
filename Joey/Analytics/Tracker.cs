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

            // XXX: Workaround wrong signature for setNetSession in the component bindings:
            HitBuilderWorkaround.SetNewSession (builder);

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
                // XXX: Workaround wrong signature for setCustomDimension in the component bindings:
                HitBuilderWorkaround.SetCustomDimension (builder, kvp.Key, kvp.Value);
            }
            customDimensions.Clear ();

            tracker.Send (builder.Build ());
        }

        [Obsolete]
        private static class HitBuilderWorkaround
        {
            private static IntPtr HitBuilderClassHandle;
            private static IntPtr HitBuilderSetNewSessionId;
            private static IntPtr HitBuilderSetCustomDimensionId;

            private static IntPtr HitBuilderClassRef
            {
                get { return Android.Runtime.JNIEnv.FindClass ("com/google/android/gms/analytics/HitBuilders$HitBuilder", ref HitBuilderClassHandle); }
            }

            public static void SetNewSession (HitBuilders.HitBuilder builder)
            {
                if (HitBuilderSetNewSessionId == IntPtr.Zero) {
                    HitBuilderSetNewSessionId = Android.Runtime.JNIEnv.GetMethodID (HitBuilderClassRef, "setNewSession", "()Lcom/google/android/gms/analytics/HitBuilders$HitBuilder;");
                }
                Android.Runtime.JNIEnv.CallObjectMethod (builder.Handle, HitBuilderSetNewSessionId);
            }

            public static void SetCustomDimension (HitBuilders.HitBuilder builder, int index, string dimension)
            {
                if (HitBuilderSetCustomDimensionId == IntPtr.Zero) {
                    HitBuilderSetCustomDimensionId = Android.Runtime.JNIEnv.GetMethodID (HitBuilderClassRef, "setCustomDimension", "(ILjava/lang/String;)Lcom/google/android/gms/analytics/HitBuilders$HitBuilder;");
                }
                IntPtr dimensionPtr = Android.Runtime.JNIEnv.NewString (dimension);
                Android.Runtime.JNIEnv.CallObjectMethod (builder.Handle, HitBuilderSetCustomDimensionId, new Android.Runtime.JValue[] {
                    new Android.Runtime.JValue (index),
                    new Android.Runtime.JValue (dimensionPtr)
                });
                Android.Runtime.JNIEnv.DeleteLocalRef (dimensionPtr);
            }
        }
    }
}
