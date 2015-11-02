using Android.App;
using Android.OS;
using Android.Views;
using Android.Widget;
using System;
using Android.Gms.Common.Apis;

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
            DescriptionTextView.Text = dataObject.Description;
            ProjectTextView.Text = dataObject.Project;
            Rebind();
            return view;
        }


        private void Rebind()
        {
            if (dataObject.IsRunning) {
                var dur = dataObject.GetDuration();
                DurationTextView.Text = TimeSpan.FromSeconds ((long)dur.TotalSeconds).ToString ();

                // Schedule next rebind:
                handler.RemoveCallbacks (Rebind);
                handler.PostDelayed (Rebind, 1000 - dur.Milliseconds);
            } else {
                DurationTextView.Visibility = ViewStates.Gone;
            }
        }
    }
}

