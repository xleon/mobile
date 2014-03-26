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
            if (welcomeManager != null) {
                welcomeManager.Dispose ();
                welcomeManager = null;
            }
            base.OnDestroy ();
        }

        public override void OnListItemClick (ListView l, View v, int position, long id)
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

            var model = adapter.GetModel (position);
            if (model == null)
                return;

            var entry = model.Continue ();

            // Scroll to top (where the new model will appear)
            ListView.SmoothScrollToPosition (0);

            // Notify that the user explicitly started something
            var bus = ServiceContainer.Resolve<MessageBus> ();
            bus.Send (new UserTimeEntryStateChangeMessage (this, entry));

            DurOnlyNoticeDialogFragment.TryShow (FragmentManager);
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
                if (welcomeManager != null) {
                    welcomeManager.Attach (adapter);
                }
            }
        }

        private class WelcomeBoxManager : Android.Database.DataSetObserver
        {
            private readonly ListView listView;
            private IListAdapter adapter;
            private View root;
            private Parent parent;

            public WelcomeBoxManager (ListView listView)
            {
                this.listView = listView;

                Inflate (LayoutInflater.FromContext (listView.Context));
                listView.AddHeaderView (root);
                parent = Parent.ListView;
            }

            public void Attach (IListAdapter adapter)
            {
                if (this.adapter != null) {
                    this.adapter.UnregisterDataSetObserver (this);
                }

                this.adapter = adapter;
                ReparentRoot ();

                if (this.adapter != null) {
                    this.adapter.RegisterDataSetObserver (this);
                }
            }

            protected override void Dispose (bool disposing)
            {
                if (disposing) {
                    if (adapter != null) {
                        adapter.UnregisterDataSetObserver (this);
                    }
                }
                base.Dispose (disposing);
            }

            private void Inflate (LayoutInflater inflater)
            {
                root = inflater.Inflate (Resource.Layout.WelcomeBox, null);
                root.FindViewById<TextView> (Resource.Id.StartTextView).SetFont (Font.Roboto);
                root.FindViewById<TextView> (Resource.Id.SwipeLeftTextView).SetFont (Font.RobotoLight);
                root.FindViewById<TextView> (Resource.Id.SwipeRightTextView).SetFont (Font.RobotoLight);
                root.FindViewById<TextView> (Resource.Id.TapToContinueTextView).SetFont (Font.RobotoLight);
                root.FindViewById<Button> (Resource.Id.GotItButton)
                    .SetFont (Font.Roboto).Click += OnGotItButtonClick;
            }

            private void ReparentRoot ()
            {
                var fromParent = parent;

                // Determine where to show the welcome message
                var settingsStore = ServiceContainer.Resolve<SettingsStore> ();
                if (settingsStore.GotWelcomeMessage) {
                    parent = Parent.Orphan;
                } else {
                    if (adapter == null || adapter.Count == 0) {
                        parent = Parent.EmptyView;
                    } else {
                        parent = Parent.ListView;
                    }
                }

                if (fromParent == parent)
                    return;

                // Remove from old parent:
                switch (fromParent) {
                case Parent.ListView:
                    listView.RemoveHeaderView (root);
                    break;
                default:
                    var vg = root.Parent as ViewGroup;
                    if (vg != null) {
                        vg.RemoveView (root);
                    }
                    break;
                }

                // Add to correct parent:
                switch (parent) {
                case Parent.ListView:
                    listView.AddHeaderView (root);
                    break;
                case Parent.EmptyView:
                    var cont = listView.EmptyView.FindViewById<LinearLayout> (Resource.Id.EmptyLinearLayout);
                    cont.AddView (root, 0);
                    break;
                }
            }

            private void OnGotItButtonClick (object sender, EventArgs e)
            {
                var settingsStore = ServiceContainer.Resolve<SettingsStore> ();
                settingsStore.GotWelcomeMessage = true;
                ReparentRoot ();
            }

            public override void OnChanged ()
            {
                ReparentRoot ();
            }

            private enum Parent
            {
                Orphan,
                ListView,
                EmptyView,
            }
        }
    }
}
