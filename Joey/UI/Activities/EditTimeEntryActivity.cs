using Android.App;
using Android.Content.PM;
using Android.OS;
using Android.Views;
using Toggl.Joey.UI.Fragments;
using System.Threading.Tasks;
using System.Collections.Generic;
using Toggl.Phoebe.Data.DataObjects;
using Toggl.Phoebe.Data.Utils;

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

        protected async override void OnCreateActivity (Bundle state)
        {
            base.OnCreateActivity (state);

            SetContentView (Resource.Layout.EditTimeEntryActivity);

            var timeEntryList = await GetIntentTimeEntryData (Intent);
            TimeEntryData timeEntry = null;
            if (timeEntryList.Count > 0) {
                timeEntry = timeEntryList[0];
            }

            var isGrouped = Intent.Extras.GetBoolean (IsGrouped, false);
            if (isGrouped)
                FragmentManager.BeginTransaction ()
                .Add (Resource.Id.FrameLayout, new EditGroupedTimeEntryFragment (timeEntryList))
                .Commit ();
            else
                FragmentManager.BeginTransaction ()
                .Add (Resource.Id.FrameLayout, new EditTimeEntryFragment (timeEntry))
                .Commit ();
        }

        public async static Task<IList<TimeEntryData>> GetIntentTimeEntryData (Android.Content.Intent intent)
        {
            var extras = intent.Extras;
            if (extras == null) {
                return new List<TimeEntryData> ();
            }

            // Get TimeEntryData from intent.
            var extraGuids = extras.GetStringArrayList (ExtraGroupedTimeEntriesGuids);
            var timeEntryList = await TimeEntryGroup.GetTimeEntryDataList (extraGuids);
            return timeEntryList;
        }
    }
}
