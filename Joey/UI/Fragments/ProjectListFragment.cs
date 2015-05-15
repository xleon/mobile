using System;
using System.Collections.Generic;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Support.V7.App;
using Android.Support.V7.Widget;
using Android.Views;
using Toggl.Joey.UI.Activities;
using Toggl.Joey.UI.Adapters;
using Toggl.Joey.UI.Views;
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
        private ProjectListView viewModel;

        public ProjectListFragment ()
        {
        }

        public ProjectListFragment (IntPtr jref, Android.Runtime.JniHandleOwnership xfer) : base (jref, xfer)
        {
        }

        public override View OnCreateView (LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            var view = inflater.Inflate (Resource.Layout.ProjectListFragment, container, false);

            recyclerView = view.FindViewById<RecyclerView> (Resource.Id.ProjectListRecyclerView);
            recyclerView.SetLayoutManager (new LinearLayoutManager (Activity));
            recyclerView.AddItemDecoration (new ShadowItemDecoration<ProjectListAdapter.ProjectListItemHolder, ProjectListAdapter.NoProjectListItemHolder> (Activity, true));

            recyclerView.AddItemDecoration (new DividerItemDecoration (Activity, DividerItemDecoration.VerticalList));

            var activity = (AppCompatActivity)Activity;
            var toolbar = view.FindViewById<Toolbar> (Resource.Id.ProjectListToolbar);
            activity.SetSupportActionBar (toolbar);

            var actionBar = activity.SupportActionBar;
            actionBar.SetDisplayHomeAsUpEnabled (true);
            actionBar.SetTitle (Resource.String.ChooseTimeEntryProjectDialogTitle);
            HasOptionsMenu = true;

            return view;
        }

        public override void OnViewCreated (View view, Bundle savedInstanceState)
        {
            base.OnViewCreated (view, savedInstanceState);

            var extras = Intent.Extras;
            IList<string> extraGuids;

            if (extras == null) {
                Activity.Finish ();
            } else {
                extraGuids = extras.GetStringArrayList (ProjectListActivity.ExtraTimeEntriesIds);
            }

            viewModel = (extraGuids.Count > 0) ? new ProjectListView (extraGuids) : new ProjectListView (extraGuids[0]);
            viewModel.Init ();

            // set list adapter
            var adapter = new ProjectListAdapter (recyclerView, viewModel);
            recyclerView.SetAdapter (adapter);
            adapter.HandleProjectSelection = OnItemSelected;
        }

        private async void OnItemSelected (object m)
        {
            if (viewModel.Model != null) {
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
                    await viewModel.SaveModelAsync (project, workspace);
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
                    Guid.TryParse (data.Extras.GetString (NewProjectActivity.ExtraProjectId), out extraGuid);

                    await viewModel.SaveModelAsync (new ProjectModel (extraGuid), new WorkspaceModel (extraGuid));
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
                viewModel.Dispose ();
            }
            base.Dispose (disposing);
        }
    }
}

