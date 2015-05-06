using Android.App;
using Android.Content.PM;
using Android.OS;
using Android.Views;
using Toggl.Joey.UI.Fragments;

namespace Toggl.Joey.UI.Activities
{
    [Activity (
         Exported = false,
         WindowSoftInputMode = SoftInput.StateHidden,
         ScreenOrientation = ScreenOrientation.Portrait,
         Theme = "@style/Theme.Toggl.App")]
    public class EditTimeEntryActivity : BaseActivity
    {
        public static readonly string IsGrouped = "com.toggl.timer.grouped_edit";
        public static readonly string ExtraTimeEntryId = "com.toggl.timer.time_entry_id";
        public static readonly string ExtraGroupedTimeEntriesGuids = "com.toggl.timer.grouped_time_entry_id";

        protected override void OnCreateActivity (Bundle state)
        {
            base.OnCreateActivity (state);
            SetContentView (Resource.Layout.EditTimeEntryActivity);

            var isGrouped = Intent.Extras.GetBoolean (IsGrouped, false);
            if (isGrouped)
                FragmentManager.BeginTransaction ()
                .Add (Resource.Id.FrameLayout, new EditGroupedTimeEntryFragment ())
                .Commit ();
            else
                FragmentManager.BeginTransaction ()
                .Add (Resource.Id.FrameLayout, new EditTimeEntryFragment ())
                .Commit ();
        }
    }
}
