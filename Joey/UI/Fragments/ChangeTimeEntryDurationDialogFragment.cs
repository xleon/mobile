using System;
using Android.App;
using Android.Content;
using Android.Graphics;
using Android.OS;
using Android.Text;
using Android.Text.Style;
using Android.Views;
using Android.Widget;
using Toggl.Phoebe.Data;
using Toggl.Phoebe.Data.Models;
using Toggl.Joey.UI.Utils;
using Toggl.Joey.UI.Views;

namespace Toggl.Joey.UI.Fragments
{
    public class ChangeTimeEntryDurationDialogFragment : BaseDialogFragment
    {
        private static readonly string TimeEntryIdArgument = "com.toggl.timer.time_entry_id";

        public ChangeTimeEntryDurationDialogFragment (TimeEntryModel model) : base ()
        {
            var args = new Bundle ();
            args.PutString (TimeEntryIdArgument, model.Id.ToString ());

            Arguments = args;
        }

        public ChangeTimeEntryDurationDialogFragment ()
        {
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

        protected TextView DurationTextView { get; private set; }

        protected ImageButton DeleteImageButton { get; private set; }

        protected Button Add5Button { get; private set; }

        protected Button Add30Button { get; private set; }

        protected Button OkButton { get; private set; }

        private readonly Button[] numButtons = new Button[10];
        private TimeEntryModel model;
        private Duration oldDuration;
        private Duration newDuration;
        private int digitsEntered;

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
            if (model != null) {
                oldDuration = model.GetDuration ();
            } else {
                oldDuration = Duration.Zero;
            }

            var view = LayoutInflater.From (Activity)
                .Inflate (Resource.Layout.ChangeTimeEntryDurationDialogFragment, null);
            DurationTextView = view.FindViewById<TextView> (Resource.Id.DurationTextView).SetFont (Font.Roboto);
            DeleteImageButton = view.FindViewById<ImageButton> (Resource.Id.DeleteImageButton);
            Add5Button = view.FindViewById<Button> (Resource.Id.Add5Button).SetFont (Font.RobotoLight);
            Add30Button = view.FindViewById<Button> (Resource.Id.Add30Button).SetFont (Font.RobotoLight);
            numButtons [0] = view.FindViewById<Button> (Resource.Id.Num0Button).SetFont (Font.RobotoLight);
            numButtons [1] = view.FindViewById<Button> (Resource.Id.Num1Button).SetFont (Font.RobotoLight);
            numButtons [2] = view.FindViewById<Button> (Resource.Id.Num2Button).SetFont (Font.RobotoLight);
            numButtons [3] = view.FindViewById<Button> (Resource.Id.Num3Button).SetFont (Font.RobotoLight);
            numButtons [4] = view.FindViewById<Button> (Resource.Id.Num4Button).SetFont (Font.RobotoLight);
            numButtons [5] = view.FindViewById<Button> (Resource.Id.Num5Button).SetFont (Font.RobotoLight);
            numButtons [6] = view.FindViewById<Button> (Resource.Id.Num6Button).SetFont (Font.RobotoLight);
            numButtons [7] = view.FindViewById<Button> (Resource.Id.Num7Button).SetFont (Font.RobotoLight);
            numButtons [8] = view.FindViewById<Button> (Resource.Id.Num8Button).SetFont (Font.RobotoLight);
            numButtons [9] = view.FindViewById<Button> (Resource.Id.Num9Button).SetFont (Font.RobotoLight);

            DeleteImageButton.Click += OnDeleteImageButtonClick;
            DeleteImageButton.LongClick += OnDeleteImageButtonLongClick;
            foreach (var numButton in numButtons) {
                numButton.Click += OnNumButtonClick;
            }
            Add5Button.Click += OnAdd5ButtonClick;
            Add30Button.Click += OnAdd30ButtonClick;

            return new AlertDialog.Builder (Activity)
                .SetTitle (Resource.String.ChangeTimeEntryDurationDialogTitle)
                .SetView (view)
                .SetNegativeButton (Resource.String.ChangeTimeEntryDurationDialogCancel, OnCancelClicked)
                .SetPositiveButton (Resource.String.ChangeTimeEntryDurationDialogOk, OnOkClicked)
                .Create ();
        }

        public override void OnStart ()
        {
            base.OnStart ();

            // AlertDialog buttons aren't available earlier:
            var dia = (AlertDialog)Dialog;
            OkButton = dia.GetButton ((int)DialogButtonType.Positive);

            Rebind ();
        }

        private void Rebind ()
        {
            var durationShown = digitsEntered > 0 ? newDuration : oldDuration;
            var durationText = durationShown.ToString ();
            var durationSpannable = new SpannableString (durationText);
            durationSpannable.SetSpan (
                new ForegroundColorSpan (Color.LightGray),
                0, durationSpannable.Length (),
                SpanTypes.InclusiveExclusive);
            if (digitsEntered > 0) {
                // Divider
                var sepIdx = durationText.IndexOf (":", StringComparison.Ordinal);
                if (sepIdx >= 0) {
                    durationSpannable.SetSpan (
                        new ForegroundColorSpan (Color.Black),
                        sepIdx, sepIdx + 1,
                        SpanTypes.InclusiveExclusive);
                }
                // Color entered minutes
                durationSpannable.SetSpan (
                    new ForegroundColorSpan (Color.Black),
                    durationText.Length - (Math.Min (digitsEntered, 2)), durationText.Length,
                    SpanTypes.InclusiveExclusive);
                // Color entered hours
                if (digitsEntered > 2) {
                    durationSpannable.SetSpan (
                        new ForegroundColorSpan (Color.Black),
                        2 - (digitsEntered - 2), 2,
                        SpanTypes.InclusiveExclusive);
                }
            }
            DurationTextView.SetText (durationSpannable, TextView.BufferType.Spannable);

            int num = 0;
            foreach (var numButton in numButtons) {
                numButton.Enabled = digitsEntered < 4;
                num += 1;
            }

            DeleteImageButton.Enabled = digitsEntered > 0;
            Add5Button.Enabled = newDuration.IsValid && newDuration.AddMinutes (5).IsValid;
            Add30Button.Enabled = newDuration.IsValid && newDuration.AddMinutes (30).IsValid;
            OkButton.Enabled = digitsEntered > 0 && newDuration.IsValid;
        }

        private void OnDeleteImageButtonClick (object sender, EventArgs e)
        {
            if (digitsEntered < 1)
                return;
            newDuration = newDuration.RemoveDigit ();
            if (newDuration == Duration.Zero) {
                // Clear all entered digits, is no value remains
                digitsEntered = 0;
            } else {
                digitsEntered -= 1;
            }

            Rebind ();
        }

        void OnDeleteImageButtonLongClick (object sender, View.LongClickEventArgs e)
        {
            newDuration = Duration.Zero;
            digitsEntered = 0;
            Rebind ();
        }

        private void OnNumButtonClick (object sender, EventArgs e)
        {
            if (digitsEntered > 3)
                return;

            int num = Array.IndexOf (numButtons, sender);
            if (num < 0)
                return;

            newDuration = newDuration.AppendDigit (num);
            digitsEntered += 1;
            Rebind ();
        }

        private void OnAdd5ButtonClick (object sender, EventArgs e)
        {
            var duration = newDuration.AddMinutes (5);
            if (!duration.IsValid)
                return;

            newDuration = duration;
            digitsEntered = 4;
            Rebind ();
        }

        private void OnAdd30ButtonClick (object sender, EventArgs e)
        {
            var duration = newDuration.AddMinutes (30);
            if (!duration.IsValid)
                return;

            newDuration = duration;
            digitsEntered = 4;
            Rebind ();
        }

        private void OnCancelClicked (object sender, DialogClickEventArgs e)
        {
            Dismiss ();
        }

        private void OnOkClicked (object sender, DialogClickEventArgs e)
        {
            if (model != null) {
                var duration = model.GetDuration ();
                if (model.State == TimeEntryState.New) {
                    duration = new TimeSpan (newDuration.Hours, newDuration.Minutes, 0);
                } else {
                    // Keep the current seconds and milliseconds
                    duration = new TimeSpan (0, newDuration.Hours, newDuration.Minutes, duration.Seconds, duration.Milliseconds);
                }
                model.SetDuration (duration);
            }
            Dismiss ();
        }

        private struct Duration
        {
            private readonly int encoded;
            private readonly int digits;

            public Duration (int hours, int minutes) : this (hours * 100 + minutes)
            {
            }

            private Duration (int encoded)
            {
                this.encoded = encoded;
                this.digits = GetDigits (encoded);
            }

            public Duration RemoveDigit ()
            {
                return new Duration (encoded / 10);
            }

            public Duration AppendDigit (int digit)
            {
                return new Duration (encoded * 10 + digit);
            }

            public Duration AddMinutes (int minutes)
            {
                minutes += Minutes;
                var hours = Hours + minutes / 60;
                minutes = minutes % 60;
                return new Duration (hours, minutes);
            }

            public override bool Equals (object obj)
            {
                return obj is Duration && this == (Duration)obj;
            }

            public override int GetHashCode ()
            {
                return encoded.GetHashCode ();
            }

            public override string ToString ()
            {
                return String.Format ("{0:00}:{1:00}", Hours, Minutes);
            }

            public bool IsValid {
                get { return Minutes < 60 && Digits < 5; }
            }

            public int Digits {
                get { return digits; }
            }

            public int Hours {
                get { return encoded / 100; }
            }

            public int Minutes {
                get { return encoded % 100; }
            }

            private static int GetDigits (int encoded)
            {
                int digits = 0;
                while (encoded > 0) {
                    digits += 1;
                    encoded /= 10;
                }
                return digits;
            }

            public static bool operator == (Duration left, Duration right)
            {
                return left.encoded == right.encoded;
            }

            public static bool operator != (Duration left, Duration right)
            {
                return !(left == right);
            }

            public static implicit operator Duration (TimeSpan timespan)
            {
                return new Duration (timespan.Hours, timespan.Minutes);
            }

            public static Duration Zero {
                get { return new Duration (); }
            }
        }
    }
}

