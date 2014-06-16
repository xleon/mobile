using System;
using Android.App;
using Android.Content;
using Android.OS;
using Toggl.Phoebe;
using XPlatUtils;
using Toggl.Joey.Data;
using TimeEntryModel = Toggl.Phoebe.Data.Models.TimeEntryModel;
using FragmentManager = Android.Support.V4.App.FragmentManager;

namespace Toggl.Joey.UI.Fragments
{
    public class RecentTimeEntryContinueDialogFragment : BaseDialogFragment
    {

        private TimeEntryModel model;

        public RecentTimeEntryContinueDialogFragment ()
        {
        }

        public RecentTimeEntryContinueDialogFragment (IntPtr jref, Android.Runtime.JniHandleOwnership xfer) : base (jref, xfer)
        {
        }

        public override Dialog OnCreateDialog (Bundle savedInstanceState)
        {
            return new AlertDialog.Builder (Activity)
                .SetTitle (Resource.String.RecentTimeEntryContinueDialogTitle)
                .SetMessage (Resource.String.RecentTimeEntryContinueDialogMessage)
                .SetPositiveButton (Resource.String.RecentTimeEntryContinueDialogOk, OnOkClicked)
                .SetNegativeButton (Resource.String.RecentTimeEntryContinueDialogCancel, (EventHandler<DialogClickEventArgs>)null)
                .Create ();
        }

        public static bool ShowConfirm (FragmentManager fragmentManager, TimeEntryModel model)
        {
            RecentTimeEntryContinueDialogFragment f = new RecentTimeEntryContinueDialogFragment ();
            f.Show (fragmentManager, "notice_dialog");
            f.SetModel (model);
            return true;
        }

        private void OnOkClicked (object sender, DialogClickEventArgs e)
        {
            ServiceContainer.Resolve<SettingsStore> ().ReadContinueDialog = true;
            ContinueEntry ();
            Dismiss ();
        }

        private void SetModel (TimeEntryModel model)
        {
            this.model = model;
        }

        private void ContinueEntry ()
        {
            var entry = model.Continue ();

            // Notify that the user explicitly started something
            var bus = ServiceContainer.Resolve<MessageBus> ();
            bus.Send (new UserTimeEntryStateChangeMessage (this, entry));
        }
    }
}

