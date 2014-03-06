using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Android.Accounts;
using Android.App;
using Android.Content;
using Android.Gms.Auth;
using Android.Gms.Common;
using Android.OS;
using Android.Text;
using Android.Views;
using Android.Widget;
using Toggl.Phoebe;
using Toggl.Phoebe.Net;
using XPlatUtils;
using DialogFragment = Android.Support.V4.App.DialogFragment;
using Fragment = Android.Support.V4.App.Fragment;
using FragmentManager = Android.Support.V4.App.FragmentManager;

namespace Toggl.Joey.UI.Activities
{
    [Activity (
        Exported = false,
        WindowSoftInputMode = SoftInput.StateHidden,
        Theme = "@style/Theme.Toggl.Login")]
    public class LoginActivity : BaseActivity, ViewTreeObserver.IOnGlobalLayoutListener
    {
        private static readonly string ExtraHidePassword = "com.toggl.android.hide_password";
        private bool hasGoogleAccounts;
        private bool hidePassword;

        protected ScrollView ScrollView { get; private set; }

        protected AutoCompleteTextView EmailEditText { get; private set; }

        protected EditText PasswordEditText { get; private set; }

        protected Button PasswordToggleButton { get; private set; }

        protected Button LoginButton { get; private set; }

        protected Button GoogleLoginButton { get; private set; }

        private void FindViews ()
        {
            ScrollView = FindViewById<ScrollView> (Resource.Id.ScrollView);
            EmailEditText = FindViewById<AutoCompleteTextView> (Resource.Id.EmailAutoCompleteTextView);
            PasswordEditText = FindViewById<EditText> (Resource.Id.PasswordEditText);
            PasswordToggleButton = FindViewById<Button> (Resource.Id.PasswordToggleButton);
            LoginButton = FindViewById<Button> (Resource.Id.LoginButton);
            GoogleLoginButton = FindViewById<Button> (Resource.Id.GoogleLoginButton);
        }

        protected override bool RequireAuth {
            get { return false; }
        }

        private void CheckAuth ()
        {
            var authManager = ServiceContainer.Resolve<AuthManager> ();
            if (authManager.IsAuthenticated) {
                // Try to avoid flickering of buttons during activity transition by
                // faking that we're still authenticating
                IsAuthenticating = true;

                var intent = new Intent (this, typeof(MainDrawerActivity));
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

        void ViewTreeObserver.IOnGlobalLayoutListener.OnGlobalLayout ()
        {
            // Make sure that the on every resize we scroll to the bottom
            ScrollView.ScrollTo (0, ScrollView.Bottom);
        }

        protected override void OnCreate (Bundle bundle)
        {
            base.OnCreate (bundle);

            CheckAuth ();

            SetContentView (Resource.Layout.LoginActivity);
            FindViews ();

            ScrollView.ViewTreeObserver.AddOnGlobalLayoutListener (this);

            LoginButton.Click += OnLoginButtonClick;
            GoogleLoginButton.Click += OnGoogleLoginButtonClick;
            EmailEditText.Adapter = MakeEmailsAdapter ();
            EmailEditText.Threshold = 1;
            PasswordEditText.TextChanged += OnPasswordEditTextTextChanged;
            PasswordToggleButton.Click += OnPasswordToggleButtonClick;

            hasGoogleAccounts = GoogleAccounts.Count > 0;
            GoogleLoginButton.Visibility = hasGoogleAccounts ? ViewStates.Visible : ViewStates.Gone;

            if (bundle != null) {
                hidePassword = bundle.GetBoolean (ExtraHidePassword);
            }

            SyncPasswordVisibility ();
        }

        protected override void OnSaveInstanceState (Bundle outState)
        {
            base.OnSaveInstanceState (outState);
            outState.PutBoolean (ExtraHidePassword, hidePassword);
        }

        protected override void OnResume ()
        {
            base.OnResume ();

            CheckAuth ();
        }

        private void SyncPasswordVisibility ()
        {
            if (PasswordEditText.Text.Length == 0) {
                // Reset buttons and mask
                PasswordToggleButton.Visibility = ViewStates.Gone;
                hidePassword = false;
            } else if (hidePassword) {
                PasswordToggleButton.SetText (Resource.String.LoginShowButtonText);
                PasswordToggleButton.Visibility = ViewStates.Visible;
            } else {
                PasswordToggleButton.SetText (Resource.String.LoginHideButtonText);
                PasswordToggleButton.Visibility = ViewStates.Visible;
            }

            int selectionStart = PasswordEditText.SelectionStart;
            int selectionEnd = PasswordEditText.SelectionEnd;

            if (hidePassword) {
                PasswordEditText.InputType = (PasswordEditText.InputType & ~InputTypes.TextVariationVisiblePassword) | InputTypes.TextVariationPassword;
            } else {
                PasswordEditText.InputType = (PasswordEditText.InputType & ~InputTypes.TextVariationPassword) | InputTypes.TextVariationVisiblePassword;
            }

            // Restore cursor position:
            PasswordEditText.SetSelection (selectionStart, selectionEnd);
        }

        private void OnPasswordEditTextTextChanged (object sender, Android.Text.TextChangedEventArgs e)
        {
            SyncPasswordVisibility ();
        }

        private void OnPasswordToggleButtonClick (object sender, EventArgs e)
        {
            hidePassword = !hidePassword;
            SyncPasswordVisibility ();
        }

        private async void OnLoginButtonClick (object sender, EventArgs e)
        {
            IsAuthenticating = true;
            var authManager = ServiceContainer.Resolve<AuthManager> ();
            var success = await authManager.Authenticate (EmailEditText.Text, PasswordEditText.Text);
            IsAuthenticating = false;

            if (!success) {
                PasswordEditText.Text = String.Empty;
                PasswordEditText.RequestFocus ();

                new InvalidCredentialsDialogFragment ().Show (FragmentManager, "invalid_credentials_dialog");
            }

            CheckAuth ();
        }

        private void OnGoogleLoginButtonClick (object sender, EventArgs e)
        {
            var accounts = GoogleAccounts;

            if (accounts.Count == 1) {
                GoogleAuthFragment.Start (FragmentManager, accounts [0]);
            } else if (accounts.Count > 1) {
                var dia = new GoogleAccountSelectionDialogFragment ();
                dia.Show (FragmentManager, "accounts_dialog");
            }
        }

        private List<string> GoogleAccounts {
            get {
                var am = AccountManager.Get (this);
                return am.GetAccounts ()
                    .Where ((a) => a.Type == GoogleAuthUtil.GoogleAccountType)
                    .Select ((a) => a.Name)
                    .Distinct ()
                    .ToList ();
            }
        }

        private bool IsAuthenticating {
            set {
                EmailEditText.Enabled = !value;
                PasswordEditText.Enabled = !value;
                LoginButton.Enabled = !value;
                LoginButton.SetText (value ? Resource.String.LoginButtonProgressText : Resource.String.LoginButtonText);
                GoogleLoginButton.Enabled = !value;
            }
        }

        public class GoogleAuthFragment : Fragment
        {
            private static readonly int GoogleAuthRequestCode = 1;
            private static readonly string GoogleOAuthScope =
                "oauth2:https://www.googleapis.com/auth/userinfo.profile " +
                "https://www.googleapis.com/auth/userinfo.email";
            private static readonly string EmailArgument = "com.toggl.android.email";

            public static void Start (FragmentManager mgr, string email)
            {
                if (mgr.FindFragmentByTag ("google_auth") != null)
                    return;

                // Find old fragment to replace
                var frag = mgr.FindFragmentByTag ("google_auth");
                if (frag != null) {
                    var authFrag = frag as GoogleAuthFragment;
                    if (authFrag != null && authFrag.IsAuthenticating) {
                        // Authentication going on still, do nothing.
                        return;
                    }
                }

                var tx = mgr.BeginTransaction ();
                if (frag != null)
                    tx.Remove (frag);
                tx.Add (new GoogleAuthFragment (email), "google_auth");
                tx.Commit ();
            }

            public GoogleAuthFragment (string email) : base ()
            {
                var args = new Bundle ();
                args.PutString (EmailArgument, email);
                Arguments = args;
            }

            public GoogleAuthFragment () : base ()
            {
            }

            public GoogleAuthFragment (IntPtr javaRef, Android.Runtime.JniHandleOwnership transfer) : base (javaRef, transfer)
            {
            }

            public override void OnCreate (Bundle state)
            {
                base.OnCreate (state);

                RetainInstance = true;
                StartAuth ();
            }

            public override void OnActivityCreated (Bundle state)
            {
                base.OnActivityCreated (state);

                // Restore IsAuthenticating value
                var activity = Activity as LoginActivity;
                if (activity != null)
                    activity.IsAuthenticating = IsAuthenticating;
            }

            public override void OnActivityResult (int requestCode, int resultCode, Intent data)
            {
                base.OnActivityResult (requestCode, resultCode, data);

                if (requestCode == GoogleAuthRequestCode) {
                    if (resultCode == (int)Result.Ok) {
                        StartAuth ();
                    }
                }
            }

            private async void StartAuth ()
            {
                if (IsAuthenticating)
                    return;
                IsAuthenticating = true;

                try {
                    var log = ServiceContainer.Resolve<Logger> ();
                    var authManager = ServiceContainer.Resolve<AuthManager> ();
                    var ctx = Activity;

                    String token = null;
                    try {
                        token = await Task.Factory.StartNew (() => GoogleAuthUtil.GetToken (ctx, Email, GoogleOAuthScope));
                    } catch (GooglePlayServicesAvailabilityException exc) {
                        var dia = GooglePlayServicesUtil.GetErrorDialog (
                                      exc.ConnectionStatusCode, ctx, GoogleAuthRequestCode);
                        dia.Show ();
                    } catch (UserRecoverableAuthException exc) {
                        StartActivityForResult (exc.Intent, GoogleAuthRequestCode);
                    } catch (Java.IO.IOException exc) {
                        // Connectivity error.. nothing to do really?
                        log.Info (Tag, exc, "Failed to login with Google due to network issues.");
                    } catch (Exception exc) {
                        log.Error (Tag, exc, "Failed to get access token for '{0}'.", Email);
                    }

                    // Failed to get token
                    if (token == null) {
                        return;
                    }

                    // Authenticate client
                    var success = await authManager.AuthenticateWithGoogle (token);
                    if (!success) {
                        GoogleAuthUtil.InvalidateToken (ctx, token);
                    }
                } finally {
                    IsAuthenticating = false;
                }

                // Clean up self:
                if (Activity != null) {
                    FragmentManager.BeginTransaction ()
                        .Remove (this)
                        .Commit ();
                }

                // Try to make the activity recheck auth status
                var activity = Activity as LoginActivity;
                if (activity != null) {
                    activity.CheckAuth ();
                }
            }

            private string Email {
                get {
                    return Arguments != null ? Arguments.GetString (EmailArgument) : null;
                }
            }

            private bool isAuthenticating;

            private bool IsAuthenticating {
                get { return isAuthenticating; }
                set {
                    isAuthenticating = value;
                    var activity = Activity as LoginActivity;
                    if (activity != null) {
                        activity.IsAuthenticating = isAuthenticating;
                    }
                }
            }
        }

        public class GoogleAccountSelectionDialogFragment : DialogFragment
        {
            private ArrayAdapter<string> accountsAdapter;

            public override Dialog OnCreateDialog (Bundle savedInstanceState)
            {
                accountsAdapter = MakeAccountsAdapter ();

                return new AlertDialog.Builder (Activity)
                        .SetTitle (Resource.String.LoginAccountsDialogTitle)
                        .SetAdapter (MakeAccountsAdapter (), OnAccountSelected)
                        .SetNegativeButton (Resource.String.LoginAccountsDialogCancelButton, OnCancelButtonClicked)
                        .Create ();
            }

            private void OnAccountSelected (object sender, DialogClickEventArgs args)
            {
                var email = accountsAdapter.GetItem (args.Which);
                GoogleAuthFragment.Start (FragmentManager, email);
                Dismiss ();
            }

            private void OnCancelButtonClicked (object sender, DialogClickEventArgs args)
            {
                Dismiss ();
            }

            private ArrayAdapter<string> MakeAccountsAdapter ()
            {
                var am = AccountManager.Get (Activity);
                var emails = am.GetAccounts ()
                                    .Where ((a) => a.Type == GoogleAuthUtil.GoogleAccountType)
                                    .Select ((a) => a.Name)
                                    .Distinct ()
                                    .ToList ();

                return new ArrayAdapter<string> (Activity, Android.Resource.Layout.SimpleListItem1, emails);
            }
        }

        public class InvalidCredentialsDialogFragment : DialogFragment
        {
            public override Dialog OnCreateDialog (Bundle savedInstanceState)
            {
                return new AlertDialog.Builder (Activity)
                        .SetTitle (Resource.String.LoginInvalidCredentialsDialogTitle)
                        .SetMessage (Resource.String.LoginInvalidCredentialsDialogText)
                        .SetPositiveButton (Resource.String.LoginInvalidCredentialsDialogOk, OnOkButtonClicked)
                        .Create ();
            }

            private void OnOkButtonClicked (object sender, DialogClickEventArgs args)
            {
                Dismiss ();
            }
        }
    }
}
