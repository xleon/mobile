using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Android.App;
using Android.Content.PM;
using Android.OS;
using Android.Support.Design.Widget;
using Android.Support.V4.App;
using Android.Support.V4.View;
using Android.Views;
using Android.Widget;
using Toggl.Joey.UI.Fragments;
using Toggl.Joey.UI.Views;
using Toggl.Phoebe.Data;
using Toggl.Phoebe.Data.DataObjects;
using Toggl.Phoebe.Data.Utils;
using Toggl.Phoebe.Data.Views;
using Toggl.Phoebe.Net;
using XPlatUtils;
using Activity = Android.Support.V7.App.AppCompatActivity;
using Fragment = Android.Support.V4.App.Fragment;
using FragmentManager = Android.Support.V4.App.FragmentManager;
using Toolbar = Android.Support.V7.Widget.Toolbar;

namespace Toggl.Joey.UI.Activities
{
    [Activity (Label = "ProjectListActivity",
               ScreenOrientation = ScreenOrientation.Portrait,
               Theme = "@style/Theme.Toggl.App")]
    public class ProjectListActivity : BaseActivity, AppBarLayout.IOnOffsetChangedListener, Toolbar.IOnMenuItemClickListener
    {
        public static readonly string ExtraTimeEntriesIds = "com.toggl.timer.time_entries_ids";
        private static readonly int ProjectCreatedRequestCode = 1;

        private NoSwipePager viewPager;
        private ProjectFragmentAdapter projectFragmentAdapter;
        private TabLayout tabLayout;
        private TogglAppBar appBar;
        private Toolbar toolbar;
        private FloatingActionButton fab;
        private IList<TimeEntryData> timeEntryList;

        protected async override void OnCreateActivity (Bundle state)
        {
            base.OnCreateActivity (state);

            timeEntryList = await GetIntentTimeEntryData (Intent);
            if (timeEntryList.Count == 0) {
                Finish ();
            }

            SetContentView (Resource.Layout.ProjectListActivityLayout);

            projectFragmentAdapter = new ProjectFragmentAdapter (SupportFragmentManager, timeEntryList);
            appBar = FindViewById<TogglAppBar> (Resource.Id.ProjectListAppBar);
            toolbar = FindViewById<Toolbar> (Resource.Id.ProjectListToolbar);
            tabLayout = FindViewById<TabLayout> (Resource.Id.WorkspaceTabLayout);
            viewPager = FindViewById<NoSwipePager> (Resource.Id.ProjectListViewPager);
            fab = FindViewById<AddProjectFab> (Resource.Id.AddNewProjectFAB);

            SetSupportActionBar (toolbar);
            SupportActionBar.SetDisplayHomeAsUpEnabled (true);
            SupportActionBar.SetTitle (Resource.String.ChooseTimeEntryProjectDialogTitle);

            appBar.AddOnOffsetChangedListener (this);
            fab.Click += OnFABClick;

            SetupViews ();
        }

        private void SetupViews ()
        {
            viewPager.Adapter = projectFragmentAdapter;
            viewPager.SetCurrentItem (projectFragmentAdapter.FirstTabPos, false);

            tabLayout.SetupWithViewPager (viewPager);
            tabLayout.Visibility = projectFragmentAdapter.Count == 1 ? ViewStates.Gone : ViewStates.Visible;

            SetupCoordinatorViews ();
        }

        public override bool OnCreateOptionsMenu (IMenu menu)
        {
            base.OnCreateOptionsMenu (menu);
            MenuInflater.Inflate (Resource.Menu.ProjectListToolbarMenu, menu);
            toolbar.SetOnMenuItemClickListener (this);
            return true;
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

            ChangeListWorkspace (entryList, projectFragmentAdapter.GetWorkspaceIdOfPosition (tabLayout.SelectedTabPosition));

            var intent = BaseActivity.CreateDataIntent<NewProjectActivity, List<TimeEntryData>>
                         (this, entryList, NewProjectActivity.ExtraTimeEntryDataListId);

            StartActivityForResult (intent, ProjectCreatedRequestCode);
        }

        private void ChangeListWorkspace (List<TimeEntryData> list, Guid wsId)
        {
            foreach (var entry in list ) {
                entry.WorkspaceId = wsId;
            }
        }

        private void OnTabSelected (object sender, ViewPager.PageSelectedEventArgs e)
        {
            appBar.Expand ();
        }

        private class ProjectFragmentAdapter : FragmentPagerAdapter
        {
            private List<Fragment> fragments = new List<Fragment> ();
            private List<String> fragmentTitles = new List<String> ();
            private List<WorkspaceData> workspaces;
            private int firstTabPos = 0;

            public ProjectFragmentAdapter (FragmentManager fm, IList<TimeEntryData> timeEntryList) : base (fm)
            {
                workspaces = GetWorkspacesAsync().Result;

                int pos = 0;
                foreach (var ws in workspaces) {
                    fragmentTitles.Add (ws.Name);
                    fragments.Add (new ProjectListFragment (timeEntryList, ws.Id));

                    if (ws.Id == timeEntryList[0].WorkspaceId) {
                        firstTabPos = pos;
                    }
                    pos++;
                }
            }

            public override int Count
            {
                get {
                    return fragments.Count;
                }
            }

            public int FirstTabPos
            {
                get {
                    return firstTabPos;
                }
            }

            public WorkspaceProjectsView.SortProjectsBy SortBy
            {
                set {
                    foreach (var fragment in fragments) {
                        ((ProjectListFragment)fragment).SortBy = value;
                    }
                }
            }

            public override Fragment GetItem (int position)
            {
                return fragments[position];
            }

            public override Java.Lang.ICharSequence GetPageTitleFormatted (int position)
            {
                return new Java.Lang.String (fragmentTitles[position]);
            }

            public Guid GetWorkspaceIdOfPosition (int position)
            {
                return workspaces [position].Id;
            }

            private async Task<List<WorkspaceData>> GetWorkspacesAsync ()
            {
                var store = ServiceContainer.Resolve<IDataStore> ();
                var workspacesTask = store.Table<WorkspaceData> ()
                                     .QueryAsync (r => r.DeletedAt == null);
                await Task.WhenAll (workspacesTask);
                return SortWorkspaces (workspacesTask.Result);
            }

            private List<WorkspaceData> SortWorkspaces (List<WorkspaceData> data)
            {
                var user = ServiceContainer.Resolve<AuthManager> ().User;

                data.Sort ((a, b) => {
                    if (user != null) {
                        if (a != null && a.Id == user.DefaultWorkspaceId) {
                            return -1;
                        }
                        if (b != null && b.Id == user.DefaultWorkspaceId) {
                            return 1;
                        }
                    }
                    var aName = a != null ? (a.Name ?? String.Empty) : String.Empty;
                    var bName = b != null ? (b.Name ?? String.Empty) : String.Empty;
                    return String.Compare (aName, bName, StringComparison.Ordinal);
                });
                return data;
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

        public void OnOffsetChanged (AppBarLayout layout, int verticalOffset)
        {
            tabLayout.TranslationY = -verticalOffset;
        }

        public bool OnMenuItemClick (IMenuItem item)
        {
            switch (item.ItemId) {
            case Resource.Id.SortByClients:
                projectFragmentAdapter.SortBy = WorkspaceProjectsView.SortProjectsBy.Clients;
                return true;
            case Resource.Id.SortByProjects:
                projectFragmentAdapter.SortBy = WorkspaceProjectsView.SortProjectsBy.Projects;
                return true;
            }
            return false;
        }
    }
}

