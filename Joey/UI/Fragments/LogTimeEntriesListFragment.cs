using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Android.Content;
using Android.OS;
using Android.Support.Design.Widget;
using Android.Support.V4.App;
using Android.Support.V4.Widget;
using Android.Support.V7.Widget;
using Android.Support.V7.Widget.Helper;
using Android.Views;
using Android.Widget;
using GalaSoft.MvvmLight.Helpers;
using Toggl.Joey.UI.Activities;
using Toggl.Joey.UI.Adapters;
using Toggl.Joey.UI.Components;
using Toggl.Joey.UI.Utils;
using Toggl.Joey.UI.Views;
using Toggl.Phoebe;
using Toggl.Phoebe.Net;
using Toggl.Phoebe._Helpers;
using Toggl.Phoebe._ViewModels;
using Toggl.Phoebe._ViewModels.Timer;
using XPlatUtils;

namespace Toggl.Joey.UI.Fragments
{
    public class LogTimeEntriesListFragment : Fragment,
        SwipeDismissCallback.IDismissListener,
        ItemTouchListener.IItemTouchListener,
        SwipeRefreshLayout.IOnRefreshListener
    {
        public static bool NewTimeEntryStartedByFAB;

        private RecyclerView recyclerView;
        private SwipeRefreshLayout swipeLayout;
        private View emptyMessageView;
        private LogTimeEntriesAdapter logAdapter;
        private CoordinatorLayout coordinatorLayout;
        private Subscription<SyncFinishedMessage> drawerSyncFinished;
        private TimerComponent timerComponent;

        // Recycler setup
        private DividerItemDecoration dividerDecoration;
        private ShadowItemDecoration shadowDecoration;
        private ItemTouchListener itemTouchListener;

        // binding references
        private Binding<bool, bool> hasMoreBinding, newMenuBinding;
        private Binding<TimeEntryCollectionVM, TimeEntryCollectionVM> collectionBinding;
        private Binding<bool, FABButtonState> fabBinding;

        #region Binding objects and properties.

        public LogTimeEntriesVM ViewModel { get; private set;}
        public IMenuItem AddNewMenuItem { get; private set; }
        public StartStopFab StartStopBtn { get; private set;}

        #endregion

        public override View OnCreateView (LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            var view = inflater.Inflate (Resource.Layout.LogTimeEntriesListFragment, container, false);
            view.FindViewById<TextView> (Resource.Id.EmptyTextTextView).SetFont (Font.RobotoLight);

            emptyMessageView = view.FindViewById<View> (Resource.Id.EmptyMessageView);
            emptyMessageView.Visibility = ViewStates.Gone;
            recyclerView = view.FindViewById<RecyclerView> (Resource.Id.LogRecyclerView);
            recyclerView.SetLayoutManager (new LinearLayoutManager (Activity));
            swipeLayout = view.FindViewById<SwipeRefreshLayout> (Resource.Id.LogSwipeContainer);
            swipeLayout.SetOnRefreshListener (this);
            coordinatorLayout = view.FindViewById<CoordinatorLayout> (Resource.Id.logCoordinatorLayout);
            StartStopBtn = view.FindViewById<StartStopFab> (Resource.Id.StartStopBtn);
            timerComponent = ((MainDrawerActivity)Activity).Timer; // TODO: a better way to do this?
            HasOptionsMenu = true;

            return view;
        }

        public override void OnViewCreated (View view, Bundle savedInstanceState)
        {
            base.OnViewCreated (view, savedInstanceState);

            // init viewModel
            ViewModel = LogTimeEntriesVM.Init ();

            collectionBinding = this.SetBinding (() => ViewModel.Collection).WhenSourceChanges (() => {
                logAdapter = new LogTimeEntriesAdapter (recyclerView, ViewModel);
                recyclerView.SetAdapter (logAdapter);
            });
            fabBinding = this.SetBinding (() => ViewModel.IsTimeEntryRunning, () => StartStopBtn.ButtonAction)
                         .ConvertSourceToTarget (isRunning => isRunning ? FABButtonState.Stop : FABButtonState.Start);

            newMenuBinding = this.SetBinding (() => ViewModel.IsTimeEntryRunning)
            .WhenSourceChanges (() => {
                if (AddNewMenuItem != null) {
                    AddNewMenuItem.SetVisible (!ViewModel.IsTimeEntryRunning);
                }
            });

            // Pass ViewModel to TimerComponent.
            timerComponent.SetViewModel (ViewModel);
            StartStopBtn.Click += StartStopClick;
            SetupRecyclerView (ViewModel);

            // TODO: Review this line.
            // Get data to fill the list. For the moment,
            // until a screenloader is added to the screen
            // is better to load the items after create
            // the viewModel and show the loader from RecyclerView
            ViewModel.LoadMore ();

            // Subscribe to sync messages
            var bus = ServiceContainer.Resolve<MessageBus> ();
            drawerSyncFinished = bus.Subscribe<SyncFinishedMessage> (SyncFinished);
        }


        public async void StartStopClick (object sender, EventArgs e)
        {
            var timeEntryData = await ViewModel.StartStopTimeEntry ();

            if (ViewModel.HasMoreItems) {
                OBMExperimentManager.Send (OBMExperimentManager.HomeEmptyState, "startButton", "click");
            }

            if (ViewModel.IsTimeEntryRunning) {

                NewTimeEntryStartedByFAB = true;

                var ids = new List<string> { timeEntryData.Id.ToString () };
                var intent = new Intent (Activity, typeof (EditTimeEntryActivity));
                intent.PutStringArrayListExtra (EditTimeEntryActivity.ExtraGroupedTimeEntriesGuids, ids);
                intent.PutExtra (EditTimeEntryActivity.IsGrouped,  false);
                StartActivity (intent);
            }
        }

        public override void OnDestroyView ()
        {
            // Protect against Java side being GCed
            if (Handle == IntPtr.Zero) {
                return;
            }

            // TODO: Remove bindings to ViewModel
            // check if it is needed or not.
            timerComponent.DetachBindind ();
            ReleaseRecyclerView ();
            ViewModel.Dispose ();
            base.OnDestroyView ();
        }

        #region Menu setup
        public override void OnCreateOptionsMenu (IMenu menu, MenuInflater inflater)
        {
            inflater.Inflate (Resource.Menu.NewItemMenu, menu);
            AddNewMenuItem = menu.FindItem (Resource.Id.newItem);
            ConfigureOptionMenu ();
        }

        public override bool OnOptionsItemSelected (IMenuItem item)
        {
            var i = new Intent (Activity, typeof (EditTimeEntryActivity));
            i.PutStringArrayListExtra (EditTimeEntryActivity.ExtraGroupedTimeEntriesGuids, new List<string> { ViewModel.GetActiveTimeEntry ().Id.ToString ()});
            Activity.StartActivity (i);

            return base.OnOptionsItemSelected (item);
        }

        // Because the viewModel needs time to be created,
        // this method is called from two points
        private void ConfigureOptionMenu ()
        {
            if (ViewModel != null && AddNewMenuItem != null) {
                AddNewMenuItem.SetVisible (!ViewModel.IsTimeEntryRunning);
            }
        }
        #endregion

        #region IDismissListener implementation
        public bool CanDismiss (RecyclerView recyclerView, RecyclerView.ViewHolder viewHolder)
        {
            var adapter = recyclerView.GetAdapter ();
            return adapter.GetItemViewType (viewHolder.LayoutPosition) == RecyclerCollectionDataAdapter<IHolder>.ViewTypeContent;
        }

        public void OnDismiss (RecyclerView.ViewHolder viewHolder)
        {
            const int duration = Literals.TimeEntryRemoveUndoSeconds * 1000;

            ViewModel.RemoveItemWithUndo (viewHolder.AdapterPosition);
            var snackBar = Snackbar
                           .Make (coordinatorLayout, Resources.GetString (Resource.String.UndoBarDeletedText), duration)
                           .SetAction (Resources.GetString (Resource.String.UndoBarButtonText),
                                       _ => ViewModel.RestoreItemFromUndo ());
            ChangeSnackBarColor (snackBar);
            snackBar.Show ();
        }
        #endregion

        #region IRecyclerViewOnItemClickListener implementation
        public void OnItemClick (RecyclerView parent, View clickedView, int position)
        {
            var intent = new Intent (Activity, typeof (EditTimeEntryActivity));
            IList<string> guids = ((ITimeEntryHolder)ViewModel.Collection.Data.ElementAt (position)).Guids;
            intent.PutStringArrayListExtra (EditTimeEntryActivity.ExtraGroupedTimeEntriesGuids, guids);
            intent.PutExtra (EditTimeEntryActivity.IsGrouped, guids.Count > 1);

            StartActivity (intent);
        }

        public void OnItemLongClick (RecyclerView parent, View clickedView, int position)
        {
            OnItemClick (parent, clickedView, position);
        }

        public bool CanClick (RecyclerView view, int position)
        {
            var adapter = recyclerView.GetAdapter ();
            return adapter.GetItemViewType (position) == RecyclerCollectionDataAdapter<IHolder>.ViewTypeContent;
        }
        #endregion

        #region Sync
        public void OnRefresh ()
        {
            ViewModel.TriggerFullSync ();
        }

        private void SyncFinished (SyncFinishedMessage msg)
        {
            if (!swipeLayout.Refreshing) {
                return;
            }

            swipeLayout.Refreshing = false;

            if (msg.HadErrors) {
                int msgId = Resource.String.LastSyncHadErrors;

                if (msg.FatalError.IsNetworkFailure ()) {
                    msgId = Resource.String.LastSyncNoConnection;
                } else if (msg.FatalError is TaskCanceledException) {
                    msgId = Resource.String.LastSyncFatalError;
                }
                Snackbar.Make (coordinatorLayout, Resources.GetString (msgId), Snackbar.LengthLong).Show ();
            }
        }
        #endregion

        private void SetupRecyclerView (LogTimeEntriesVM viewModel)
        {
            // Touch listeners.
            itemTouchListener = new ItemTouchListener (recyclerView, this);
            recyclerView.AddOnItemTouchListener (itemTouchListener);

            // Scroll listener
            recyclerView.AddOnScrollListener (
                new ScrollListener ((LinearLayoutManager)recyclerView.GetLayoutManager (), viewModel));

            var touchCallback = new SwipeDismissCallback (ItemTouchHelper.Up | ItemTouchHelper.Down, ItemTouchHelper.Left, this);
            var touchHelper = new ItemTouchHelper (touchCallback);
            touchHelper.AttachToRecyclerView (recyclerView);

            // Decorations.
            dividerDecoration = new DividerItemDecoration (Activity, DividerItemDecoration.VerticalList);
            shadowDecoration = new ShadowItemDecoration (Activity);
            recyclerView.AddItemDecoration (dividerDecoration);
            recyclerView.AddItemDecoration (shadowDecoration);
            recyclerView.GetItemAnimator ().SupportsChangeAnimations = false;
        }

        private void ReleaseRecyclerView ()
        {
            recyclerView.RemoveItemDecoration (shadowDecoration);
            recyclerView.RemoveItemDecoration (dividerDecoration);
            recyclerView.RemoveOnItemTouchListener (itemTouchListener);

            recyclerView.GetAdapter ().Dispose ();
            recyclerView.Dispose ();
            logAdapter = null;

            itemTouchListener.Dispose ();
            dividerDecoration.Dispose ();
            shadowDecoration.Dispose ();
        }


        // Temporal hack to change the
        // action color in snack bar
        private void ChangeSnackBarColor (Snackbar snack)
        {
            var group = (ViewGroup) snack.View;
            for (int i = 0; i < group.ChildCount; i++) {
                View v = group.GetChildAt (i);
                var textView = v as TextView;
                if (textView != null) {
                    TextView t = textView;
                    if (t.Text == Resources.GetString (Resource.String.UndoBarButtonText)) {
                        t.SetTextColor (Resources.GetColor (Resource.Color.material_green));
                    }
                }
            }
        }

        class ScrollListener : RecyclerView.OnScrollListener
        {
            private const int visibleThreshold = 5; // The minimum amount of items to have below your current scroll position before loading more.
            private int previousTotal; // The total number of items in the dataset after the last load
            private bool loading = true; // True if we are still waiting for the last set of data to load.
            private int firstVisibleItem, visibleItemCount, totalItemCount;
            private readonly LinearLayoutManager linearLayoutManager;
            private readonly LogTimeEntriesVM viewModel;

            public ScrollListener (LinearLayoutManager linearLayoutManager, LogTimeEntriesVM viewModel)
            {
                this.linearLayoutManager = linearLayoutManager;
                this.viewModel = viewModel;
            }

            public override void OnScrolled (RecyclerView recyclerView, int dx, int dy)
            {
                base.OnScrolled (recyclerView, dx, dy);

                visibleItemCount = recyclerView.ChildCount;
                totalItemCount = linearLayoutManager.ItemCount;
                firstVisibleItem = linearLayoutManager.FindFirstVisibleItemPosition ();

                if (loading) {
                    if (totalItemCount > previousTotal) {
                        loading = false;
                        previousTotal = totalItemCount;
                    }
                }

                if (!loading && (totalItemCount - visibleItemCount) <= (firstVisibleItem + visibleThreshold)) {
                    loading = true;
                    // Request more entries.
                    viewModel.LoadMore ();
                }
            }
        }
    }
}
