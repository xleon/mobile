using System;
using Android.App;
using Android.Content.PM;
using Android.OS;
using Toggl.Joey.UI.Fragments;
using Activity = Android.Support.V7.App.AppCompatActivity;
using Fragment = Android.Support.V4.App.Fragment;

namespace Toggl.Joey.UI.Activities
{
    [Activity (Label = "NewProjectActivity",
               ScreenOrientation = ScreenOrientation.Portrait,
               Theme = "@style/Theme.Toggl.App")]
    public class NewProjectActivity : BaseActivity
    {
        public static readonly string ExtraWorkspaceId = "com.toggl.timer.workspace_id";
        public static readonly string ExtraProjectId = "com.toggl.timer.project_id";

        protected override void OnCreateActivity (Bundle state)
        {
            base.OnCreateActivity (state);

            SetContentView (Resource.Layout.NewProjectActivity);

            var workspaceId = GetWorkspaceData (Intent);

            SupportFragmentManager.BeginTransaction ()
            .Add (Resource.Id.NewProjectActivityLayout, new NewProjectFragment (workspaceId))
            .Commit ();
        }

        public static Guid GetWorkspaceData (Android.Content.Intent intent)
        {
            var extras = intent.Extras;
            if (extras == null) {
                return Guid.Empty;
            }

            // Get TimeEntryData from intent.
            var extraIdStr = extras.GetString (ExtraWorkspaceId);
            Guid extraGuid;
            Guid.TryParse (extraIdStr, out extraGuid);

            return extraGuid;
        }
    }
}

