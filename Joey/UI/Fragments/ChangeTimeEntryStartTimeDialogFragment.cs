using System;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Provider;
using Android.Text.Format;
using Android.Util;
using Android.Views;
using Android.Widget;
using Toggl.Phoebe;
using Toggl.Phoebe.Data;
using Toggl.Phoebe.Data.Models;
using Toggl.Joey.UI.Utils;
using Toggl.Joey.UI.Views;

namespace Toggl.Joey.UI.Fragments
{
    public class ChangeTimeEntryStartTimeDialogFragment : BaseDialogFragment, DatePicker.IOnDateChangedListener
    {
        private static readonly string TimeEntryIdArgument = "com.toggl.timer.time_entry_id";

        public ChangeTimeEntryStartTimeDialogFragment (TimeEntryModel model) : base ()
        {
            var args = new Bundle ();
            args.PutString (TimeEntryIdArgument, model.Id.ToString ());

            Arguments = args;
        }

        public ChangeTimeEntryStartTimeDialogFragment ()
        {
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
        private bool is24h;

        protected RadioGroup TabsRadioGroup { get; private set; }

        protected RadioButton TimeTabRadioButton { get; private set; }

        protected RadioButton DateTabRadioButton { get; private set; }

        protected TimePicker TimePicker { get; private set; }

        protected DatePicker DatePicker { get; private set; }

        public override void OnCreate (Bundle state)
        {
            base.OnCreate (state);

            model = Model.ById<TimeEntryModel> (TimeEntryId);
            if (model == null) {
                Dismiss ();
            }

            var clockType = Settings.System.GetString (Activity.ContentResolver, Settings.System.Time1224);
            is24h = !(clockType == null || clockType == "12");
        }

        public override Dialog OnCreateDialog (Bundle state)
        {
            var time = DateTime.Now;
            if (model != null && model.StartTime != DateTime.MinValue) {
                time = model.StartTime.ToLocalTime ();
            }
            var date = DateTime.Now;
            if (model != null && model.StartTime != DateTime.MinValue) {
                date = model.StartTime.ToLocalTime ().Date;
            }

            var view = LayoutInflater.From (Activity)
                .Inflate (Resource.Layout.ChangeTimeEntryStartTimeDialogFragment, null);
            TabsRadioGroup = view.FindViewById<RadioGroup> (Resource.Id.TabsRadioGroup);
            TimeTabRadioButton = view.FindViewById<RadioButton> (Resource.Id.TimeTabRadioButton).SetFont (Font.Roboto);
            DateTabRadioButton = view.FindViewById<RadioButton> (Resource.Id.DateTabRadioButton).SetFont (Font.Roboto);
            TimePicker = view.FindViewById<TimePicker> (Resource.Id.TimePicker);
            DatePicker = view.FindViewById<DatePicker> (Resource.Id.DatePicker);

            TabsRadioGroup.CheckedChange += OnTabsRadioGroupCheckedChange;

            TimePicker.CurrentHour = new Java.Lang.Integer (time.Hour);
            TimePicker.CurrentMinute = new Java.Lang.Integer (time.Minute);
            TimePicker.SetIs24HourView (new Java.Lang.Boolean (is24h));
            TimePicker.TimeChanged += OnTimePickerTimeChanged;

            DatePicker.Init (date.Year, date.Month - 1, date.Day, this);

            Rebind ();

            var dia = new AlertDialog.Builder (Activity)
                .SetTitle (Resource.String.ChangeTimeEntryStartTimeDialogTitle)
                .SetView (view)
                .SetPositiveButton (Resource.String.ChangeTimeEntryStartTimeDialogOk, OnOkButtonClicked)
                .Create ();

            return dia;
        }

        private void OnTabsRadioGroupCheckedChange (object sender, RadioGroup.CheckedChangeEventArgs e)
        {
            Rebind ();
        }

        private void Rebind ()
        {
            if (TabsRadioGroup.CheckedRadioButtonId == TimeTabRadioButton.Id) {
                TimeTabRadioButton.SetTextSize (ComplexUnitType.Dip, 18);
                DateTabRadioButton.SetTextSize (ComplexUnitType.Dip, 14);
                TimePicker.Visibility = ViewStates.Visible;
                DatePicker.Visibility = ViewStates.Gone;
            } else {
                TimeTabRadioButton.SetTextSize (ComplexUnitType.Dip, 14);
                DateTabRadioButton.SetTextSize (ComplexUnitType.Dip, 18);
                TimePicker.Visibility = ViewStates.Gone;
                DatePicker.Visibility = ViewStates.Visible;
            }

            var dateTime = DateTime;
            TimeTabRadioButton.Text = FormatTime (dateTime);
            DateTabRadioButton.Text = FormatDate (dateTime);
        }

        private void OnTimePickerTimeChanged (object sender, TimePicker.TimeChangedEventArgs e)
        {
            Rebind ();
        }

        void DatePicker.IOnDateChangedListener.OnDateChanged (DatePicker view, int year, int monthOfYear, int dayOfMonth)
        {
            Rebind ();
        }

        private void OnOkButtonClicked (object sender, DialogClickEventArgs args)
        {
            if (model != null) {
                model.StartTime = DateTime;
            }

            Dismiss ();
        }

        private DateTime DateTime {
            get {
                return DateTime.SpecifyKind (DatePicker.DateTime
                    .AddHours (TimePicker.CurrentHour.IntValue ())
                    .AddMinutes (TimePicker.CurrentMinute.IntValue ()), DateTimeKind.Local);
            }
        }

        private string FormatTime (DateTime time)
        {
            if (is24h) {
                return time.ToString ("HH:mm");
            }
            return time.ToString ("h:mm tt");
        }

        private string FormatDate (DateTime date)
        {
            var javaDate = new Java.Util.Date ((long)date.ToUnix ().TotalMilliseconds);
            return DateFormat.GetDateFormat (Activity).Format (javaDate);
        }
    }
}

