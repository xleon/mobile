using System;
using Android.App;
using Android.OS;
using Toggl.Phoebe.Data;
using Toggl.Phoebe.Data.Models;

namespace Toggl.Joey.UI.Fragments
{
    public class ChangeTimeEntryDateDialogFragment : BaseDialogFragment
    {
        private static readonly string TimeEntryIdArgument = "com.toggl.timer.time_entry_id";

        public ChangeTimeEntryDateDialogFragment (TimeEntryModel model) : base ()
        {
            var args = new Bundle ();
            args.PutString (TimeEntryIdArgument, model.Id.ToString ());

            Arguments = args;
        }

        public ChangeTimeEntryDateDialogFragment (IntPtr jref, Android.Runtime.JniHandleOwnership xfer) : base (jref, xfer)
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
            var date = DateTime.Now;

            if (model != null && model.StartTime != DateTime.MinValue) {
                date = model.StartTime.ToLocalTime ().Date;
            }

            var dia = new DatePickerDialog (
                          Activity, OnDateSelected,
                          date.Year, date.Month - 1, date.Day
                      );
            dia.SetTitle (Resource.String.ChangeTimeEntryDateDialogTitle);
            return dia;
        }

        private void OnDateSelected (object sender, DatePickerDialog.DateSetEventArgs e)
        {
            if (model != null) {
                var startTime = DateTime.UtcNow;
                if (model.StartTime != DateTime.MinValue) {
                    startTime = model.StartTime.ToLocalTime ();
                }

                var selectedDate = new DateTime (e.Year, e.MonthOfYear + 1, e.DayOfMonth, 0, 0, 0, 0, DateTimeKind.Local);
                var adjustment = selectedDate - startTime.Date;

                startTime += adjustment;
                model.StartTime = startTime.ToUniversalTime ();
            }

            Dismiss ();
        }
    }
}

