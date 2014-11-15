using System;
using Toggl.Phoebe.Net;
using XPlatUtils;

namespace Toggl.Phoebe.Analytics
{
    public abstract class BaseTracker : ITracker, IDisposable
    {
        private Subscription<AuthChangedMessage> subscriptionAuthChanged;

        public BaseTracker ()
        {
            PlanDimensionIndex = 1;
            ExperimentDimensionIndex = 2;

            var bus = ServiceContainer.Resolve<MessageBus> ();
            subscriptionAuthChanged = bus.Subscribe<AuthChangedMessage> (OnAuthChanged);
        }

        ~BaseTracker ()
        {
            Dispose (false);
        }

        public void Dispose()
        {
            Dispose (true);
            GC.SuppressFinalize (this);
        }

        protected virtual void Dispose (bool disposing)
        {
            if (disposing) {
                var bus = ServiceContainer.Resolve<MessageBus> ();

                if (subscriptionAuthChanged != null) {
                    bus.Unsubscribe (subscriptionAuthChanged);
                    subscriptionAuthChanged = null;
                }
            }
        }

        public int ExperimentDimensionIndex { get; set; }
        public int PlanDimensionIndex { get; set; }

        public void SendTiming (TimedEvent timedEvent, TimeSpan duration, string label = null)
        {
            string category;
            string variable;

            switch (timedEvent) {
            case TimedEvent.AppInit:
                category = "App";
                variable = "Init";
                break;
            case TimedEvent.AppScreenDisplay:
                category = "App";
                variable = "ScreenDisplay";
                break;
            case TimedEvent.SyncDuration:
                category = "Sync";
                variable = "Duration";
                break;
            default:
                throw new ArgumentException ("Unsupported value.", "timedEvent");
            }

            SendTiming ((long)duration.TotalMilliseconds, category, variable, label);
        }

        public string RunningExperiment
        {
            set { SetCustomDimension (ExperimentDimensionIndex, value); }
        }

        public PlanType UserPlan
        {
            set {
                string planName;
                switch (value) {
                case PlanType.None:
                    planName = null;
                    break;
                case PlanType.Free:
                    planName = "Free";
                    break;
                case PlanType.Pro:
                    planName = "Pro";
                    break;
                default:
                    throw new ArgumentException ("Unsupported value.", "value");
                }

                SetCustomDimension (PlanDimensionIndex, planName);
            }
        }

        public abstract string CurrentScreen { set; }

        protected abstract void StartNewSession ();
        protected abstract void SendTiming (long elapsedMilliseconds, string category, string variable, string label);
        protected abstract void SetCustomDimension (int idx, string value);

        private void OnAuthChanged (AuthChangedMessage msg)
        {
            // Start a new session whenever the user changes, exception being signup where the user just created an account
            if (msg.Reason != AuthChangeReason.Signup) {
                StartNewSession ();
            }
        }
    }
}
