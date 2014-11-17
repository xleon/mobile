using System;
using Android.App;
using Android.Content;
using Android.OS;
using Toggl.Phoebe;
using Toggl.Phoebe.Analytics;
using Toggl.Phoebe.Data.Models;
using XPlatUtils;
using Toggl.Joey.Data;
using FragmentManager = Android.Support.V4.App.FragmentManager;

namespace Toggl.Joey.UI.Fragments
{
    public class RecentTimeEntryContinueDialogFragment : BaseDialogFragment
    {
        private static readonly string TimeEntryIdArgument = "com.toggl.timer.time_entry_id";

        public static bool TryShow (FragmentManager fragmentManager, TimeEntryModel model)
        {
            var settingsStore = ServiceContainer.Resolve<SettingsStore> ();
            if (settingsStore.ReadContinueDialog) {
                return false;
            }

            new RecentTimeEntryContinueDialogFragment (model).Show (fragmentManager, "notice_dialog");
            return true;
        }

        private TimeEntryModel model;
        private bool modelLoaded;

        public RecentTimeEntryContinueDialogFragment ()
        {
        }

        public RecentTimeEntryContinueDialogFragment (TimeEntryModel model)
        {
            var args = new Bundle ();
            args.PutString (TimeEntryIdArgument, model.Id.ToString ());

            Arguments = args;
        }

        public RecentTimeEntryContinueDialogFragment (IntPtr jref, Android.Runtime.JniHandleOwnership xfer) : base (jref, xfer)
        {
        }

        private Guid TimeEntryId
        {
            get {
                var id = Guid.Empty;
                if (Arguments != null) {
                    Guid.TryParse (Arguments.GetString (TimeEntryIdArgument), out id);
                }
                return id;
            }
        }

        public override void OnCreate (Bundle savedInstanceState)
        {
            base.OnCreate (savedInstanceState);

            LoadData ();
        }

        private async void LoadData ()
        {
            model = new TimeEntryModel (TimeEntryId);
            await model.LoadAsync ();
            if (model.Workspace == null || model.Workspace.Id == Guid.Empty) {
                // Invalid model, do nothing.
            } else {
                modelLoaded = true;
            }
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

        private void OnOkClicked (object sender, DialogClickEventArgs e)
        {
            ServiceContainer.Resolve<SettingsStore> ().ReadContinueDialog = true;
            ContinueEntry ();
        }

        private void SetModel (TimeEntryModel model)
        {
            this.model = model;
        }

        private async void ContinueEntry ()
        {
            if (!modelLoaded) {
                return;
            }

            var entry = await model.ContinueAsync ();

            // Notify that the user explicitly started something
            var bus = ServiceContainer.Resolve<MessageBus> ();
            bus.Send (new UserTimeEntryStateChangeMessage (this, entry));

            // Ping analytics
            ServiceContainer.Resolve<ITracker> ().SendTimerStartEvent (TimerStartSource.AppContinue);
        }
    }
}
