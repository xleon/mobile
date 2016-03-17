using System;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Text;
using Android.Widget;
using Toggl.Phoebe._ViewModels;

namespace Toggl.Joey.UI.Fragments
{
    public class CreateClientDialogFragment : BaseDialogFragment
    {
        private const string WorkspaceIdArgument = "workspace_id";
        private IOnClientSelectedHandler clientSelectedHandler;
        private Button positiveButton;
        public NewClientVM ViewModel { get; private set;}
        public EditText NameEditText { get; private set;}

        private Guid WorkspaceId
        {
            get {
                var id = Guid.Empty;
                if (Arguments != null) {
                    Guid.TryParse (Arguments.GetString (WorkspaceIdArgument), out id);
                }
                return id;
            }
        }

        public CreateClientDialogFragment ()
        {
        }

        public CreateClientDialogFragment (IntPtr jref, Android.Runtime.JniHandleOwnership xfer) : base (jref, xfer)
        {
        }

        public static CreateClientDialogFragment NewInstance (Guid workspaceId)
        {
            var fragment = new CreateClientDialogFragment ();

            var args = new Bundle();
            args.PutString (WorkspaceIdArgument, workspaceId.ToString ());
            fragment.Arguments = args;

            return fragment;
        }

        public override void OnCreate (Bundle savedInstanceState)
        {
            base.OnCreate (savedInstanceState);
            ViewModel = new NewClientVM (Phoebe._Reactive.StoreManager.Singleton.AppState.TimerState, WorkspaceId);
            ValidateClientName ();
        }

        public override void OnDestroy ()
        {
            ViewModel.Dispose ();
            base.OnDestroy ();
        }

        public override Dialog OnCreateDialog (Bundle savedInstanceState)
        {
            NameEditText = new EditText (Activity);
            NameEditText.SetHint (Resource.String.CreateClientDialogHint);
            NameEditText.InputType = InputTypes.TextFlagCapSentences;
            NameEditText.TextChanged += OnNameEditTextTextChanged;

            return new AlertDialog.Builder (Activity)
                   .SetTitle (Resource.String.CreateClientDialogTitle)
                   .SetView (NameEditText)
                   .SetPositiveButton (Resource.String.CreateClientDialogOk, OnPositiveButtonClicked)
                   .Create ();
        }

        public override void OnStart ()
        {
            base.OnStart ();
            positiveButton = ((AlertDialog)Dialog).GetButton ((int)DialogButtonType.Positive);
            ValidateClientName ();
        }

        public CreateClientDialogFragment SetOnClientSelectedListener (IOnClientSelectedHandler handler)
        {
            clientSelectedHandler = handler;
            return this;
        }

        private void OnNameEditTextTextChanged (object sender, TextChangedEventArgs e)
        {
            ValidateClientName ();
        }

        private void OnPositiveButtonClicked (object sender, DialogClickEventArgs e)
        {
            var clientData = ViewModel.SaveClient (NameEditText.Text);
            if (clientSelectedHandler != null) {
                clientSelectedHandler.OnClientSelected (clientData);
            }
        }

        private void ValidateClientName ()
        {
            if (positiveButton == null || NameEditText == null) {
                return;
            }

            var valid = true;
            var name = NameEditText.Text;

            if (string.IsNullOrWhiteSpace (name)) {
                valid = false;
            }

            positiveButton.Enabled = valid;
        }
    }
}
