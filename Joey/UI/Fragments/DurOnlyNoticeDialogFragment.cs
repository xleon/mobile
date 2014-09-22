using System;
using Android.App;
using Android.Content;
using Android.OS;
using Toggl.Phoebe;
using Toggl.Phoebe.Net;
using XPlatUtils;
using Toggl.Joey.Data;
using FragmentManager = Android.Support.V4.App.FragmentManager;

namespace Toggl.Joey.UI.Fragments
{
    public class DurOnlyNoticeDialogFragment : BaseDialogFragment
    {
        public DurOnlyNoticeDialogFragment ()
        {
        }

        public DurOnlyNoticeDialogFragment (IntPtr jref, Android.Runtime.JniHandleOwnership xfer) : base (jref, xfer)
        {
        }

        public static bool TryShow (FragmentManager fragmentManager)
        {
            var authManager = ServiceContainer.Resolve<AuthManager> ();
            if (authManager.User == null || authManager.User.TrackingMode == TrackingMode.StartNew) {
                return false;
            }

            var settingsStore = ServiceContainer.Resolve<SettingsStore> ();
            if (settingsStore.ReadDurOnlyNotice) {
                return false;
            }

            new DurOnlyNoticeDialogFragment ().Show (fragmentManager, "notice_dialog");
            return true;
        }

        public override Dialog OnCreateDialog (Bundle savedInstanceState)
        {
            return new AlertDialog.Builder (Activity)
                   .SetTitle (Resource.String.DurOnlyNoticeDialogTitle)
                   .SetMessage (Resource.String.DurOnlyNoticeDialogMessage)
                   .SetPositiveButton (Resource.String.DurOnlyNoticeDialogOk, OnOkClicked)
                   .Create ();
        }

        private void OnOkClicked (object sender, DialogClickEventArgs e)
        {
            ServiceContainer.Resolve<SettingsStore> ().ReadDurOnlyNotice = true;
        }
    }
}
