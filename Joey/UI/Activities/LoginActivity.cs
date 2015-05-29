using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Android.Accounts;
using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Gms.Auth;
using Android.Gms.Common;
using Android.OS;
using Android.Text;
using Android.Text.Style;
using Android.Views;
using Android.Widget;
using Toggl.Joey.UI.Utils;
using Toggl.Joey.UI.Views;
using Toggl.Phoebe.Analytics;
using Toggl.Phoebe.Logging;
using Toggl.Phoebe.Net;
using XPlatUtils;
using DialogFragment = Android.Support.V4.App.DialogFragment;
using Fragment = Android.Support.V4.App.Fragment;
using FragmentManager = Android.Support.V4.App.FragmentManager;

namespace Toggl.Joey.UI.Activities
{
    [Activity (
         Exported = false,
         ScreenOrientation = ScreenOrientation.Portrait,
         WindowSoftInputMode = SoftInput.StateHidden,
         Theme = "@style/Theme.Toggl.Login")]
    public class LoginActivity : BaseActivity, ViewTreeObserver.IOnGlobalLayoutListener
    {
        private const string LogTag = "LoginActivity";

        private static readonly string ExtraShowPassword = "com.toggl.timer.show_password";
        private Mode? lastScreen;
        private bool hasGoogleAccounts;
        private bool showPassword;
        private bool isAuthenticating;
        private ISpannable formattedLegalText;

        protected ScrollView ScrollView { get; private set; }

        protected RadioGroup TabsRadioGroup { get; private set; }

        protected RadioButton LoginTabRadioButton { get; private set; }

        protected RadioButton SignupTabRadioButton { get; private set; }

        protected AutoCompleteTextView EmailEditText { get; private set; }

        protected EditText PasswordEditText { get; private set; }

        protected Button PasswordToggleButton { get; private set; }

        protected Button LoginButton { get; private set; }

        protected TextView LegalTextView { get; private set; }

        protected Button GoogleLoginButton { get; private set; }

        private void FindViews ()
        {
            ScrollView = FindViewById<ScrollView> (Resource.Id.ScrollView);
            FindViewById<TextView> (Resource.Id.SloganTextView).SetFont (Font.RobotoLight);
            TabsRadioGroup = FindViewById<RadioGroup> (Resource.Id.TabsRadioGroup);
            LoginTabRadioButton = FindViewById<RadioButton> (Resource.Id.LoginTabRadioButton).SetFont (Font.Roboto);
            SignupTabRadioButton = FindViewById<RadioButton> (Resource.Id.SignupTabRadioButton).SetFont (Font.Roboto);
            EmailEditText = FindViewById<AutoCompleteTextView> (Resource.Id.EmailAutoCompleteTextView).SetFont (Font.RobotoLight);
            PasswordEditText = FindViewById<EditText> (Resource.Id.PasswordEditText).SetFont (Font.RobotoLight);
            PasswordToggleButton = FindViewById<Button> (Resource.Id.PasswordToggleButton).SetFont (Font.Roboto);
            LoginButton = FindViewById<Button> (Resource.Id.LoginButton).SetFont (Font.Roboto);
            LegalTextView = FindViewById<TextView> (Resource.Id.LegalTextView).SetFont (Font.RobotoLight);
            GoogleLoginButton = FindViewById<Button> (Resource.Id.GoogleLoginButton).SetFont (Font.Roboto);
        }

        protected override bool StartAuthActivity ()
        {
            var authManager = ServiceContainer.Resolve<AuthManager> ();
            if (authManager.IsAuthenticated) {
                // Try to avoid flickering of buttons during activity transition by
                // faking that we're still authenticating
                IsAuthenticating = true;

                var intent = new Intent (this, typeof (MainDrawerActivity));
                intent.AddFlags (ActivityFlags.ClearTop);
                StartActivity (intent);
                Finish ();
                return true;
            }

            return false;
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

        protected override void OnCreateActivity (Bundle bundle)
        {
            base.OnCreateActivity (bundle);

            SetContentView (Resource.Layout.LoginActivity);
            FindViews ();

            ScrollView.ViewTreeObserver.AddOnGlobalLayoutListener (this);

            TabsRadioGroup.CheckedChange += OnTabsRadioGroupCheckedChange;
            LoginButton.Click += OnLoginButtonClick;
            GoogleLoginButton.Click += OnGoogleLoginButtonClick;
            EmailEditText.Adapter = MakeEmailsAdapter ();
            EmailEditText.Threshold = 1;
            EmailEditText.TextChanged += OnEmailEditTextTextChanged;
            PasswordEditText.TextChanged += OnPasswordEditTextTextChanged;
            PasswordToggleButton.Click += OnPasswordToggleButtonClick;

            hasGoogleAccounts = GoogleAccounts.Count > 0;
            GoogleLoginButton.Visibility = hasGoogleAccounts ? ViewStates.Visible : ViewStates.Gone;

            if (bundle != null) {
                showPassword = bundle.GetBoolean (ExtraShowPassword);
            }

            SyncContent ();
            SyncPasswordVisibility ();
        }

        protected override void OnSaveInstanceState (Bundle outState)
        {
            base.OnSaveInstanceState (outState);
            outState.PutBoolean (ExtraShowPassword, showPassword);
        }

        protected override void OnStart ()
        {
            base.OnStart ();
            SyncCurrentScreen ();
        }

        private void OnTabsRadioGroupCheckedChange (object sender, RadioGroup.CheckedChangeEventArgs e)
        {
            SyncContent ();
            SyncCurrentScreen ();
        }

        private void SyncCurrentScreen()
        {
            // Just to make sure we don't double count screens should this funciton be called multiple times in a row
            var currentScreen = CurrentMode;
            if (lastScreen == currentScreen) {
                return;
            }

            switch (currentScreen) {
            case Mode.Login:
                ServiceContainer.Resolve<ITracker> ().CurrentScreen = "Login";
                break;
            default:
                ServiceContainer.Resolve<ITracker> ().CurrentScreen = "Signup";
                break;
            }

            lastScreen = currentScreen;
        }

        private void SyncContent ()
        {
            // Views not loaded yet/anymore?
            if (LoginButton == null) {
                return;
            }

            if (CurrentMode == Mode.Login) {
                LoginButton.SetText (isAuthenticating ? Resource.String.LoginButtonProgressText : Resource.String.LoginButtonText);
                LegalTextView.Visibility = ViewStates.Gone;
                GoogleLoginButton.SetText (Resource.String.LoginGoogleButtonText);
            } else {
                LoginButton.SetText (isAuthenticating ? Resource.String.LoginButtonSignupProgressText : Resource.String.LoginSignupButtonText);
                LegalTextView.SetText (FormattedLegalText, TextView.BufferType.Spannable);
                LegalTextView.MovementMethod = Android.Text.Method.LinkMovementMethod.Instance;
                LegalTextView.Visibility = ViewStates.Visible;
                GoogleLoginButton.SetText (Resource.String.LoginSignupGoogleButtonText);
            }

            LoginTabRadioButton.Enabled = !isAuthenticating;
            SignupTabRadioButton.Enabled = !isAuthenticating;
            TabsRadioGroup.Enabled = !isAuthenticating;
            EmailEditText.Enabled = !isAuthenticating;
            PasswordEditText.Enabled = !isAuthenticating;
            GoogleLoginButton.Enabled = !isAuthenticating;

            SyncLoginButton ();
        }

        private void SyncLoginButton ()
        {
            var enabled = !isAuthenticating;
            if (CurrentMode == Mode.Signup) {
                if (String.IsNullOrWhiteSpace (EmailEditText.Text) || !EmailEditText.Text.Contains ('@')) {
                    enabled = false;
                } else if (String.IsNullOrWhiteSpace (PasswordEditText.Text) || PasswordEditText.Text.Length < 6) {
                    enabled = false;
                }
            }

            LoginButton.Enabled = enabled;
        }

        private void SyncPasswordVisibility ()
        {
            if (PasswordEditText.Text.Length == 0) {
                // Reset buttons and mask
                PasswordToggleButton.Visibility = ViewStates.Gone;
                showPassword = false;
            } else if (showPassword) {
                PasswordToggleButton.SetText (Resource.String.LoginHideButtonText);
                PasswordToggleButton.Visibility = ViewStates.Visible;
            } else {
                PasswordToggleButton.SetText (Resource.String.LoginShowButtonText);
                PasswordToggleButton.Visibility = ViewStates.Visible;
            }

            int selectionStart = PasswordEditText.SelectionStart;
            int selectionEnd = PasswordEditText.SelectionEnd;

            var passwordInputType = PasswordEditText.InputType;
            if (showPassword) {
                passwordInputType = (passwordInputType & ~InputTypes.TextVariationPassword) | InputTypes.TextVariationVisiblePassword;
            } else {
                passwordInputType = (passwordInputType & ~InputTypes.TextVariationVisiblePassword) | InputTypes.TextVariationPassword;
            }
            if (PasswordEditText.InputType != passwordInputType) {
                PasswordEditText.InputType = passwordInputType;

                // Need to reset font after changing input type
                PasswordEditText.SetFont (Font.RobotoLight);

                // Restore cursor position:
                PasswordEditText.SetSelection (selectionStart, selectionEnd);
            }
        }

        private void OnEmailEditTextTextChanged (object sender, TextChangedEventArgs e)
        {
            SyncLoginButton ();
        }

        private void OnPasswordEditTextTextChanged (object sender, TextChangedEventArgs e)
        {
            SyncPasswordVisibility ();
            SyncLoginButton ();
        }

        private void OnPasswordToggleButtonClick (object sender, EventArgs e)
        {
            showPassword = !showPassword;
            SyncPasswordVisibility ();
        }

        private void OnLoginButtonClick (object sender, EventArgs e)
        {
            if (CurrentMode == Mode.Login) {
                TryLoginPassword ();
            } else {
                TrySignupPassword ();
            }
        }

        private async void TryLoginPassword ()
        {
            IsAuthenticating = true;
            var authManager = ServiceContainer.Resolve<AuthManager> ();
            AuthResult authRes;
            try {
                authRes = await authManager.Authenticate (EmailEditText.Text, PasswordEditText.Text);
            } catch (InvalidOperationException ex) {
                var log = ServiceContainer.Resolve<ILogger> ();
                log.Info (LogTag, ex, "Failed to authenticate user with password.");
                return;
            } finally {
                IsAuthenticating = false;
            }

            if (authRes != AuthResult.Success) {
                if (authRes == AuthResult.InvalidCredentials) {
                    PasswordEditText.Text = String.Empty;
                }
                PasswordEditText.RequestFocus ();

                ShowAuthError (EmailEditText.Text, authRes, Mode.Login);
            } else {
                // Start the initial sync for the user
                ServiceContainer.Resolve<ISyncManager> ().Run (SyncMode.Full);
            }

            StartAuthActivity ();
        }

        private async void TrySignupPassword ()
        {
            IsAuthenticating = true;
            var authManager = ServiceContainer.Resolve<AuthManager> ();
            AuthResult authRes;
            try {
                authRes = await authManager.Signup (EmailEditText.Text, PasswordEditText.Text);
            } catch (InvalidOperationException ex) {
                var log = ServiceContainer.Resolve<ILogger> ();
                log.Info (LogTag, ex, "Failed to signup user with password.");
                return;
            } finally {
                IsAuthenticating = false;
            }

            if (authRes != AuthResult.Success) {
                EmailEditText.RequestFocus ();

                ShowAuthError (EmailEditText.Text, authRes, Mode.Signup);
            } else {
                // Start the initial sync for the user
                ServiceContainer.Resolve<ISyncManager> ().Run (SyncMode.Full);
            }

            StartAuthActivity ();
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

        private List<string> GoogleAccounts
        {
            get {
                var am = AccountManager.Get (this);
                return am.GetAccounts ()
                       .Where ((a) => a.Type == GoogleAuthUtil.GoogleAccountType)
                       .Select ((a) => a.Name)
                       .Distinct ()
                       .ToList ();
            }
        }

        private bool IsAuthenticating
        {
            set {
                if (isAuthenticating == value) {
                    return;
                }
                isAuthenticating = value;
                SyncContent ();
            }
        }

        private ISpannable FormattedLegalText
        {
            get {
                if (formattedLegalText == null) {
                    var template = Resources.GetText (Resource.String.LoginSignupLegalText);
                    var arg0 = Resources.GetText (Resource.String.LoginSignupLegalTermsText);
                    var arg1 = Resources.GetText (Resource.String.LoginSignupLegalPrivacyText);

                    var arg0idx = String.Format (template, "{0}", arg1).IndexOf ("{0}", StringComparison.Ordinal);
                    var arg1idx = String.Format (template, arg0, "{1}").IndexOf ("{1}", StringComparison.Ordinal);

                    var s = formattedLegalText = new SpannableString (String.Format (template, arg0, arg1));
                    var mode = SpanTypes.InclusiveExclusive;
                    s.SetSpan (
                        new URLSpan (Phoebe.Build.TermsOfServiceUrl.ToString ()),
                        arg0idx,
                        arg0idx + arg0.Length,
                        mode
                    );
                    s.SetSpan (
                        new URLSpan (Phoebe.Build.PrivacyPolicyUrl.ToString ()),
                        arg1idx,
                        arg1idx + arg1.Length,
                        mode
                    );
                }

                return formattedLegalText;
            }
        }

        private Mode CurrentMode
        {
            get {
                switch (TabsRadioGroup != null ? TabsRadioGroup.CheckedRadioButtonId : -1) {
                case Resource.Id.SignupTabRadioButton:
                    return Mode.Signup;
                default:
                    return Mode.Login;
                }
            }
        }

        private enum Mode {
            Login,
            Signup
        }

        private void ShowAuthError (string email, AuthResult res, Mode mode, bool googleAuth=false)
        {
            DialogFragment dia = null;

            switch (res) {
            case AuthResult.InvalidCredentials:
                if (mode == Mode.Login && !googleAuth) {
                    dia = new InvalidCredentialsDialogFragment ();
                } else if (mode == Mode.Signup && !googleAuth) {
                    dia = new SignupFailedDialogFragment ();
                } else if (mode == Mode.Login && googleAuth) {
                    dia = new NoAccountDialogFragment ();
                } else if (mode == Mode.Signup && googleAuth) {
                    dia = new SignupFailedDialogFragment ();
                }
                break;
            case AuthResult.NoDefaultWorkspace:
                dia = new NoWorkspaceDialogFragment (email);
                break;
            case AuthResult.NetworkError:
                dia = new NetworkErrorDialogFragment ();
                break;
            default:
                dia = new SystemErrorDialogFragment ();
                break;
            }

            if (dia != null) {
                dia.Show (FragmentManager, "auth_result_dialog");
            }
        }

        public class GoogleAuthFragment : Fragment
        {
            private static readonly int GoogleAuthRequestCode = 1;
            private static readonly string GoogleOAuthScope =
                "oauth2:https://www.googleapis.com/auth/userinfo.profile " +
                "https://www.googleapis.com/auth/userinfo.email";
            private static readonly string EmailArgument = "com.toggl.timer.email";

            public static void Start (FragmentManager mgr, string email)
            {
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
                if (frag != null) {
                    tx.Remove (frag);
                }
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
                if (activity != null) {
                    activity.IsAuthenticating = IsAuthenticating;
                }
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
                if (IsAuthenticating) {
                    return;
                }
                IsAuthenticating = true;

                LoginActivity activity;

                try {
                    var log = ServiceContainer.Resolve<ILogger> ();
                    var authManager = ServiceContainer.Resolve<AuthManager> ();
                    var ctx = Activity;

                    // No point in trying to reauth when old authentication is still running.
                    if (authManager.IsAuthenticating) {
                        return;
                    }

                    // Workaround for Android linker bug which forgets to register JNI types
                    Java.Interop.TypeManager.RegisterType ("com/google/android/gms/auth/GoogleAuthException", typeof (GoogleAuthException));
                    Java.Interop.TypeManager.RegisterType ("com/google/android/gms/auth/GooglePlayServicesAvailabilityException", typeof (GooglePlayServicesAvailabilityException));
                    Java.Interop.TypeManager.RegisterType ("com/google/android/gms/auth/UserRecoverableAuthException", typeof (UserRecoverableAuthException));
                    Java.Interop.TypeManager.RegisterType ("com/google/android/gms/auth/UserRecoverableNotifiedException", typeof (UserRecoverableNotifiedException));

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
                        log.Info (LogTag, exc, "Failed to login with Google due to network issues.");
                    } catch (Exception exc) {
                        log.Error (LogTag, exc, "Failed to get access token for '{0}'.", Email);
                    }

                    // Failed to get token
                    if (token == null) {
                        return;
                    }

                    try {
                        activity = Activity as LoginActivity;
                        if (activity != null && activity.CurrentMode == Mode.Signup) {
                            // Signup with Google
                            var authRes = await authManager.SignupWithGoogle (token);
                            if (authRes != AuthResult.Success) {
                                ClearGoogleToken (ctx, token);
                                activity.ShowAuthError (Email, authRes, Mode.Signup, googleAuth: true);
                            }
                        } else {
                            // Authenticate client
                            var authRes = await authManager.AuthenticateWithGoogle (token);
                            if (authRes != AuthResult.Success) {
                                ClearGoogleToken (ctx, token);
                                activity.ShowAuthError (Email, authRes, Mode.Login, googleAuth: true);
                            }
                        }
                    } catch (InvalidOperationException ex) {
                        log.Info (LogTag, ex, "Failed to authenticate user with Google login.");
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
                activity = Activity as LoginActivity;
                if (activity != null) {
                    activity.StartAuthActivity ();
                }
            }

            private void ClearGoogleToken (Context ctx, string token)
            {
                var log = ServiceContainer.Resolve<ILogger> ();

                ThreadPool.QueueUserWorkItem (delegate {
                    try {
                        GoogleAuthUtil.ClearToken (ctx, token);
                    } catch (Exception ex) {
                        log.Warning (LogTag, ex, "Failed to authenticate user with Google login.");
                    }
                });
            }

            private string Email
            {
                get {
                    return Arguments != null ? Arguments.GetString (EmailArgument) : null;
                }
            }

            private bool isAuthenticating;

            private bool IsAuthenticating
            {
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
            }

            private void OnCancelButtonClicked (object sender, DialogClickEventArgs args)
            {
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
            }
        }

        public class SignupFailedDialogFragment : DialogFragment
        {
            public override Dialog OnCreateDialog (Bundle savedInstanceState)
            {
                return new AlertDialog.Builder (Activity)
                       .SetTitle (Resource.String.LoginSignupFailedDialogTitle)
                       .SetMessage (Resource.String.LoginSignupFailedDialogText)
                       .SetPositiveButton (Resource.String.LoginSignupFailedDialogOk, OnOkButtonClicked)
                       .Create ();
            }

            private void OnOkButtonClicked (object sender, DialogClickEventArgs args)
            {
            }
        }

        public class NoAccountDialogFragment : DialogFragment
        {
            public override Dialog OnCreateDialog (Bundle savedInstanceState)
            {
                return new AlertDialog.Builder (Activity)
                       .SetTitle (Resource.String.LoginNoAccountDialogTitle)
                       .SetMessage (Resource.String.LoginNoAccountDialogText)
                       .SetPositiveButton (Resource.String.LoginNoAccountDialogOk, OnOkButtonClicked)
                       .Create ();
            }

            private void OnOkButtonClicked (object sender, DialogClickEventArgs args)
            {
            }
        }

        public class NetworkErrorDialogFragment : DialogFragment
        {
            public override Dialog OnCreateDialog (Bundle savedInstanceState)
            {
                return new AlertDialog.Builder (Activity)
                       .SetTitle (Resource.String.LoginNetworkErrorDialogTitle)
                       .SetMessage (Resource.String.LoginNetworkErrorDialogText)
                       .SetPositiveButton (Resource.String.LoginNetworkErrorDialogOk, OnOkButtonClicked)
                       .Create ();
            }

            private void OnOkButtonClicked (object sender, DialogClickEventArgs args)
            {
            }
        }

        public class SystemErrorDialogFragment : DialogFragment
        {
            public override Dialog OnCreateDialog (Bundle savedInstanceState)
            {
                return new AlertDialog.Builder (Activity)
                       .SetTitle (Resource.String.LoginSystemErrorDialogTitle)
                       .SetMessage (Resource.String.LoginSystemErrorDialogText)
                       .SetPositiveButton (Resource.String.LoginSystemErrorDialogOk, OnOkButtonClicked)
                       .Create ();
            }

            private void OnOkButtonClicked (object sender, DialogClickEventArgs args)
            {
            }
        }

        public class NoWorkspaceDialogFragment : DialogFragment
        {
            private const string EmailKey = "com.toggl.timer.email";

            public NoWorkspaceDialogFragment ()
            {
            }

            public NoWorkspaceDialogFragment (string email)
            {
                var args = new Bundle();
                args.PutString (EmailKey, email);

                Arguments = args;
            }

            private string Email
            {
                get {
                    if (Arguments == null) {
                        return String.Empty;
                    }
                    return Arguments.GetString (EmailKey);
                }
            }

            public override Dialog OnCreateDialog (Bundle savedInstanceState)
            {
                return new AlertDialog.Builder (Activity)
                       .SetTitle (Resource.String.LoginNoWorkspaceDialogTitle)
                       .SetMessage (Resource.String.LoginNoWorkspaceDialogText)
                       .SetPositiveButton (Resource.String.LoginNoWorkspaceDialogOk, OnOkButtonClicked)
                       .SetNegativeButton (Resource.String.LoginNoWorkspaceDialogCancel, OnCancelButtonClicked)
                       .Create ();
            }

            private void OnOkButtonClicked (object sender, DialogClickEventArgs args)
            {
                var intent = new Intent (Intent.ActionSend);
                intent.SetType ("message/rfc822");
                intent.PutExtra (Intent.ExtraEmail, new[] { Resources.GetString (Resource.String.LoginNoWorkspaceDialogEmail) });
                intent.PutExtra (Intent.ExtraSubject, Resources.GetString (Resource.String.LoginNoWorkspaceDialogSubject));
                intent.PutExtra (Intent.ExtraText, String.Format (Resources.GetString (Resource.String.LoginNoWorkspaceDialogBody), Email));
                StartActivity (Intent.CreateChooser (intent, (string)null));
            }

            private void OnCancelButtonClicked (object sender, DialogClickEventArgs args)
            {
            }
        }
    }
}
