using System;
using Toggl.Phoebe.Data;
using Toggl.Phoebe.Data.DataObjects;
using Toggl.Phoebe.Net;
using XPlatUtils;

namespace Toggl.Phoebe.Analytics
{
    public abstract class BaseTracker : ITracker, IDisposable
    {
        private Subscription<AuthChangedMessage> subscriptionAuthChanged;
        private Subscription<SyncFinishedMessage> subscriptionSyncFinished;
        private Subscription<ExperimentChangedMessage> subscriptionExperimentChanged;
        private Plan userPlan;

        public BaseTracker ()
        {
            var bus = ServiceContainer.Resolve<MessageBus> ();
            subscriptionAuthChanged = bus.Subscribe<AuthChangedMessage> (OnAuthChanged);
            subscriptionSyncFinished = bus.Subscribe<SyncFinishedMessage> (OnSyncFinished);
            subscriptionExperimentChanged = bus.Subscribe<ExperimentChangedMessage> (OnExperimentChanged);

            var experiment = ServiceContainer.Resolve<ExperimentManager> ().CurrentExperiment;
            CurrentExperiment = experiment != null ? experiment.Id : null;
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
                if (subscriptionSyncFinished != null) {
                    bus.Unsubscribe (subscriptionSyncFinished);
                    subscriptionSyncFinished = null;
                }
                if (subscriptionExperimentChanged != null) {
                    bus.Unsubscribe (subscriptionExperimentChanged);
                    subscriptionExperimentChanged = null;
                }
            }
        }

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

        private string CurrentExperiment
        {
            set { SetCustomDimension (Build.GoogleAnalyticsExperimentIndex, value); }
        }

        private Plan UserPlan
        {
            set {
                // Don't set the custom dimensions when it hasn't changed (except for null value)
                if (value != Plan.None && value == userPlan) {
                    return;
                }

                string planName;
                switch (value) {
                case Plan.None:
                    planName = null;
                    break;
                case Plan.Free:
                    planName = "Free";
                    break;
                case Plan.Pro:
                    planName = "Pro";
                    break;
                default:
                    throw new ArgumentException ("Unknown value.", "value");
                }

                userPlan = value;
                SetCustomDimension (Build.GoogleAnalyticsPlanIndex, planName);
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
                UserPlan = Plan.None;
                StartNewSession ();
            }
        }

        private async void OnSyncFinished (SyncFinishedMessage msg)
        {
            // Check if the user has access to any premium workspaces
            var store = ServiceContainer.Resolve<IDataStore> ();
            var numPremium = await store.Table<WorkspaceData> ().CountAsync (r => r.IsPremium);

            UserPlan = numPremium > 0 ? Plan.Pro : Plan.Free;
        }

        private void OnExperimentChanged (ExperimentChangedMessage msg)
        {
            var experiment = msg.CurrentExperiment;
            CurrentExperiment = experiment != null ? experiment.Id : null;
        }

        private enum Plan {
            None,
            Free,
            Pro
        }
    }
}
