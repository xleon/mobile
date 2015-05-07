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
        public ITimeEntryModel Model { get; set; }

        protected override void OnCreateActivity (Bundle state)
        {
            base.OnCreateActivity (state);

            SetContentView (Resource.Layout.ProjectListActivityLayout);

            if (NavBridge.FinishedNav != null) {
                NavBridge.FinishedNav (this);
                NavBridge.FinishedNav = null;
            }

            if (Model != null) {
                Model.PropertyChanged += OnPropertyChange;
                SupportFragmentManager.BeginTransaction ()
                .Add (Resource.Id.ProjectListActivityLayout, new ProjectListFragment (Model))
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
            if (Model.Id == Guid.Empty) {
                Finish ();
            }
        }
    }
}

