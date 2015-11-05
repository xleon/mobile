using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Android.App;
using Android.Gms.Wearable;
using Android.OS;
using Android.Views;
using Android.Widget;
using Toggl.Chandler.UI.Activities;

namespace Toggl.Chandler.UI.Fragments
{
    public class TimerFragment : Fragment
    {
        private readonly Handler handler = new Handler ();
        private TextView DurationTextView;
        private TextView DescriptionTextView;
        private TextView ProjectTextView;
        private ImageButton ActionButton;

        public override View OnCreateView (LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            var view = inflater.Inflate (Resource.Layout.TimeEntryFragment, container, false);

            ActionButton = view.FindViewById<ImageButton> (Resource.Id.testButton);
            DurationTextView = view.FindViewById<TextView> (Resource.Id.DurationTextView);
            DescriptionTextView = view.FindViewById<TextView> (Resource.Id.DescriptionTextView);
            ProjectTextView = view.FindViewById<TextView> (Resource.Id.ProjectTextView);

            ActionButton.Click += OnActionButtonClicked;
            ((MainActivity)Activity).CollectionChanged += OnCollectionChanged;

//            Rebind();
            return view;
        }

        private SimpleTimeEntryData data
        {
            get {
                if (((MainActivity)Activity).Data[0] != null) {
                    return ((MainActivity)Activity).Data[0];
                } else {
                    return new SimpleTimeEntryData {
                        Description = "empty",
                        Project = "project",
                        IsRunning = false,
                        StartTime = DateTime.UtcNow,
                        StopTime = DateTime.UtcNow
                    };
                }
            }
        }
        private void OnCollectionChanged (object sender, EventArgs e)
        {
            Rebind();
        }

        private void OnActionButtonClicked (object sender, EventArgs e)
        {
            ((MainActivity)Activity).RequestSync();

        }

        private void SendStartStopMessage ()
        {

        }

        private void Rebind()
        {
            DescriptionTextView.Text = String.IsNullOrWhiteSpace (data.Description) ? Resources.GetString (Resource.String.TimeEntryNoDescription) : data.Description;
            ProjectTextView.Text = String.IsNullOrWhiteSpace (data.Project) ? Resources.GetString (Resource.String.TimeEntryNoProject) : data.Project;

            var dur = data.GetDuration();
            DurationTextView.Text = TimeSpan.FromSeconds ((long)dur.TotalSeconds).ToString ();

            if (!data.IsRunning) {
                return;
            }

            // Schedule next rebind:
            handler.RemoveCallbacks (Rebind);
            handler.PostDelayed (Rebind, 1000);
        }
    }
}

