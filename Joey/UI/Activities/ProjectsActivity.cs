using Android.App;
using Android.OS;
using Toggl.Joey.UI.Fragments;
using Activity = Android.Support.V7.App.ActionBarActivity;
using Fragment = Android.Support.V4.App.Fragment;

namespace Toggl.Joey.UI.Activities
{
    [Activity (Label = "ProjectsActivity")]
    public class ProjectsActivity : Activity
    {

        protected override void OnCreate (Bundle bundle)
        {
            SetTheme (Resource.Style.Theme_AppCompat_Light_NoActionBar);

            base.OnCreate (bundle);

            SetContentView (Resource.Layout.ProjectActivity);

            SupportFragmentManager.BeginTransaction ()
            .Add (Resource.Id.ProjectActivityLayout, new ProjectsFragment())
            .Commit ();
        }
    }
}

