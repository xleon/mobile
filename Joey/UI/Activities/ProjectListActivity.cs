using System.Collections.Generic;
using System.Threading.Tasks;
using Android.App;
using Android.Content.PM;
using Android.OS;
using Toggl.Joey.UI.Fragments;
using Toggl.Phoebe.Data.DataObjects;
using Toggl.Phoebe.Data.Utils;
using Activity = Android.Support.V7.App.AppCompatActivity;
using Fragment = Android.Support.V4.App.Fragment;
using FragmentManager = Android.Support.V4.App.FragmentManager;

namespace Toggl.Joey.UI.Activities
{
    [Activity (Label = "ProjectListActivity",
               ScreenOrientation = ScreenOrientation.Portrait,
               Theme = "@style/Theme.Toggl.App")]
    public class ProjectListActivity : BaseActivity
    {
        public static readonly string ExtraTimeEntriesIds = "com.toggl.timer.time_entries_ids";
        private IList<TimeEntryData> timeEntryList;

        protected async override void OnCreateActivity (Bundle state)
        {
            base.OnCreateActivity (state);

            timeEntryList = await GetIntentTimeEntryData (Intent);
            if (timeEntryList.Count == 0) {
                Finish ();
            }

            SetContentView (Resource.Layout.ProjectListActivityLayout);
            SupportFragmentManager.BeginTransaction ()
            .Add (Resource.Id.ProjectListActivityLayout, new ProjectListFragment (timeEntryList))
            .Commit ();

        }

        public async static Task<IList<TimeEntryData>> GetIntentTimeEntryData (Android.Content.Intent intent)
        {
            var extras = intent.Extras;
            if (extras == null) {
                return new List<TimeEntryData> ();
            }

            // Get TimeEntryData from intent.
            var extraGuids = extras.GetStringArrayList (ExtraTimeEntriesIds);
            var timeEntryList = await TimeEntryGroup.GetTimeEntryDataList (extraGuids);
            return timeEntryList;
        }
    }
}

