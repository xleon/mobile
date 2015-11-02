using System;
using Android.App;
using Android.OS;
using Android.Views;
using Android.Widget;

namespace Toggl.Chandler
{
    public class TimeEntryFragment : Fragment
    {
        private readonly SimpleTimeEntryData dataObject;
        private readonly Handler handler = new Handler ();
        private TextView DurationTextView;
        private TextView DescriptionTextView;
        private TextView ProjectTextView;

        public TimeEntryFragment (SimpleTimeEntryData data)
        {
            dataObject = data;
        }

        public override View OnCreateView (LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            var view = inflater.Inflate (Resource.Layout.TimeEntryFragment, container, false);

            DurationTextView = view.FindViewById<TextView> (Resource.Id.DurationTextView);
            DescriptionTextView = view.FindViewById<TextView> (Resource.Id.DescriptionTextView);
            ProjectTextView = view.FindViewById<TextView> (Resource.Id.ProjectTextView);

            Rebind();
            return view;
        }


        private void Rebind()
        {
            DescriptionTextView.Text = String.IsNullOrWhiteSpace (dataObject.Description) ? Resources.GetString (Resource.String.TimeEntryNoDescription) : dataObject.Description;
            ProjectTextView.Text = String.IsNullOrWhiteSpace (dataObject.Project) ? Resources.GetString (Resource.String.TimeEntryNoProject) : dataObject.Project;

            var dur = dataObject.GetDuration();
            DurationTextView.Text = TimeSpan.FromSeconds ((long)dur.TotalSeconds).ToString ();

            if (!dataObject.IsRunning) {
                return;
            }

            // Schedule next rebind:
            handler.RemoveCallbacks (Rebind);
            handler.PostDelayed (Rebind, 1000);
        }
    }
}

