using System;
using Android.App;
using Android.OS;
using Android.Views;
using Android.Widget;
using Toggl.Phoebe;
using Toggl.Phoebe.Analytics;
using Toggl.Phoebe.Data;
using Toggl.Phoebe.Data.Models;
using XPlatUtils;
using Toggl.Joey.UI.Fragments;
using Android.Graphics.Drawables;

namespace Toggl.Joey.UI.Activities
{
    [Activity (
         Exported = false,
         WindowSoftInputMode = SoftInput.StateHidden)]
    public class EditTimeEntryActivity : BaseActivity
    {
        public static readonly string ExtraTimeEntryId = "com.toggl.timer.time_entry_id";

        private FrameLayout DoneFrameLayout { get; set; }

        private TimeEntryModel model;

        protected override void OnCreateActivity (Bundle state)
        {
            base.OnCreateActivity (state);

            ActionBar.Hide ();

            SetContentView (Resource.Layout.EditTimeEntryActivity);

            CreateModelFromIntent ();

            if (state == null) {
                if (model == null) {
                    Finish ();
                } else {
                    FragmentManager.BeginTransaction ()
                    .Add (Resource.Id.FrameLayout, new EditTimeEntryFragment (model))
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
