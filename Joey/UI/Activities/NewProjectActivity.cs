using System;
using Android.App;
using Android.OS;
using Toggl.Joey.UI.Fragments;
using Toggl.Phoebe.Data.Models;

using Activity = Android.Support.V7.App.ActionBarActivity;
using Fragment = Android.Support.V4.App.Fragment;

namespace Toggl.Joey.UI.Activities
{
    [Activity (Label = "NewProjectActivity")]
    public class NewProjectActivity : Activity
    {
        public static readonly string ExtraWorkspaceId = "com.toggl.timer.workspace_id";

        private WorkspaceModel workspace;

        protected override void OnCreate (Bundle bundle)
        {
            SetTheme (Resource.Style.Theme_AppCompat_Light_NoActionBar);

            base.OnCreate (bundle);

            SetContentView (Resource.Layout.NewProjectActivity);

            CreateModelFromIntent ();

            SupportFragmentManager.BeginTransaction ()
            .Add (Resource.Id.NewProjectActivityLayout, new NewProjectFragment (workspace))
            .Commit ();
        }

        private async void CreateModelFromIntent ()
        {
            var extras = Intent.Extras;
            if (extras == null) {
                return;
            }

            var extraIdStr = extras.GetString (ExtraWorkspaceId);
            Guid extraGuid;
            Guid.TryParse (extraIdStr, out extraGuid);

            workspace = new WorkspaceModel (extraGuid);
            workspace.PropertyChanged += OnPropertyChange;
            await workspace.LoadAsync ();

            if (workspace == null) {
                Finish ();
            }
        }

        private void OnPropertyChange (object sender, EventArgs e)
        {
            // Protect against Java side being GCed
            if (Handle == IntPtr.Zero) {
                return;
            }
            if (workspace.Id == Guid.Empty) {
                Finish ();
            }
        }
    }
}

