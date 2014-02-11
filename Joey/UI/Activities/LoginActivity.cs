using System;
using System.Collections.Generic;
using System.Linq;
using Android.Accounts;
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
        protected AutoCompleteTextView EmailEditText { get; private set; }

        protected EditText PasswordEditText { get; private set; }

        protected Button LoginButton { get; private set; }

        private void FindViews ()
        {
            EmailEditText = FindViewById<AutoCompleteTextView> (Resource.Id.EmailAutoCompleteTextView);
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

        private ArrayAdapter<string> MakeEmailsAdapter ()
        {
            var am = AccountManager.Get (this);
            var emails = am.GetAccounts ()
                .Select ((a) => a.Name)
                .Where ((a) => a.Contains ("@"))
                .Distinct ()
                .ToList ();
            return new ArrayAdapter<string> (this, Android.Resource.Layout.SelectDialogItem, emails);
        }

        protected override void OnCreate (Bundle bundle)
        {
            base.OnCreate (bundle);

            CheckAuth ();

            SetContentView (Resource.Layout.LoginActivity);
            FindViews ();

            LoginButton.Click += OnLoginButtonClick;
            EmailEditText.Adapter = MakeEmailsAdapter ();
            EmailEditText.Threshold = 1;
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
