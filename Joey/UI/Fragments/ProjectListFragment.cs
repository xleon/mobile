using System;
using System.Collections.Generic;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Support.Design.Widget;
using Android.Support.V7.Widget;
using Android.Views;
using Android.Widget;
using Toggl.Joey.UI.Activities;
using Toggl.Joey.UI.Adapters;
using Toggl.Joey.UI.Views;
using Toggl.Phoebe.Data.DataObjects;
using Toggl.Phoebe.Data.Models;
using Toggl.Phoebe.Data.ViewModels;
using Toggl.Phoebe.Data.Views;
using ActionBar = Android.Support.V7.App.ActionBar;
using Activity = Android.Support.V7.App.AppCompatActivity;
using Fragment = Android.Support.V4.App.Fragment;
using Toolbar = Android.Support.V7.Widget.Toolbar;

namespace Toggl.Joey.UI.Fragments
{
    public class ProjectListFragment : Fragment, AppBarLayout.IOnOffsetChangedListener, Toolbar.IOnMenuItemClickListener, TabLayout.IOnTabSelectedListener
    {
        private static readonly int ProjectCreatedRequestCode = 1;

        private RecyclerView recyclerView;
        private TabLayout tabLayout;
        private TogglAppBar appBar;
        private Toolbar Toolbar;
        private FloatingActionButton fab;
        private LinearLayout emptyStateLayout;
        private ProjectListViewModel viewModel;
        private IList<TimeEntryData> timeEntryList;
        private bool listLoadedAtLeastOnce;
        private Guid focusedWorkspaceId;

        public ProjectListFragment ()
        {
        }

        public ProjectListFragment (IntPtr jref, Android.Runtime.JniHandleOwnership xfer) : base (jref, xfer)
        {
        }

        public ProjectListFragment (IList<TimeEntryData> timeEntryList)
        {
            this.timeEntryList = timeEntryList;
            viewModel = new ProjectListViewModel (timeEntryList);
        }

        public override View OnCreateView (LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            var view = inflater.Inflate (Resource.Layout.ProjectListFragment, container, false);

            recyclerView = view.FindViewById<RecyclerView> (Resource.Id.ProjectListRecyclerView);
            recyclerView.SetLayoutManager (new LinearLayoutManager (Activity));
            recyclerView.AddItemDecoration (new ShadowItemDecoration (Activity));
            recyclerView.AddItemDecoration (new DividerItemDecoration (Activity, DividerItemDecoration.VerticalList));

            emptyStateLayout = view.FindViewById<LinearLayout> (Resource.Id.ProjectListEmptyState);
            appBar = view.FindViewById<TogglAppBar> (Resource.Id.ProjectListAppBar);
            tabLayout = view.FindViewById<TabLayout> (Resource.Id.WorkspaceTabLayout);
            fab = view.FindViewById<AddProjectFab> (Resource.Id.AddNewProjectFAB);

            Toolbar = view.FindViewById<Toolbar> (Resource.Id.ProjectListToolbar);
            var activity = (Activity)Activity;
            activity.SetSupportActionBar (Toolbar);
            activity.SupportActionBar.SetDisplayHomeAsUpEnabled (true);
            activity.SupportActionBar.SetTitle (Resource.String.ChooseTimeEntryProjectDialogTitle);

            HasOptionsMenu = true;

            appBar.AddOnOffsetChangedListener (this);
            fab.Click += OnFABClick;

            SetupViews ();

            return view;
        }

        private void SetupViews ()
        {
            tabLayout.Visibility = viewModel.ProjectList.CountWorkspaces == 1 ? ViewStates.Gone : ViewStates.Visible;
            SetupCoordinatorViews ();
        }

        public override void OnCreateOptionsMenu (IMenu menu, MenuInflater inflater)
        {
            base.OnCreateOptionsMenu (menu, inflater);
            inflater.Inflate (Resource.Menu.ProjectListToolbarMenu, menu);
            Toolbar.SetOnMenuItemClickListener (this);
        }

        private void SetupCoordinatorViews()
        {
            var appBarLayoutParamaters = new CoordinatorLayout.LayoutParams (appBar.LayoutParameters);
            appBarLayoutParamaters.Behavior = new AppBarLayout.Behavior();
            appBar.LayoutParameters = appBarLayoutParamaters;
        }

        private void OnFABClick (object sender, EventArgs e)
        {
            var entryList = new List<TimeEntryData> (timeEntryList);

            ChangeListWorkspace (entryList, focusedWorkspaceId);

            var intent = BaseActivity.CreateDataIntent<NewProjectActivity, List<TimeEntryData>>
                         (Activity, entryList, NewProjectActivity.ExtraTimeEntryDataListId);

            StartActivityForResult (intent, ProjectCreatedRequestCode);
        }

        private void ChangeListWorkspace (List<TimeEntryData> list, Guid wsId)
        {
            foreach (var entry in list) {
                entry.WorkspaceId = wsId;
            }
        }

        public async override void OnViewCreated (View view, Bundle savedInstanceState)
        {
            base.OnViewCreated (view, savedInstanceState);

            if (viewModel == null) {
                var timeEntryList = await ProjectListActivity.GetIntentTimeEntryData (Activity.Intent);
                if (timeEntryList.Count == 0) {
                    Activity.Finish ();
                    return;
                }
                viewModel = new ProjectListViewModel (timeEntryList);
            }

            var adapter = new ProjectListAdapter (recyclerView, viewModel.ProjectList);
            adapter.HandleProjectSelection = OnItemSelected;
            recyclerView.SetAdapter (adapter);
            viewModel.OnIsLoadingChanged += OnModelLoaded;
            viewModel.ProjectList.OnIsLoadingChanged += OnListLoaded;
            await viewModel.Init ();
        }

        public override void OnResume ()
        {
            if (listLoadedAtLeastOnce) {
                EnsureCorrectState ();
            }
            base.OnResume ();
        }

        private void GenerateTabs ()
        {
            foreach (var ws in viewModel.ProjectList.Workspaces) {
                tabLayout.AddTab (tabLayout.NewTab().SetText (ws.Data.Name));
            }
            tabLayout.SetOnTabSelectedListener (this);
        }

        private void OnModelLoaded (object sender, EventArgs e)
        {
            if (!viewModel.IsLoading) {
                if (viewModel.Model == null) {
                    Activity.Finish ();
                }
            }
        }

        public WorkspaceProjectsView.SortProjectsBy SortBy
        {
            set {
                viewModel.ProjectList.SortBy = value;
            }
        }

        private void OnListLoaded (object sender, EventArgs e)
        {
            listLoadedAtLeastOnce = true;
            EnsureCorrectState ();
            GenerateTabs ();
        }

        private void EnsureCorrectState()
        {
            recyclerView.Visibility = viewModel.ProjectList.IsEmpty ? ViewStates.Gone : ViewStates.Visible;
            emptyStateLayout.Visibility = viewModel.ProjectList.IsEmpty ? ViewStates.Visible : ViewStates.Gone;
        }

        private async void OnItemSelected (object m)
        {
            ProjectModel project = null;
            WorkspaceModel workspace = null;
            TaskData task = null;

            if (m is WorkspaceProjectsView.Project) {
                var wrap = (WorkspaceProjectsView.Project)m;
                if (wrap.IsNoProject) {
                    workspace = new WorkspaceModel (wrap.WorkspaceId);
                } else if (wrap.IsNewProject) {
                    // Show create project activity instead
                    var entryList = new List<TimeEntryData> (viewModel.TimeEntryList);
                    var intent = BaseActivity.CreateDataIntent<NewProjectActivity, List<TimeEntryData>>
                                 (Activity, entryList, NewProjectActivity.ExtraTimeEntryDataListId);
                    StartActivityForResult (intent, ProjectCreatedRequestCode);
                } else {
                    project = (ProjectModel)wrap.Data;
                    workspace = project.Workspace;
                }
            } else if (m is ProjectAndTaskView.Workspace) {
                var wrap = (ProjectAndTaskView.Workspace)m;
                workspace = (WorkspaceModel)wrap.Data;
            } else if (m is TaskData) {
                task = (TaskData)m;
                project = new ProjectModel (task.ProjectId);
                workspace = new WorkspaceModel (task.WorkspaceId);
            }

            if (project != null || workspace != null) {
                await viewModel.SaveModelAsync (project, workspace, task);
                Activity.Finish ();
            }
        }

        public override bool OnOptionsItemSelected (IMenuItem item)
        {
            if (item.ItemId == Android.Resource.Id.Home) {
                Activity.OnBackPressed ();
            }
            return base.OnOptionsItemSelected (item);
        }

        public override void OnActivityResult (int requestCode, int resultCode, Intent data)
        {
            base.OnActivityResult (requestCode, resultCode, data);

            if (requestCode == ProjectCreatedRequestCode) {
                if (resultCode == (int)Result.Ok) {
                    Activity.Finish();
                }
            }
        }

        public override void OnDestroyView ()
        {
            Dispose (true);
            base.OnDestroyView ();
        }

        protected override void Dispose (bool disposing)
        {
            if (disposing) {
                viewModel.OnIsLoadingChanged -= OnModelLoaded;
                viewModel.Dispose ();
                viewModel.ProjectList.OnIsLoadingChanged -= OnListLoaded;
                viewModel.ProjectList.Dispose ();
            }
            base.Dispose (disposing);
        }

        public void OnOffsetChanged (AppBarLayout layout, int verticalOffset)
        {
            tabLayout.TranslationY = -verticalOffset;
        }

        public bool OnMenuItemClick (IMenuItem item)
        {
            switch (item.ItemId) {
            case Resource.Id.SortByClients:
                viewModel.ProjectList.SortBy = WorkspaceProjectsView.SortProjectsBy.Clients;
                return true;
            case Resource.Id.SortByProjects:
                viewModel.ProjectList.SortBy = WorkspaceProjectsView.SortProjectsBy.Projects;
                return true;
            }
            return false;
        }

        public void OnTabSelected (TabLayout.Tab tab)
        {
            viewModel.ProjectList.CurrentPosition = tab.Position;
            focusedWorkspaceId = viewModel.ProjectList.Workspaces[tab.Position].Data.Id;
            EnsureCorrectState();
        }

        public void OnTabReselected (TabLayout.Tab tab)
        {
        }

        public void OnTabUnselected (TabLayout.Tab tab)
        {
        }
    }
}

