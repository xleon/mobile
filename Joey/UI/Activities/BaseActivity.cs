using System;
using Android.Content;
using Android.OS;
using Android.Views;
using Toggl.Phoebe;
using Toggl.Phoebe.Net;
using XPlatUtils;
using Toggl.Joey.Bugsnag;
using ActionBar = Android.Support.V7.App.ActionBar;
using Activity = Android.Support.V4.App.FragmentActivity;
using FragmentManager = Android.Support.V4.App.FragmentManager;

namespace Toggl.Joey.UI.Activities
{
    public abstract class BaseActivity : Activity
    {
        private const int SyncErrorMenuItemId = 0;
        protected readonly Handler Handler = new Handler ();
        private Subscription<SyncStartedMessage> subscriptionSyncStarted;
        private Subscription<SyncFinishedMessage> subscriptionSyncFinished;

        private void OnSyncStarted (SyncStartedMessage msg)
        {
            if (Handle == IntPtr.Zero)
                return;
            Handler.PostDelayed (ResetSyncProgressBar, 2500);
        }

        private void OnSyncFinished (SyncFinishedMessage msg)
        {
            if (Handle == IntPtr.Zero)
                return;
            ToggleProgressBar (false);
        }

        private void ResetSyncProgressBar ()
        {
            var syncManager = ServiceContainer.Resolve<SyncManager> ();
            ToggleProgressBar (syncManager.IsRunning);
        }

        private void ToggleProgressBar (bool switchOn)
        {
            SetProgressBarIndeterminate (true);
            SetProgressBarVisibility (switchOn);
        }

        protected virtual bool RequireAuth {
            get { return true; }
        }

        protected void CheckAuth ()
        {
            if (!RequireAuth)
                return;
            var authManager = ServiceContainer.Resolve<AuthManager> ();
            if (!authManager.IsAuthenticated) {
                var intent = new Intent (this, typeof(LoginActivity));
                intent.AddFlags (ActivityFlags.ClearTop);
                StartActivity (intent);
                Finish ();
            }
        }

        private BugsnagClient BugsnagClient {
            get {
                return (BugsnagClient)ServiceContainer.Resolve<Toggl.Phoebe.Bugsnag.BugsnagClient> ();
            }
        }

        protected override void OnCreate (Bundle state)
        {
            base.OnCreate (state);
            BugsnagClient.OnActivityCreated (this);

            var bus = ServiceContainer.Resolve<MessageBus> ();
            subscriptionSyncStarted = bus.Subscribe<SyncStartedMessage> (OnSyncStarted);
            subscriptionSyncFinished = bus.Subscribe<SyncFinishedMessage> (OnSyncFinished);

            RequestWindowFeature (WindowFeatures.Progress);
            CheckAuth ();
        }

        protected override void OnResume ()
        {
            base.OnResume ();
            BugsnagClient.OnActivityResumed (this);
            CheckAuth ();

            ResetSyncProgressBar ();
        }

        protected override void OnPause ()
        {
            base.OnPause ();
            BugsnagClient.OnActivityPaused (this);
        }

        protected override void OnDestroy ()
        {
            base.OnDestroy ();
            BugsnagClient.OnActivityDestroyed (this);

            var bus = ServiceContainer.Resolve<MessageBus> ();
            bus.Unsubscribe (subscriptionSyncStarted);
            bus.Unsubscribe (subscriptionSyncFinished);
        }

        public new FragmentManager FragmentManager {
            get { return SupportFragmentManager; }
        }
    }
}
