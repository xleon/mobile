using System;
using Android.App;
using Android.OS;
using Android.Provider;
using Toggl.Phoebe.Data;
using Toggl.Phoebe.Data.Models;
using DialogFragment = Android.Support.V4.App.DialogFragment;

namespace Toggl.Joey.UI.Fragments
{
    public class ChangeTimeEntryStartTimeDialogFragment : DialogFragment
    {
        private static readonly string TimeEntryIdArgument = "com.toggl.android.time_entry_id";

        public ChangeTimeEntryStartTimeDialogFragment (TimeEntryModel model) : base ()
        {
            var args = new Bundle ();
            args.PutString (TimeEntryIdArgument, model.Id.ToString ());

            Arguments = args;
        }

        public ChangeTimeEntryStartTimeDialogFragment (IntPtr jref, Android.Runtime.JniHandleOwnership xfer) : base (jref, xfer)
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
            var time = DateTime.Now;
            if (model != null && model.StartTime != DateTime.MinValue) {
                time = model.StartTime.ToLocalTime ();
            }

            var clockType = Settings.System.GetString (Activity.ContentResolver, Settings.System.Time1224);
            var is24h = !(clockType == null || clockType == "12");

            return new TimePickerDialog (
                Activity, OnTimeSelected,
                time.Hour, time.Minute, is24h
            );
        }

        private void OnTimeSelected (object sender, TimePickerDialog.TimeSetEventArgs e)
        {
            if (model != null) {
                var dt = DateTime.Now;
                if (model.StartTime != DateTime.MinValue) {
                    dt = model.StartTime.ToLocalTime ();
                }

                model.StartTime = dt.Date
                    .AddHours (e.HourOfDay)
                    .AddMinutes (e.Minute);
            }

            Dismiss ();
        }
    }
}

