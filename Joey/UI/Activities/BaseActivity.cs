using System;
using Android.App;
using XPlatUtils;
using Toggl.Phoebe.Net;
using Android.Content;

namespace Toggl.Joey.UI.Activities
{
    public abstract class BaseActivity : Activity
    {
        protected virtual bool RequireAuth {
            get { return true; }
        }

        private void CheckAuth ()
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

        protected override void OnCreate (Android.OS.Bundle state)
        {
            base.OnCreate (state);
            CheckAuth ();
        }

        protected override void OnResume ()
        {
            base.OnResume ();
            CheckAuth ();
        }
    }
}
