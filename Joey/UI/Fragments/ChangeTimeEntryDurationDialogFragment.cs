using System;
using Android.App;
using Android.OS;
using Toggl.Phoebe.Data;
using Toggl.Phoebe.Data.Models;
using DialogFragment = Android.Support.V4.App.DialogFragment;

namespace Toggl.Joey.UI.Fragments
{
    public class ChangeTimeEntryDurationDialogFragment : DialogFragment
    {
        private static readonly string TimeEntryIdArgument = "com.toggl.android.time_entry_id";

        public ChangeTimeEntryDurationDialogFragment (TimeEntryModel model) : base ()
        {
            var args = new Bundle ();
            args.PutString (TimeEntryIdArgument, model.Id.ToString ());

            Arguments = args;
        }

        public ChangeTimeEntryDurationDialogFragment (IntPtr jref, Android.Runtime.JniHandleOwnership xfer) : base (jref, xfer)
        {
        }

        private Guid TimeEntryId {
            get {
                var id = Guid.Empty;
                if (Arguments != null) {
                    Guid.TryParse (Arguments.GetString (TimeEntryIdArgument), out id);
                }
                return id;
            }
        }

        private TimeEntryModel model;

        public override void OnCreate (Bundle state)
        {
            base.OnCreate (state);

            model = Model.ById<TimeEntryModel> (TimeEntryId);
            if (model == null) {
                Dismiss ();
            }
        }

        public override Dialog OnCreateDialog (Bundle state)
        {
            var hours = 0;
            var minutes = 0;

            if (model != null) {
                var duration = model.GetDuration ();
                hours = duration.Hours;
                minutes = duration.Minutes;
            }

            return new TimePickerDialog (
                Activity, OnDurationSelected,
                hours, minutes, true
            );
        }

        private void OnDurationSelected (object sender, TimePickerDialog.TimeSetEventArgs e)
        {
            if (model != null) {
                var duration = model.GetDuration ();
                duration = new TimeSpan (0, e.HourOfDay, e.Minute, duration.Seconds, duration.Milliseconds);
                model.SetDuration (duration);
            }
            Dismiss ();
        }
    }
}

