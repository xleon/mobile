﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
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
using Toggl.Phoebe.Data.Models;
using Toggl.Phoebe.Reactive;
using Toggl.Phoebe.ViewModels;
using Toggl.Phoebe.ViewModels.Timer;

namespace Toggl.Joey.UI.Fragments
{
    public class LogTimeEntriesListFragment : Fragment,
        SwipeDismissCallback.IDismissListener,
        ItemTouchListener.IItemTouchListener,
        SwipeRefreshLayout.IOnRefreshListener
    {

        private CoordinatorLayout coordinatorLayout;
        private RecyclerView recyclerView;
        private SwipeRefreshLayout swipeLayout;
        private View emptyMessageView;
        private View experimentEmptyView;
        private LogTimeEntriesAdapter logAdapter;
        private TimerComponent timerComponent;
        private View welcomeView;

        // Recycler setup
        private DividerItemDecoration dividerDecoration;
        private ShadowItemDecoration shadowDecoration;
        private ItemTouchListener itemTouchListener;

        // binding references
        private Binding<bool, bool> newMenuBinding, isSyncingBinding;
        private Binding<int, int> hasItemsBinding;
        private Binding<LogTimeEntriesVM.LoadInfoType, LogTimeEntriesVM.LoadInfoType> loadInfoBinding;
        private Binding<ObservableCollection<IHolder>, ObservableCollection<IHolder>> collectionBinding;
        private Binding<bool, FABButtonState> fabBinding;
        private Binding<RichTimeEntry, RichTimeEntry> activeEntryBinding;


        #region Binding objects and properties.

        public LogTimeEntriesVM ViewModel { get; private set;}
        public IMenuItem AddNewMenuItem { get; private set; }
        public StartStopFab StartStopBtn { get; private set;}

        #endregion

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            var view = inflater.Inflate(Resource.Layout.LogTimeEntriesListFragment, container, false);
            view.FindViewById<TextView> (Resource.Id.EmptyTextTextView).SetFont(Font.RobotoLight);
            coordinatorLayout = view.FindViewById<CoordinatorLayout> (Resource.Id.logCoordinatorLayout);
            experimentEmptyView = view.FindViewById<View> (Resource.Id.ExperimentEmptyMessageView);
            emptyMessageView = view.FindViewById<View> (Resource.Id.EmptyMessageView);
            welcomeView = view.FindViewById<View> (Resource.Id.WelcomeLayout);

            view.FindViewById<TextView>(Resource.Id.welcomeHelloTextView).SetFont(Font.TektonPro);
            view.FindViewById<TextView>(Resource.Id.welcomeSignInTextView).SetFont(Font.TektonPro);
            view.FindViewById<TextView>(Resource.Id.welcomeStartTextView).SetFont(Font.TektonPro);

            recyclerView = view.FindViewById<RecyclerView> (Resource.Id.LogRecyclerView);
            recyclerView.SetLayoutManager(new LinearLayoutManager(Activity));
            swipeLayout = view.FindViewById<SwipeRefreshLayout> (Resource.Id.LogSwipeContainer);
            swipeLayout.SetOnRefreshListener(this);
            StartStopBtn = view.FindViewById<StartStopFab> (Resource.Id.StartStopBtn);
            timerComponent = ((MainDrawerActivity)Activity).Timer; // TODO: a better way to do this?
            HasOptionsMenu = true;

            return view;
        }

        public override void OnViewCreated(View view, Bundle savedInstanceState)
        {
            base.OnViewCreated(view, savedInstanceState);

            // init viewModel
            ViewModel = new LogTimeEntriesVM(StoreManager.Singleton.AppState);

            //activeEntryBinding =  this.SetBinding (()=> ViewModel.ActiveEntry).WhenSourceChanges (OnActiveEntryChanged);
            collectionBinding = this.SetBinding(() => ViewModel.Collection).WhenSourceChanges(() =>
            {
                logAdapter = new LogTimeEntriesAdapter(recyclerView, ViewModel);
                recyclerView.SetAdapter(logAdapter);
            });
            isSyncingBinding = this.SetBinding(() => ViewModel.IsFullSyncing).WhenSourceChanges(SetSyncState);
            hasItemsBinding = this.SetBinding(() => ViewModel.Collection.Count).WhenSourceChanges(SetCollectionState);
            loadInfoBinding = this.SetBinding(() => ViewModel.LoadInfo).WhenSourceChanges(SetFooterState);
            fabBinding = this.SetBinding(() => ViewModel.IsEntryRunning, () => StartStopBtn.ButtonAction)
                         .ConvertSourceToTarget(isRunning => isRunning ? FABButtonState.Stop : FABButtonState.Start);

            newMenuBinding = this.SetBinding(() => ViewModel.IsEntryRunning)
                             .WhenSourceChanges(() =>
            {
                if (AddNewMenuItem != null)
                {
                    AddNewMenuItem.SetVisible(!ViewModel.IsEntryRunning);
                }
            });

            // Pass ViewModel to TimerComponent.
            timerComponent.SetViewModel(ViewModel);
            StartStopBtn.Click += StartStopClick;
            SetupRecyclerView(ViewModel);
        }


        public async void StartStopClick(object sender, EventArgs e)
        {
            ViewModel.ReportExperiment(OBMExperimentManager.StartButtonActionKey,
                                       OBMExperimentManager.ClickActionValue);

            if (!ViewModel.IsEntryRunning)
            {
                var te = await ViewModel.StartNewTimeEntryAsync();

                var ids = new List<string> { te.Id.ToString() };
                var intent = new Intent(Activity, typeof(EditTimeEntryActivity));
                intent.PutStringArrayListExtra(EditTimeEntryActivity.ExtraGroupedTimeEntriesGuids, ids);
                intent.PutExtra(EditTimeEntryActivity.IsGrouped, false);

                if (StoreManager.Singleton.AppState.Settings.ChooseProjectForNew)
                {
                    var startedByFAB = ViewModel.ActiveEntry.Data.State != TimeEntryState.Running;
                    intent.PutExtra(EditTimeEntryActivity.StartedByFab, startedByFAB);
                }

                StartActivity(intent);
            }
            else
            {
                ViewModel.StopTimeEntry();
            }
        }

        public override void OnDestroyView()
        {
            // Protect against Java side being GCed
            if (Handle == IntPtr.Zero)
            {
                return;
            }

            // TODO: Remove bindings to ViewModel
            // check if it is needed or not.
            timerComponent.DetachBindind();
            ReleaseRecyclerView();
            ViewModel.Dispose();
            base.OnDestroyView();
        }

        #region Menu setup
        public override void OnCreateOptionsMenu(IMenu menu, MenuInflater inflater)
        {
            inflater.Inflate(Resource.Menu.NewItemMenu, menu);
            AddNewMenuItem = menu.FindItem(Resource.Id.newItem);
            ConfigureOptionMenu();
        }

        public override bool OnOptionsItemSelected(IMenuItem item)
        {
            var i = new Intent(Activity, typeof(EditTimeEntryActivity));
            i.PutStringArrayListExtra(
                EditTimeEntryActivity.ExtraGroupedTimeEntriesGuids,
                new List<string> { ViewModel.ActiveEntry.Data.Id.ToString()});
            Activity.StartActivity(i);

            return base.OnOptionsItemSelected(item);
        }

        // Because the viewModel needs time to be created,
        // this method is called from two points
        private void ConfigureOptionMenu()
        {
            if (ViewModel != null && AddNewMenuItem != null)
            {
                AddNewMenuItem.SetVisible(!ViewModel.IsEntryRunning);
            }
        }
        #endregion

        #region IDismissListener implementation
        public bool CanDismiss(RecyclerView recyclerView, RecyclerView.ViewHolder viewHolder)
        {
            var adapter = recyclerView.GetAdapter();
            return adapter.GetItemViewType(viewHolder.LayoutPosition) == RecyclerCollectionDataAdapter<IHolder>.ViewTypeContent;
        }
        #endregion

        #region IRecyclerViewOnItemClickListener implementation
        public void OnItemClick(RecyclerView parent, View clickedView, int position)
        {
            var undoAdapter = (IUndoAdapter)parent.GetAdapter();
            if (undoAdapter.IsUndo(position))
            {
                return;
            }

            var intent = new Intent(Activity, typeof(EditTimeEntryActivity));
            IList<string> guids = ((ITimeEntryHolder)ViewModel.Collection.ElementAt(position)).Guids;
            intent.PutStringArrayListExtra(EditTimeEntryActivity.ExtraGroupedTimeEntriesGuids, guids);
            intent.PutExtra(EditTimeEntryActivity.IsGrouped, guids.Count > 1);

            StartActivity(intent);
        }

        public void OnItemLongClick(RecyclerView parent, View clickedView, int position)
        {
            OnItemClick(parent, clickedView, position);
        }

        public bool CanClick(RecyclerView view, int position)
        {
            var adapter = recyclerView.GetAdapter();
            return adapter.GetItemViewType(position) == RecyclerCollectionDataAdapter<IHolder>.ViewTypeContent;
        }
        #endregion

        #region Sync
        public void OnRefresh()
        {
            if (!ViewModel.IsNoUserMode)
                ViewModel.TriggerFullSync();
            else
                swipeLayout.Refreshing = false;
        }

        private void OnActiveEntryChanged()
        {
            var activeEntry = ViewModel.ActiveEntry.Data;
            if (activeEntry.State == TimeEntryState.Running)
            {
                var ids = new List<string> { activeEntry.Id.ToString() };
                var intent = new Intent(Activity, typeof(EditTimeEntryActivity));
                intent.PutStringArrayListExtra(EditTimeEntryActivity.ExtraGroupedTimeEntriesGuids, ids);
                intent.PutExtra(EditTimeEntryActivity.IsGrouped,  false);
                StartActivity(intent);
            }
        }

        private void SetSyncState()
        {
            // Full sync method.
            if (!swipeLayout.Refreshing)
            {
                return;
            }
            swipeLayout.Refreshing = ViewModel.IsFullSyncing;
            if (ViewModel.HasSyncErrors)
            {
                int msgId = Resource.String.LastSyncHadErrors;
                Snackbar.Make(coordinatorLayout, Resources.GetString(msgId), Snackbar.LengthLong).Show();
            }
        }

        private void SetFooterState()
        {
            var info = ViewModel.LoadInfo;

            if (!info.HasMore && !info.HadErrors)
            {
                logAdapter.SetFooterState(RecyclerCollectionDataAdapter<IHolder>.RecyclerLoadState.Finished);
                SetCollectionState();
            }
            else if (info.HasMore && !info.HadErrors)
            {
                logAdapter.SetFooterState(RecyclerCollectionDataAdapter<IHolder>.RecyclerLoadState.Loading);
            }
            else if (info.HasMore && info.HadErrors)
            {
                logAdapter.SetFooterState(RecyclerCollectionDataAdapter<IHolder>.RecyclerLoadState.Retry);
            }
        }

        private void SetCollectionState()
        {
            if (ViewModel.LoadInfo.IsSyncing && ViewModel.Collection.Count == 0)
            {
                return;
            }

            View emptyView = emptyMessageView;
            var isWelcome = ViewModel.ShowWelcomeScreen();
            var hasItems = ViewModel.Collection.Count > 0;
            var isInExperiment = ViewModel.IsInExperiment();

            // TODO RX: OBM Experiments
            if (isWelcome && isInExperiment)
            {
                emptyView = experimentEmptyView;
            }
            else
            {
                // always keeps this view hidden if it is not needed.
                experimentEmptyView.Visibility = ViewStates.Gone;
            }

            // According to settings, show welcome message or no.
            welcomeView.Visibility = isWelcome ? ViewStates.Visible : ViewStates.Gone;
            emptyView.Visibility = (!isWelcome && !hasItems) ? ViewStates.Visible : ViewStates.Gone;
            recyclerView.Visibility = hasItems ? ViewStates.Visible : ViewStates.Gone;
        }
        #endregion

        private void SetupRecyclerView(LogTimeEntriesVM viewModel)
        {
            // Touch listeners.
            itemTouchListener = new ItemTouchListener(recyclerView, this);
            recyclerView.AddOnItemTouchListener(itemTouchListener);

            // Scroll listener
            recyclerView.AddOnScrollListener(
                new ScrollListener((LinearLayoutManager)recyclerView.GetLayoutManager(), viewModel));

            var touchCallback = new SwipeDismissCallback(ItemTouchHelper.Up | ItemTouchHelper.Down, ItemTouchHelper.Left, this);
            var touchHelper = new ItemTouchHelper(touchCallback);
            touchHelper.AttachToRecyclerView(recyclerView);

            // Decorations.
            dividerDecoration = new DividerItemDecoration(Activity, DividerItemDecoration.VerticalList);
            shadowDecoration = new ShadowItemDecoration(Activity);
            recyclerView.AddItemDecoration(dividerDecoration);
            recyclerView.AddItemDecoration(shadowDecoration);
            recyclerView.GetItemAnimator().ChangeDuration = 0;
        }

        private void ReleaseRecyclerView()
        {
            recyclerView.RemoveItemDecoration(shadowDecoration);
            recyclerView.RemoveItemDecoration(dividerDecoration);
            recyclerView.RemoveOnItemTouchListener(itemTouchListener);

            recyclerView.GetAdapter().Dispose();
            recyclerView.Dispose();

            itemTouchListener.Dispose();
            dividerDecoration.Dispose();
            shadowDecoration.Dispose();
        }

        class ScrollListener : RecyclerView.OnScrollListener
        {
            private const int visibleThreshold = 5; // The minimum amount of items to have below your current scroll position before loading more.
            private int previousTotal; // The total number of items in the dataset after the last load
            private bool loading = true; // True if we are still waiting for the last set of data to load.
            private int firstVisibleItem, visibleItemCount, totalItemCount;
            private readonly LinearLayoutManager linearLayoutManager;
            private readonly LogTimeEntriesVM viewModel;

            public ScrollListener(LinearLayoutManager linearLayoutManager, LogTimeEntriesVM viewModel)
            {
                this.linearLayoutManager = linearLayoutManager;
                this.viewModel = viewModel;
            }

            public override void OnScrolled(RecyclerView recyclerView, int dx, int dy)
            {
                base.OnScrolled(recyclerView, dx, dy);

                visibleItemCount = recyclerView.ChildCount;
                totalItemCount = linearLayoutManager.ItemCount;
                firstVisibleItem = linearLayoutManager.FindFirstVisibleItemPosition();

                if (loading)
                {
                    if (totalItemCount > previousTotal)
                    {
                        loading = false;
                        previousTotal = totalItemCount;
                    }
                }

                if (!loading && (totalItemCount - visibleItemCount) <= (firstVisibleItem + visibleThreshold))
                {
                    loading = true;
                    // Request more entries.
                    viewModel.LoadMore();
                }
            }
        }
    }
}