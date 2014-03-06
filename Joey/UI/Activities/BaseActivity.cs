using System;
using Android.Content;
using Android.Views;
using Toggl.Phoebe;
using Toggl.Phoebe.Net;
using XPlatUtils;
using Toggl.Joey.Bugsnag;
using ActionBar = Android.Support.V7.App.ActionBar;
using Activity = Android.Support.V7.App.ActionBarActivity;
using FragmentManager = Android.Support.V4.App.FragmentManager;

namespace Toggl.Joey.UI.Activities
{
    public abstract class BaseActivity : Activity
    {
        private Subscription<SyncStartedMessage> subscriptionSyncStarted;
        private Subscription<SyncFinishedMessage> subscriptionSyncFinished;
        private const int SyncErrorMenuItemId = 0;

        private void OnSyncStarted (SyncStartedMessage msg)
        {
            ToggleProgressBar (true);
        }

        private void OnSyncFinished (SyncFinishedMessage msg)
        {
            ToggleProgressBar (false);
            if (msg.HadErrors) {
                //TODO Show some identificator
//                if (menu != null && menu.FindItem (SyncErrorMenuItemId) == null) {
//                    menu.Add (Menu.None, SyncErrorMenuItemId, Menu.None, "Sync error")
//                        .SetIcon (Resource.Drawable.IcDialogAlertHoloLight)
//                        .SetShowAsAction (ShowAsAction.Always);
//                }
            } else {
//                if (menu != null && menu.FindItem (SyncErrorMenuItemId) != null) {
//                    menu.RemoveItem (SyncErrorMenuItemId);
//                }
            }
        }

        public override bool OnCreateOptionsMenu (IMenu menu)
        {
            this.menu = menu;
            return base.OnCreateOptionsMenu (menu);
        }

        private void ToggleProgressBar (bool switchOn)
        {
            if (Handle == IntPtr.Zero)
                return;

            // For some reason It's not enought to make it in OnCreate.
            SetSupportProgressBarIndeterminate (true);
            SetSupportProgressBarVisibility (switchOn);
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

        protected override void OnCreate (Android.OS.Bundle state)
        {
            base.OnCreate (state);
            var bus = ServiceContainer.Resolve<MessageBus> ();
            subscriptionSyncStarted = bus.Subscribe<SyncStartedMessage> (OnSyncStarted);
            subscriptionSyncFinished = bus.Subscribe<SyncFinishedMessage> (OnSyncFinished);
            RequestWindowFeature (WindowFeatures.Progress);
            BugsnagClient.OnActivityCreated (this);
            SetSupportProgressBarIndeterminate (true);
            CheckAuth ();
        }

        protected override void OnResume ()
        {
            base.OnResume ();
            BugsnagClient.OnActivityResumed (this);
            CheckAuth ();
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

        public new ActionBar ActionBar {
            get { return SupportActionBar; }
        }

        public new FragmentManager FragmentManager {
            get { return SupportFragmentManager; }
        }
    }
}
