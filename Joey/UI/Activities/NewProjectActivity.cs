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
        public static readonly string WorkspaceIdArgument = "com.toggl.timer.workspace_id";

        // Explanation of native constructor
        // http://stackoverflow.com/questions/10593022/monodroid-error-when-calling-constructor-of-custom-view-twodscrollview/10603714#10603714
        public NewProjectActivity (System.IntPtr a, Android.Runtime.JniHandleOwnership b) : base (a, b)
        {
        }

        public NewProjectActivity ()
        {
        }

        protected override void OnCreateActivity (Bundle state)
        {
            base.OnCreateActivity (state);
            SetContentView (Resource.Layout.NewProjectActivity);

            var extras = Intent.Extras;
            if (extras == null) {
                Finish ();
            }

            var workspaceId = extras.GetString (WorkspaceIdArgument);
            var fragment = ProjectListFragment.NewInstance (workspaceId);
            FragmentManager.BeginTransaction ()
            .Add (Resource.Id.NewProjectActivityLayout, NewProjectFragment.NewInstance (workspaceId))
            .Commit ();
        }
    }
}

