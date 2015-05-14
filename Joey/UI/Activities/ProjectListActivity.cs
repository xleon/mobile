using System;
using System.Linq;
using System.Collections.Generic;
using Android.App;
using Android.Content.PM;
using Android.OS;
using Toggl.Joey.UI.Fragments;
using Toggl.Phoebe.Analytics;
using Toggl.Phoebe.Data.Models;
using Toggl.Phoebe.Data.Utils;
using XPlatUtils;
using Activity = Android.Support.V7.App.AppCompatActivity;
using Fragment = Android.Support.V4.App.Fragment;

namespace Toggl.Joey.UI.Activities
{
    [Activity (Label = "ProjectListActivity",
               ScreenOrientation = ScreenOrientation.Portrait,
               Theme = "@style/Theme.Toggl.App")]
    public class ProjectListActivity : BaseActivity
    {

        public static readonly string ExtraTimeEntriesIds = "com.toggl.timer.time_entries_ids";
        private ITimeEntryModel model;

        protected override async void OnCreateActivity (Bundle state)
        {
            base.OnCreateActivity (state);

            SetContentView (Resource.Layout.ProjectListActivityLayout);

            var extras = Intent.Extras;
            if (extras == null) {
                return;
            }

            var args = extras.GetStringArrayList (ExtraTimeEntriesIds);
            if (args.Count > 1) {
                model = await TimeEntryGroup.BuildTimeEntryGroupAsync (args);
            } else {
                model = new TimeEntryModel (args.First ());
            }

            if (model.Workspace == null || model.Workspace.Id == Guid.Empty) {
                Finish ();
            }

            if (model != null) {
                model.PropertyChanged += OnPropertyChange;
                SupportFragmentManager.BeginTransaction ()
                .Add (Resource.Id.ProjectListActivityLayout, new ProjectListFragment (model))
                .Commit ();
            }
        }

        protected override void OnStart ()
        {
            base.OnStart ();
            ServiceContainer.Resolve<ITracker> ().CurrentScreen = "Select Project";
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

