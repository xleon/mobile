using System;
using System.Collections.Generic;
using System.Linq;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Widget;
using Toggl.Phoebe.Net;
using XPlatUtils;

namespace Toggl.Joey.UI.Activities
{
    [Activity (
        Exported = false,
        Theme = "@style/Theme.Login")]
    public class LoginActivity : BaseActivity
    {
        protected EditText EmailEditText { get; private set; }

        protected EditText PasswordEditText { get; private set; }

        protected Button LoginButton { get; private set; }

        private void FindViews ()
        {
            EmailEditText = FindViewById<EditText> (Resource.Id.EmailEditText);
            PasswordEditText = FindViewById<EditText> (Resource.Id.PasswordEditText);
            LoginButton = FindViewById<Button> (Resource.Id.LoginButton);
        }

        protected override bool RequireAuth {
            get { return false; }
        }

        private void CheckAuth ()
        {
            var authManager = ServiceContainer.Resolve<AuthManager> ();
            if (authManager.IsAuthenticated) {
                var intent = new Intent (this, typeof(TimeEntriesActivity));
                intent.AddFlags (ActivityFlags.ClearTop);
                StartActivity (intent);
                Finish ();
            }
        }

        protected override void OnCreate (Bundle bundle)
        {
            base.OnCreate (bundle);

            CheckAuth ();

            SetContentView (Resource.Layout.LoginActivity);
            FindViews ();

            LoginButton.Click += OnLoginButtonClick;
        }

        protected override void OnResume ()
        {
            base.OnResume ();

            CheckAuth ();
        }

        private async void OnLoginButtonClick (object sender, EventArgs e)
        {
            LoginButton.Enabled = false;
            var authManager = ServiceContainer.Resolve<AuthManager> ();
            var success = await authManager.Authenticate (EmailEditText.Text, PasswordEditText.Text);
            LoginButton.Enabled = true;

            if (!success)
                PasswordEditText.Text = String.Empty;

            CheckAuth ();
        }
    }
}
