using System;
using Android.Content;
using Android.OS;
using Android.Support.V4.App;
using Android.Support.V7.Widget;
using Android.Views;
using Android.Views.Animations;
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
using XPlatUtils;

namespace Toggl.Joey.UI.Fragments
{
    public class LogTimeEntriesListFragment : Fragment
    {
        private RecyclerView recyclerView;
        private View emptyMessageView;
        private Subscription<SettingChangedMessage> subscriptionSettingChanged;
        private LogTimeEntriesAdapter logAdapter;
        private GroupedTimeEntriesAdapter groupedAdapter;
        private readonly Handler handler = new Handler ();
        private FrameLayout undoBar;
        private Button undoButton;
        private TimeEntryModel undoItem;
        private Context ctx;

        private LogTimeEntriesAdapter LogAdapter
        {
            get {
                if (logAdapter == null) {
                    logAdapter = new LogTimeEntriesAdapter();
                    logAdapter.HandleTimeEntryContinue = ContinueTimeEntry;
                    logAdapter.HandleTimeEntryStop = StopTimeEntry;
                    logAdapter.HandleTimeEntryEditing = OpenTimeEntryEdit;
                }
                return logAdapter;
            }
        }

        private GroupedTimeEntriesAdapter GroupedAdapter
        {
            get {
                if (groupedAdapter == null) {
                    groupedAdapter = new GroupedTimeEntriesAdapter();
                    groupedAdapter.HandleGroupContinue = ContinueTimeEntryGroup;
                    groupedAdapter.HandleGroupStop = StopTimeEntryGroup;
                    groupedAdapter.HandleGroupEditing = OpenTimeEntryGroupEdit;
                }
                return groupedAdapter;
            }
        }

        public override View OnCreateView (LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            ctx = inflater.Context;
            var view = inflater.Inflate (Resource.Layout.LogTimeEntriesListFragment, container, false);
            view.FindViewById<TextView> (Resource.Id.EmptyTitleTextView).SetFont (Font.Roboto);
            view.FindViewById<TextView> (Resource.Id.EmptyTextTextView).SetFont (Font.RobotoLight);

            emptyMessageView = view.FindViewById<View> (Resource.Id.EmptyMessageView);
            emptyMessageView.Visibility = ViewStates.Gone;
            recyclerView = view.FindViewById<RecyclerView> (Resource.Id.LogRecyclerView);

            undoBar = view.FindViewById<FrameLayout> (Resource.Id.UndoBar);
            undoButton = view.FindViewById<Button> (Resource.Id.UndoButton);
            undoButton.Click += UndoClicked;

            return view;
        }

        public override void OnViewCreated (View view, Bundle savedInstanceState)
        {
            base.OnViewCreated (view, savedInstanceState);

            // Create view model.
            var linearLayout = new LinearLayoutManager (Activity);
            recyclerView.SetLayoutManager (linearLayout);
            recyclerView.AddItemDecoration (new DividerItemDecoration (Activity, DividerItemDecoration.VerticalList));
            recyclerView.SetOnScrollListener (new RecyclerViewScrollDetector (linearLayout));

            var bus = ServiceContainer.Resolve<MessageBus> ();
            subscriptionSettingChanged = bus.Subscribe<SettingChangedMessage> (OnSettingChanged);

            var swipeTouchListener = new SwipeDeleteTouchListener (ListView, new SwipeDismissCallBacks (this));
            ListView.SetOnTouchListener (swipeTouchListener);
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

        public void ToggleUndoBar()
        {
            ToggleUndo ();
        }

        private void ShowUndo ()
        {
            if (!showingUndoBar) {
                showingUndoBar = true;
            }
            handler.RemoveCallbacks (HideUndo);
            handler.PostDelayed (HideUndo, 10000);
        }

        private void HideUndo ()
        {
            showingUndoBar = false;
        }

        private bool showingUndoBar
        {
            get {
                return undoBar.Visibility == ViewStates.Visible;
            } set {
                SetUndoBarVisibility (value);
            }
        }

        public void SetUndoBarVisibility (bool visibility)
        {
            if (visibility) {
                Animation bottomUp = AnimationUtils.LoadAnimation (ctx, Resource.Animation.BottomUpAnimation);
                undoBar.Visibility = ViewStates.Visible;
                undoBar.StartAnimation (bottomUp);
            } else {
                Animation bottomDown = AnimationUtils.LoadAnimation (ctx, Resource.Animation.BottomDownAnimation);
                undoBar.StartAnimation (bottomDown);
                undoBar.Visibility = ViewStates.Gone;
            }
        }

        private async void UndoClicked (object sender, EventArgs e)
        {
            if (undoItem != null) {
                await undoItem.SaveAsync ();
            }
            showingUndoBar = false;
            handler.RemoveCallbacks (ShowUndo);
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
                var isGrouped = ServiceContainer.Resolve<SettingsStore> ().GroupedTimeEntries;
                if (isGrouped) {
                    if (logAdapter != null) {
                        logAdapter.Dispose ();
                        logAdapter = null;
                    }
                    recyclerView.SetAdapter (GroupedAdapter);
                } else {
                    if (groupedAdapter != null) {
                        groupedAdapter.Dispose ();
                        groupedAdapter = null;
                    }
                    recyclerView.SetAdapter (LogAdapter);
                }
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
                LogAdapter.Dispose ();
                GroupedAdapter.Dispose ();
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

        public bool CanDismiss (int position)
        {
            var adapter = ListView.Adapter as LogTimeEntriesAdapter;
            if (adapter == null) {
                return false;
            }
            return adapter.GetItemViewType (position) == 1;
        }

        private async void UndoClicked (object sender, EventArgs e)
        {
            if (undoItem != null) {
                await undoItem.SaveAsync ();
            }
            showingUndoBar = false;
            handler.RemoveCallbacks (ToggleUndo);
        }

        public class SwipeDismissCallBacks : SwipeDeleteTouchListener.IDismissCallbacks
        {
            private readonly LogTimeEntriesListFragment listView;

            public SwipeDismissCallBacks (LogTimeEntriesListFragment lv)
            {
                listView = lv;
            }

            public bool CanDismiss (int position)
            {
                return listView.CanDismiss (position);
            }

            public void OnDismiss (int position)
            {
            }
        }

        private class ItemTouchListener : RecyclerView.IOnItemTouchListener
        {
            public bool OnInterceptTouchEvent (RecyclerView rv, MotionEvent e)
            {
                return false;
            }

            public void OnTouchEvent (RecyclerView rv, MotionEvent e)
            {
            }

            public void Dispose ()
            {
            }

            public IntPtr Handle
            {
                get {
                    throw new NotImplementedException ();
                }
            }
        }

        private class RecyclerViewScrollDetector : RecyclerView.OnScrollListener
        {
            private LinearLayoutManager layoutManager;

            public RecyclerViewScrollDetector (LinearLayoutManager layoutManager)
            {
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
