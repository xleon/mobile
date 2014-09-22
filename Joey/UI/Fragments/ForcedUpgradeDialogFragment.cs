using System;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Views;

namespace Toggl.Joey.UI.Fragments
{
    public class ForcedUpgradeDialogFragment : BaseDialogFragment, View.IOnClickListener
    {
        public ForcedUpgradeDialogFragment ()
        {
        }

        public ForcedUpgradeDialogFragment (IntPtr jref, Android.Runtime.JniHandleOwnership xfer) : base (jref, xfer)
        {
        }

        public override Dialog OnCreateDialog (Bundle savedInstanceState)
        {
            Cancelable = false;
            return new AlertDialog.Builder (Activity)
                   .SetTitle (Resource.String.ForcedUpgradeDialogTitle)
                   .SetMessage (Resource.String.ForcedUpgradeDialogMessage)
                   .SetPositiveButton (Resource.String.ForcedUpgradeDialogUpdate, (IDialogInterfaceOnClickListener)null)
                   .Create ();
        }

        public override void OnStart ()
        {
            base.OnStart ();

            // Hook up the listener like that such that the dialog wouldn't be dismissed on pressing the button.
            var btn = ((AlertDialog)Dialog).GetButton ((int)DialogButtonType.Positive);
            btn.SetOnClickListener (this);
        }

        void View.IOnClickListener.OnClick (View v)
        {
            StartActivity (new Intent (
                               Intent.ActionView,
                               Android.Net.Uri.Parse (Toggl.Phoebe.Build.GooglePlayUrl)
                           ));
        }
    }
}
