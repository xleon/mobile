using System;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Text.Format;
using Android.Util;
using Android.Views;
using Android.Widget;
using Toggl.Phoebe.Data.Models;
using XPlatUtils;
using Toggl.Joey.UI.Utils;
using Toggl.Joey.UI.Views;

namespace Toggl.Joey.UI.Fragments
{
    public class ChangeTimeEntryStopTimeDialogFragment : BaseDialogFragment, DatePicker.IOnDateChangedListener
    {
        private static readonly string TimeEntryIdArgument = "com.toggl.timer.time_entry_id";

        public ChangeTimeEntryStopTimeDialogFragment (TimeEntryModel model) : base ()
        {
            var args = new Bundle ();
            args.PutString (TimeEntryIdArgument, model.Id.ToString ());

            Arguments = args;
        }

        public ChangeTimeEntryStopTimeDialogFragment ()
        {
        }

        public ChangeTimeEntryStopTimeDialogFragment (IntPtr jref, Android.Runtime.JniHandleOwnership xfer) : base (jref, xfer)
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

        protected RadioGroup TabsRadioGroup { get; private set; }

        protected RadioButton TimeTabRadioButton { get; private set; }

        protected RadioButton DateTabRadioButton { get; private set; }

        protected TimePicker TimePicker { get; private set; }

        protected DatePicker DatePicker { get; private set; }

        public override void OnCreate (Bundle state)
        {
            base.OnCreate (state);

            // TODO: Really should use async here
            model = new TimeEntryModel (TimeEntryId);
            model.LoadAsync ().Wait ();
            if (model.Workspace == null || model.Workspace.Id == Guid.Empty) {
                Dismiss ();
            }
        }

        public override Dialog OnCreateDialog (Bundle state)
        {
            var time = Toggl.Phoebe.Time.Now;
            if (model != null && model.StopTime.HasValue) {
                time = model.StopTime.Value.ToLocalTime ();
            }

            var date = Toggl.Phoebe.Time.Now;
            if (model != null && model.StopTime.HasValue) {
                date = model.StopTime.Value.ToLocalTime ().Date;
            }

            var view = LayoutInflater.From (Activity)
                .Inflate (Resource.Layout.ChangeTimeEntryStopTimeDialogFragment, null);
            TabsRadioGroup = view.FindViewById<RadioGroup> (Resource.Id.TabsRadioGroup);
            TimeTabRadioButton = view.FindViewById<RadioButton> (Resource.Id.TimeTabRadioButton).SetFont (Font.Roboto);
            DateTabRadioButton = view.FindViewById<RadioButton> (Resource.Id.DateTabRadioButton).SetFont (Font.Roboto);
            TimePicker = view.FindViewById<TimePicker> (Resource.Id.TimePicker);
            DatePicker = view.FindViewById<DatePicker> (Resource.Id.DatePicker);

            TabsRadioGroup.CheckedChange += OnTabsRadioGroupCheckedChange;

            TimePicker.SetIs24HourView (new Java.Lang.Boolean (
                DateFormat.Is24HourFormat (ServiceContainer.Resolve<Context> ())));
            TimePicker.CurrentHour = new Java.Lang.Integer (time.Hour);
            TimePicker.CurrentMinute = new Java.Lang.Integer (time.Minute);
            TimePicker.TimeChanged += OnTimePickerTimeChanged;

            DatePicker.Init (date.Year, date.Month - 1, date.Day, this);

            Rebind ();

            var dia = new AlertDialog.Builder (Activity)
                .SetTitle (Resource.String.ChangeTimeEntryStopTimeDialogTitle)
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
            TimeTabRadioButton.Text = dateTime.ToDeviceTimeString ();
            DateTabRadioButton.Text = dateTime.ToDeviceDateString ();
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
                model.StopTime = DateTime;
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
    }
}
