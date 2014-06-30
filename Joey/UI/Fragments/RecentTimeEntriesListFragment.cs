using System;
using Android.OS;
using Android.Views;
using Android.Widget;
using Toggl.Phoebe;
using XPlatUtils;
using Toggl.Joey.Data;
using Toggl.Joey.UI.Adapters;
using Toggl.Joey.UI.Utils;
using Toggl.Joey.UI.Views;
using ListFragment = Android.Support.V4.App.ListFragment;
using Toggl.Phoebe.Data.Models;

namespace Toggl.Joey.UI.Fragments
{
    public class RecentTimeEntriesListFragment : ListFragment
    {
        private WelcomeBoxManager welcomeManager;
        private ViewGroup mainLayout;

        public override View OnCreateView (LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            mainLayout = (ViewGroup)inflater.Inflate (Resource.Layout.RecentTimeEntriesListFragment, container, false);
            mainLayout.FindViewById<TextView> (Resource.Id.EmptyTitleTextView).SetFont (Font.Roboto);
            mainLayout.FindViewById<TextView> (Resource.Id.EmptyTextTextView).SetFont (Font.RobotoLight);

            return mainLayout;
        }

        public override void OnViewCreated (View view, Bundle savedInstanceState)
        {
            base.OnViewCreated (view, savedInstanceState);
            ListView.SetClipToPadding (false);
        }

        public override void OnResume ()
        {
            EnsureAdapter ();
            base.OnResume ();
        }

        public override void OnDestroy ()
        {
            welcomeManager = null;
            base.OnDestroy ();
        }

        public override async void OnListItemClick (ListView l, View v, int position, long id)
        {

            RecentTimeEntriesAdapter adapter = null;
            if (l.Adapter is HeaderViewListAdapter) {
                var headerAdapter = (HeaderViewListAdapter)l.Adapter;
                adapter = headerAdapter.WrappedAdapter as RecentTimeEntriesAdapter;
                // Adjust the position by taking into account the fact that we've got headers
                position -= headerAdapter.HeadersCount;
            } else if (l.Adapter is RecentTimeEntriesAdapter) {
                adapter = (RecentTimeEntriesAdapter)l.Adapter;
            }

            if (adapter == null || position < 0 || position >= adapter.Count)
                return;

            var data = adapter.GetEntry (position);
            if (data == null)
                return;

            var settingsStore = ServiceContainer.Resolve<SettingsStore> ();
            if (settingsStore.ReadContinueDialog != true) {
                RecentTimeEntryContinueDialogFragment.ShowConfirm (FragmentManager, model);
                return;
            }

            var entry = model.Continue ();

            // Scroll to top (where the new model will appear)
            ListView.SmoothScrollToPosition (0);

            DurOnlyNoticeDialogFragment.TryShow (FragmentManager);

            var model = new TimeEntryModel (data);
            var entry = await model.ContinueAsync ();

            // Notify that the user explicitly started something
            var bus = ServiceContainer.Resolve<MessageBus> ();
            bus.Send (new UserTimeEntryStateChangeMessage (this, entry));
        }

        public override bool UserVisibleHint {
            get { return base.UserVisibleHint; }
            set {
                base.UserVisibleHint = value;
                EnsureAdapter ();
            }
        }

        private void EnsureAdapter ()
        {
            if (ListAdapter == null && UserVisibleHint && IsAdded) {
                var settingsStore = ServiceContainer.Resolve<SettingsStore> ();
                if (!settingsStore.GotWelcomeMessage) {
                    welcomeManager = new WelcomeBoxManager (ListView);
                }

                var adapter = new RecentTimeEntriesAdapter ();
                ListAdapter = adapter;
            }
        }

        private class WelcomeBoxManager
        {
            private readonly ListView listView;
            private readonly View headerView;
            private readonly LinearLayout emptyLinearLayout;
            private readonly View emptyView;

            public WelcomeBoxManager (ListView listView)
            {
                this.listView = listView;

                var inflater = LayoutInflater.FromContext (listView.Context);

                // Add list view welcome box
                headerView = Inflate (inflater);
                listView.AddHeaderView (headerView);

                // Add empty view welcome box
                emptyView = Inflate (inflater);
                emptyLinearLayout = listView.EmptyView.FindViewById<LinearLayout> (Resource.Id.EmptyLinearLayout);
                emptyLinearLayout.AddView (emptyView, 0);
            }

            private View Inflate (LayoutInflater inflater)
            {
                var root = inflater.Inflate (Resource.Layout.WelcomeBox, null);
                root.FindViewById<TextView> (Resource.Id.StartTextView).SetFont (Font.Roboto);
                root.FindViewById<TextView> (Resource.Id.SwipeLeftTextView).SetFont (Font.RobotoLight);
                root.FindViewById<TextView> (Resource.Id.SwipeRightTextView).SetFont (Font.RobotoLight);
                root.FindViewById<TextView> (Resource.Id.TapToContinueTextView).SetFont (Font.RobotoLight);
                root.FindViewById<Button> (Resource.Id.GotItButton)
                    .SetFont (Font.Roboto).Click += OnGotItButtonClick;
                return root;
            }

            private void OnGotItButtonClick (object sender, EventArgs e)
            {
                var settingsStore = ServiceContainer.Resolve<SettingsStore> ();
                settingsStore.GotWelcomeMessage = true;

                listView.RemoveHeaderView (headerView);
                emptyLinearLayout.RemoveView (emptyView);
            }
        }
    }
}
