using System;
using Android.App;
using Android.Content;
using Android.Graphics;
using Android.Graphics.Drawables;
using Android.OS;
using Android.Views;
using Android.Widget;
using Toggl.Chandler.UI.Activities;

namespace Toggl.Chandler.UI.Fragments
{
    public class TimerFragment : Fragment
    {
        private readonly string greenButtonColor = "#ee4dd965";
        private readonly string redButtonColor = "#eeff3c47";

        private readonly Handler handler = new Handler ();
        private TextView DurationTextView;
        private TextView DescriptionTextView;
        private TextView ProjectTextView;
        private ImageButton ActionButton;
        private bool userLoggedIn = true;
        private Context context;
        private MainActivity activity;

        public override View OnCreateView (LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            var view = inflater.Inflate (Resource.Layout.TimeEntryFragment, container, false);

            ActionButton = view.FindViewById<ImageButton> (Resource.Id.testButton);
            DurationTextView = view.FindViewById<TextView> (Resource.Id.DurationTextView);
            DescriptionTextView = view.FindViewById<TextView> (Resource.Id.DescriptionTextView);
            ProjectTextView = view.FindViewById<TextView> (Resource.Id.ProjectTextView);

            ActionButton.Click += OnActionButtonClicked;

            activity = ((MainActivity)Activity);
            context = Activity.ApplicationContext;

            Rebind ();

            return view;
        }

        public bool UserLoggedIn
        {
            get {
                return userLoggedIn;
            } set {
                userLoggedIn = value;
                Rebind();
            }
        }

        private SimpleTimeEntryData data
        {
            get {
                return activity.Data[0];
            }
        }

        private void OnActionButtonClicked (object sender, EventArgs e)
        {
            activity.RequestStartStop();
        }

        private void Rebind()
        {
            if (!IsAdded) {
                return;
            }

            if (activity.Data.Count != 0) {

                ActionButton.Visibility = ViewStates.Visible;
                if (data.IsRunning) {
                    ActionButton.SetImageDrawable (context.Resources.GetDrawable (Resource.Drawable.IcStop));
                    var dur = data.GetDuration();
                    DurationTextView.Text = TimeSpan.FromSeconds ((long)dur.TotalSeconds).ToString ();
                    DescriptionTextView.Text = String.IsNullOrWhiteSpace (data.Description) ? Resources.GetString (Resource.String.TimeEntryNoDescription) : data.Description;
                    ProjectTextView.Text = data.Project;
                } else {
                    ActionButton.SetImageDrawable (context.Resources.GetDrawable (Resource.Drawable.IcPlay));
                    DurationTextView.Text = Resources.GetString (Resource.String.DurationNotRunningState);
                    ProjectTextView.Text = Resources.GetString (Resource.String.TimerBlankIntroduction);;
                    DescriptionTextView.Text =  Resources.GetString (Resource.String.WearNewBlankDescription);;
                }

                var color = data.IsRunning ? Color.ParseColor (redButtonColor) : Color.ParseColor (greenButtonColor);
                var shape = ActionButton.Background as GradientDrawable;
                shape.SetColor (color);
            } else {
                ActionButton.SetImageDrawable (context.Resources.GetDrawable (Resource.Drawable.Icon));
                var shape = ActionButton.Background as GradientDrawable;
                var color = Color.Transparent;
                shape.SetColor (color);
                ProjectTextView.Text = String.Empty;
                if (!userLoggedIn) {
                    DurationTextView.Text = Resources.GetString (Resource.String.TimerWaiting);
                    DescriptionTextView.Text = Resources.GetString (Resource.String.TimerNotLoggedIn);
                } else {
                    DurationTextView.Text = Resources.GetString (Resource.String.TimerLoading);
                    DescriptionTextView.Text = String.Empty;
                }
            }
            // Schedule next rebind:
            handler.RemoveCallbacks (Rebind);
            handler.PostDelayed (Rebind, 1000);
        }
    }
}
