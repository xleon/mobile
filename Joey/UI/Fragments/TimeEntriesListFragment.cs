using System;
using Android.Content;
using Android.OS;
using Android.Support.V4.App;
using Android.Support.V7.Widget;
using Android.Views;
using Android.Widget;
using Toggl.Joey.Data;
using Toggl.Joey.UI.Activities;
using Toggl.Joey.UI.Adapters;
using Toggl.Joey.UI.Utils;
using Toggl.Joey.UI.Views;
using Toggl.Phoebe;
using Toggl.Phoebe.Analytics;
using Toggl.Phoebe.Data;
using Toggl.Phoebe.Data.Models;
using Toggl.Phoebe.Data.Utils;
using Toggl.Phoebe.Data.Views;
using XPlatUtils;

namespace Toggl.Joey.UI.Fragments
{
    public class TimeEntriesListFragment : Fragment
    {
        private RecyclerView recyclerView;
        private View emptyMessageView;
        private Subscription<SettingChangedMessage> subscriptionSettingChanged;

        public override View OnCreateView (LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            var view = inflater.Inflate (Resource.Layout.TimeEntriesListFragment, container, false);
            view.FindViewById<TextView> (Resource.Id.EmptyTitleTextView).SetFont (Font.Roboto);
            view.FindViewById<TextView> (Resource.Id.EmptyTextTextView).SetFont (Font.RobotoLight);

            emptyMessageView = view.FindViewById<View> (Resource.Id.EmptyMessageView);
            emptyMessageView.Visibility = ViewStates.Gone;
            recyclerView = view.FindViewById<RecyclerView> (Resource.Id.LogRecyclerView);

            return view;
        }

        public override void OnViewCreated (View view, Bundle savedInstanceState)
        {
            base.OnViewCreated (view, savedInstanceState);

            // Create view model.
            var linearLayout = new LinearLayoutManager (Activity);

            recyclerView.SetLayoutManager (linearLayout);
            recyclerView.AddItemDecoration (new DividerItemDecoration (Activity, DividerItemDecoration.VerticalList));

            var bus = ServiceContainer.Resolve<MessageBus> ();
            subscriptionSettingChanged = bus.Subscribe<SettingChangedMessage> (OnSettingChanged);
        }

        public override void OnResume ()
        {
            EnsureAdapter ();
            base.OnResume ();
        }

        public override bool UserVisibleHint
        {
            get { return base.UserVisibleHint; }
            set {
                base.UserVisibleHint = value;
                EnsureAdapter ();
            }
        }

        #region TimeEntry handlers
        private async void ContinueTimeEntry (TimeEntryModel model)
        {
            DurOnlyNoticeDialogFragment.TryShow (FragmentManager);

            var entry = await model.ContinueAsync ();

            var bus = ServiceContainer.Resolve<MessageBus> ();
            bus.Send (new UserTimeEntryStateChangeMessage (this, entry));

            // Ping analytics
            ServiceContainer.Resolve<ITracker> ().SendTimerStartEvent (TimerStartSource.AppContinue);
        }

        private async void StopTimeEntry (TimeEntryModel model)
        {
            await model.StopAsync ();

            // Ping analytics
            ServiceContainer.Resolve<ITracker> ().SendTimerStopEvent (TimerStopSource.App);
        }

        private void ConfirmTimeEntryDeletion (TimeEntryModel model)
        {
        }

        private void OpenTimeEntryEdit (TimeEntryModel model)
        {
            var i = new Intent (Activity, typeof (EditTimeEntryActivity));
            i.PutExtra (EditTimeEntryActivity.ExtraTimeEntryId, model.Id.ToString ());
            StartActivity (i);
        }
        #endregion

        #region TimeEntryGroup handlers
        private async void ContinueTimeEntryGroup (TimeEntryGroup entryGroup)
        {
            DurOnlyNoticeDialogFragment.TryShow (FragmentManager);

            var entry = await entryGroup.Model.ContinueAsync ();

            var bus = ServiceContainer.Resolve<MessageBus> ();
            bus.Send (new UserTimeEntryStateChangeMessage (this, entry));

            // Ping analytics
            ServiceContainer.Resolve<ITracker> ().SendTimerStartEvent (TimerStartSource.AppContinue);
        }

        private async void StopTimeEntryGroup (TimeEntryGroup entryGroup)
        {
            await entryGroup.Model.StopAsync ();

            // Ping analytics
            ServiceContainer.Resolve<ITracker> ().SendTimerStopEvent (TimerStopSource.App);
        }

        private void ConfirmTimeEntryGroupDeletion (TimeEntryGroup entryGroup)
        {
        }

        private void OpenTimeEntryGroupEdit (TimeEntryGroup entryGroup)
        {
            var i = new Intent (Activity, typeof (EditTimeEntryActivity));
            string[] guids = entryGroup.TimeEntryGuids;
            i.PutExtra (EditTimeEntryActivity.ExtraGroupedTimeEntriesGuids, guids);
            StartActivity (i);
        }
        #endregion

        private void EnsureAdapter ()
        {
            if (recyclerView.GetAdapter() == null) {
                recyclerView.SetAdapter (new TimeEntriesAdapter ());
                var isGrouped = ServiceContainer.Resolve<SettingsStore> ().GroupedTimeEntries;
            }
        }

        protected override void Dispose (bool disposing)
        {
            if (disposing) {
                var bus = ServiceContainer.Resolve<MessageBus> ();
                if (subscriptionSettingChanged != null) {
                    bus.Unsubscribe (subscriptionSettingChanged);
                    subscriptionSettingChanged = null;
                }
            }
            base.Dispose (disposing);
        }

        private void OnSettingChanged (SettingChangedMessage msg)
        {
            // Protect against Java side being GCed
            if (Handle == IntPtr.Zero) {
                return;
            }

            if (msg.Name == SettingsStore.PropertyGroupedTimeEntries) {
                EnsureAdapter();
            }
        }

        private class RecyclerViewScrollDetector : RecyclerView.OnScrollListener
        {
            private ICollectionDataView<object> viewModel;
            private LinearLayoutManager layoutManager;

            public RecyclerViewScrollDetector (ICollectionDataView<object> viewModel, LinearLayoutManager layoutManager)
            {
                this.viewModel = viewModel;
                this.layoutManager = layoutManager;
                LoadMoreThreshold = 3;
            }

            public int LoadMoreThreshold { get; set; }

            public int ScrollThreshold { get; set; }

            public RecyclerView.OnScrollListener OnScrollListener { get; set; }

            public override void OnScrolled (RecyclerView recyclerView, int dx, int dy)
            {
                if (OnScrollListener != null) {
                    OnScrollListener.OnScrolled (recyclerView, dx, dy);
                }

                var isSignificantDelta = Math.Abs (dy) > ScrollThreshold;
                if (isSignificantDelta) {
                    if (dy > 0) {
                        OnScrollUp();
                    } else {
                        OnScrollDown();
                    }
                }

                var visibleItemCount = recyclerView.ChildCount;
                var totalItemCount = layoutManager.ItemCount;
                var firstVisibleItem = layoutManager.FindFirstVisibleItemPosition();

                if (!viewModel.IsLoading  && (totalItemCount - visibleItemCount) <= (firstVisibleItem + LoadMoreThreshold)) {
                    //viewModel.LoadMore();
                }
            }

            public override void OnScrollStateChanged (RecyclerView recyclerView, int newState)
            {
                if (OnScrollListener != null) {
                    OnScrollListener.OnScrollStateChanged (recyclerView, newState);
                }

                base.OnScrollStateChanged (recyclerView, newState);
            }

            private void OnScrollUp()
            {
            }

            private void OnScrollDown()
            {
            }
        }
    }
}
