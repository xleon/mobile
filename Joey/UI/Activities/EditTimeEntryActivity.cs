using System;
using System.Collections.Generic;
using System.Linq;
using Android.App;
using Android.Content.PM;
using Android.OS;
using Android.Views;
using Android.Widget;
using Toggl.Joey.UI.Fragments;
using Toggl.Phoebe.Analytics;
using Toggl.Phoebe.Data.Models;
using Toggl.Phoebe.Data.Utils;
using XPlatUtils;

namespace Toggl.Joey.UI.Activities
{
    [Activity (
         Exported = false,
         WindowSoftInputMode = SoftInput.StateHidden,
         ScreenOrientation = ScreenOrientation.Portrait,
         Theme = "@style/Theme.Toggl.App")]
    public class EditTimeEntryActivity : BaseActivity
    {
        public static readonly string ExtraTimeEntryId = "com.toggl.timer.time_entry_id";
        public static readonly string ExtraGroupedTimeEntriesGuids = "com.toggl.timer.grouped_time_entry_id";

        private FrameLayout DoneFrameLayout { get; set; }

        private TimeEntryModel model;
        private TimeEntryGroup group;

        protected override void OnCreateActivity (Bundle state)
        {
            base.OnCreateActivity (state);

            SetContentView (Resource.Layout.EditTimeEntryActivity);

            CreateModelFromIntent ();

            if (state == null) {
                if (model == null && group == null) {
                    Finish ();
                } else if (model != null) {
                    FragmentManager.BeginTransaction ()
                    .Add (Resource.Id.FrameLayout, new EditTimeEntryFragment (model))
                    .Commit ();
                } else if (group != null) {
                    FragmentManager.BeginTransaction ()
                    .Add (Resource.Id.FrameLayout, new GroupedEditTimeEntryFragment (group))
                    .Commit ();
                }
            }
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
                var extraGuids = extras.GetStringArray (ExtraGroupedTimeEntriesGuids);
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

        private View CreateDoneActionBarView ()
        {
            var inflater = (LayoutInflater)ActionBar.ThemedContext.GetSystemService (LayoutInflaterService);
            return inflater.Inflate (Resource.Layout.DoneActionBarView, null);
        }

        private void OnDoneFrameLayoutClick (object sender, EventArgs e)
        {
            Finish ();
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

        protected override void OnStart ()
        {
            base.OnStart ();
            ServiceContainer.Resolve<ITracker> ().CurrentScreen = "Edit Time Entry";
        }
    }
}
