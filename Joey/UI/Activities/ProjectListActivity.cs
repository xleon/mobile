using Android.App;
using Android.Content.PM;
using Android.OS;
using Toggl.Joey.UI.Fragments;
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

        protected override async void OnCreateActivity (Bundle state)
        {
            base.OnCreateActivity (state);

            SetContentView (Resource.Layout.ProjectListActivityLayout);
            SupportFragmentManager.BeginTransaction ()
            .Add (Resource.Id.ProjectListActivityLayout, new ProjectListFragment ())
            .Commit ();
        }
    }
}

