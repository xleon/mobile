using System;
using Android.OS;
using Android.Views;
using Android.Widget;
using Fragment = Android.Support.V4.App.Fragment;

namespace Toggl.Joey.UI.Fragments
{
    public class CurrentTimeEntryEditFragment : Fragment
    {
        protected TextView DurationTextView { get; private set; }

        protected EditText StartTimeEditText { get; private set; }

        protected EditText StopTimeEditText { get; private set; }

        protected EditText DateEditText { get; private set; }

        protected EditText DescriptionEditText { get; private set; }

        protected EditText ProjectEditText { get; private set; }

        protected EditText TagsEditText { get; private set; }

        protected CheckBox BillableCheckBox { get; private set; }

        protected Button DeleteButton { get; private set; }

        public override View OnCreateView (LayoutInflater inflater, ViewGroup container, Bundle state)
        {
            var view = inflater.Inflate (Resource.Layout.CurrentTimeEntryEditFragment, container, false);

            DurationTextView = view.FindViewById<TextView> (Resource.Id.DurationTextView);
            StartTimeEditText = view.FindViewById<EditText> (Resource.Id.StartTimeEditText);
            StopTimeEditText = view.FindViewById<EditText> (Resource.Id.StopTimeEditText);
            DateEditText = view.FindViewById<EditText> (Resource.Id.DateEditText);
            DescriptionEditText = view.FindViewById<EditText> (Resource.Id.DescriptionEditText);
            ProjectEditText = view.FindViewById<EditText> (Resource.Id.ProjectEditText);
            TagsEditText = view.FindViewById<EditText> (Resource.Id.TagsEditText);
            BillableCheckBox = view.FindViewById<CheckBox> (Resource.Id.BillableCheckBox);
            DeleteButton = view.FindViewById<Button> (Resource.Id.DeleteButton);

            return view;
        }
    }
}
