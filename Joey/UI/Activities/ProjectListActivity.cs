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
        public static readonly string ExtraTimeEntriesIds = "com.toggl.timer.time_entries_ids";

        private TimeEntryModel model;
        private TimeEntryGroup group;

        protected override void OnCreateActivity (Bundle state)
        {
            base.OnCreateActivity (state);

            SetContentView (Resource.Layout.ProjectListActivityLayout);

            CreateModelFromIntent ();

            if (state == null) {
                if (model == null && group == null) {
                    Finish ();
                } else {
                    ProjectListFragment fragment;
                    if (model == null) {
                        fragment = new ProjectListFragment (group);
                    } else {
                        fragment = new ProjectListFragment (model);
                    }
                    SupportFragmentManager.BeginTransaction ()
                    .Add (Resource.Id.ProjectListActivityLayout, fragment)
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
                var extraGuids = extras.GetStringArray (ExtraTimeEntriesIds);
                var entriesToGroup = new List<TimeEntryModel> ();
                foreach (string guidString in extraGuids) {
                    var entry = new TimeEntryModel (new Guid (guidString));
                    await entry.LoadAsync ();
                    entriesToGroup.Add (entry);
                }
                group = new TimeEntryGroup (entriesToGroup [0].Data);
                foreach (var entry in entriesToGroup.Skip (1)) {
                    group.UpdateIfPossible (entry.Data);
                }
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

