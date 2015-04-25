using System;
using Android.App;
using Android.Content.PM;
using Android.OS;
using Toggl.Joey.UI.Fragments;
using Toggl.Phoebe.Analytics;
using Toggl.Phoebe.Data.Models;
using XPlatUtils;
using Activity = Android.Support.V7.App.ActionBarActivity;
using Fragment = Android.Support.V4.App.Fragment;

namespace Toggl.Joey.UI.Activities
{
    [Activity (Label = "ProjectListActivity",
               ScreenOrientation = ScreenOrientation.Portrait,
               Theme = "@style/Theme.Toggl.App")]
    public class ProjectListActivity : BaseActivity
    {
        public static readonly string ExtraTimeEntryId = "com.toggl.timer.time_entry_id";
        private TimeEntryModel model;

        protected override void OnCreateActivity (Bundle state)
        {
            base.OnCreateActivity (state);

            SetContentView (Resource.Layout.ProjectListActivityLayout);

            CreateModelFromIntent ();

            if (state == null) {
                if (model == null) {
                    Finish ();
                } else {
                    SupportFragmentManager.BeginTransaction ()
                    .Add (Resource.Id.ProjectListActivityLayout, new ProjectListFragment (model))
                    .Commit ();
                }
            }
        }

        protected override void OnStart ()
        {
            base.OnStart ();
            ServiceContainer.Resolve<ITracker> ().CurrentScreen = "Select Project";
        }

        private async void CreateModelFromIntent ()
        {
            var extras = Intent.Extras;
            if (extras == null) {
                return;
            }

            var extraIdStr = extras.GetString (ExtraTimeEntryId);
            Guid extraId;
            if (!Guid.TryParse (extraIdStr, out extraId)) {
                return;
            }

            model = new TimeEntryModel (extraId);
            model.PropertyChanged += OnPropertyChange;

            // Ensure that the model exists
            await model.LoadAsync ();
            if (model.Workspace == null || model.Workspace.Id == Guid.Empty) {
                Finish ();
            }
        }

        private void OnPropertyChange (object sender, EventArgs e)
        {
            // Protect against Java side being GCed
            if (Handle == IntPtr.Zero) {
                return;
            }
            if (model.Id == Guid.Empty) {
                Finish ();
            }
        }
    }
}

