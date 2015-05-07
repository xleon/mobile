using System;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Support.V7.App;
using Android.Support.V7.Widget;
using Android.Views;
using Toggl.Joey.UI.Activities;
using Toggl.Joey.UI.Adapters;
using Toggl.Joey.UI.Views;
using Toggl.Phoebe.Data.Utils;
using Toggl.Phoebe.Data.Models;
using Toggl.Phoebe.Data.Views;
using ActionBar = Android.Support.V7.App.ActionBar;
using Activity = Android.Support.V7.App.AppCompatActivity;
using Fragment = Android.Support.V4.App.Fragment;
using Toolbar = Android.Support.V7.Widget.Toolbar;

namespace Toggl.Joey.UI.Fragments
{
    public class ProjectListFragment : Fragment
    {
        private static readonly int ProjectCreatedRequestCode = 1;

        private RecyclerView recyclerView;
        private ProjectListAdapter adapter;
        private ITimeEntryModel model;

        public ProjectListFragment ()
        {
        }

        public ProjectListFragment (IntPtr jref, Android.Runtime.JniHandleOwnership xfer) : base (jref, xfer)
        {
        }

        public ProjectListFragment (ITimeEntryModel model)
        {
            this.model = model;
        }

        public override View OnCreateView (LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            var view = inflater.Inflate (Resource.Layout.ProjectListFragment, container, false);

            recyclerView = view.FindViewById<RecyclerView> (Resource.Id.ProjectListRecyclerView);
            recyclerView.SetLayoutManager (new LinearLayoutManager (Activity));
            recyclerView.AddItemDecoration (new ShadowItemDecoration<ProjectListAdapter.ProjectListItemHolder, ProjectListAdapter.NoProjectListItemHolder> (Activity, true));

            recyclerView.AddItemDecoration (new DividerItemDecoration (Activity, DividerItemDecoration.VerticalList));
            adapter = new ProjectListAdapter (recyclerView);
            recyclerView.SetAdapter (adapter);
            adapter.HandleProjectSelection = OnItemSelected;

            var activity = (AppCompatActivity)Activity;
            var toolbar = view.FindViewById<Toolbar> (Resource.Id.ProjectListToolbar);
            activity.SetSupportActionBar (toolbar);

            var actionBar = activity.SupportActionBar;
            actionBar.SetDisplayHomeAsUpEnabled (true);
            actionBar.SetTitle (Resource.String.ChooseTimeEntryProjectDialogTitle);
            HasOptionsMenu = true;

            return view;
        }

        private async void OnItemSelected (object m)
        {
            if (model != null) {
                ProjectModel project = null;
                WorkspaceModel workspace = null;

                if (m is ProjectListView.Project) {
                    var wrap = (ProjectListView.Project)m;
                    if (wrap.IsNoProject) {
                        workspace = new WorkspaceModel (wrap.WorkspaceId);
                    } else if (wrap.IsNewProject) {
                        var data = wrap.Data;
                        var ws = new WorkspaceModel (data.WorkspaceId);

                        // Show create project activity instead
                        var intent = new Intent (Activity, typeof (NewProjectActivity));
                        intent.PutExtra (NewProjectActivity.ExtraWorkspaceId, ws.Id.ToString());
                        StartActivityForResult (intent, ProjectCreatedRequestCode);
                    } else {
                        project = (ProjectModel)wrap.Data;
                        workspace = project.Workspace;
                    }
                } else if (m is ProjectAndTaskView.Workspace) {
                    var wrap = (ProjectAndTaskView.Workspace)m;
                    workspace = (WorkspaceModel)wrap.Data;
                }

                if (project != null || workspace != null) {
                    model.Workspace = workspace;
                    model.Project = project;
                    await model.SaveAsync ();
                    Activity.Finish ();
                }
            }
        }

        public override bool OnOptionsItemSelected (IMenuItem item)
        {
            if (item.ItemId == Android.Resource.Id.Home) {
                Activity.OnBackPressed ();
            }
            return base.OnOptionsItemSelected (item);
        }

        public async override void OnActivityResult (int requestCode, int resultCode, Intent data)
        {
            base.OnActivityResult (requestCode, resultCode, data);
            if (requestCode == ProjectCreatedRequestCode) {
                if (resultCode == (int)Result.Ok) {
                    Guid extraGuid;
                    Guid.TryParse (data.Extras.GetString (NewProjectActivity.ExtraWorkspaceId), out extraGuid);
                    model.Workspace = new WorkspaceModel (extraGuid);
                    Guid.TryParse (data.Extras.GetString (NewProjectActivity.ExtraProjectId), out extraGuid);
                    model.Project = new ProjectModel (extraGuid);

                    await model.SaveAsync ();
                    Activity.Finish();
                }

            }
        }

        public override void OnDestroy ()
        {
            base.OnDestroy ();
            Dispose (true);
        }

        protected override void Dispose (bool disposing)
        {
            if (disposing) {
                adapter.Dispose ();
            }
            base.Dispose (disposing);
        }
    }
}

