using System.Collections.Generic;
using System.Threading.Tasks;
using Android.App;
using Android.Content.PM;
using Android.OS;
using Toggl.Joey.UI.Fragments;
using Toggl.Phoebe.Data.DataObjects;
using Toggl.Phoebe.Data.Utils;
using Activity = Android.Support.V7.App.AppCompatActivity;
using Fragment = Android.App.Fragment;
using FragmentManager = Android.App.FragmentManager;

namespace Toggl.Joey.UI.Activities
{
    [Activity (Label = "ProjectListActivity",
               ScreenOrientation = ScreenOrientation.Portrait,
               Theme = "@style/Theme.Toggl.App")]
    public class ProjectListActivity : BaseActivity
    {
        private static readonly string fragmentTag = "projectlist_fragment";
        public static readonly string ExtraTimeEntriesIds = "com.toggl.timer.time_entries_ids";
        private IList<TimeEntryData> timeEntryList;

        protected async override void OnCreateActivity (Bundle state)
        {
            base.OnCreateActivity (state);
            SetContentView (Resource.Layout.ProjectListActivityLayout);

            // Check if fragment is still in Fragment manager.
            var fragment = FragmentManager.FindFragmentByTag (fragmentTag);

            if (fragment != null) {
                FragmentManager.BeginTransaction ()
                .Attach (fragment)
                .Commit ();
            } else {
                timeEntryList = await GetIntentTimeEntryData (Intent);
                if (timeEntryList.Count == 0) {
                    Finish ();
                }

                fragment = new ProjectListFragment (timeEntryList);
                FragmentManager.BeginTransaction ()
                .Add (Resource.Id.ProjectListActivityLayout, fragment)
                .Commit ();
            }
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

