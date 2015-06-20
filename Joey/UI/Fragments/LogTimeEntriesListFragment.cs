using System;
using System.Collections.Generic;
using Android.Animation;
using Android.Content;
using Android.Graphics;
using Android.Graphics.Drawables;
using Android.OS;
using Android.Support.V4.App;
using Android.Support.V7.Widget;
using Android.Support.V7.Widget.Helper;
using Android.Util;
using Android.Views;
using Android.Widget;
using Toggl.Joey.Data;
using Toggl.Joey.UI.Activities;
using Toggl.Joey.UI.Adapters;
using Toggl.Joey.UI.Utils;
using Toggl.Joey.UI.Views;
using Toggl.Phoebe;
using Toggl.Phoebe.Data;
using Toggl.Phoebe.Data.Utils;
using Toggl.Phoebe.Data.Views;
using XPlatUtils;

namespace Toggl.Joey.UI.Fragments
{
    public class LogTimeEntriesListFragment : Fragment, SwipeDismissTouchListener.IDismissCallbacks, ItemTouchListener.IItemTouchListener
    {
        private static readonly int UndbarVisibleTime = 6000;
        private static readonly int UndbarScrollThreshold = 100;

        private RecyclerView recyclerView;
        private View emptyMessageView;
        private Subscription<SettingChangedMessage> subscriptionSettingChanged;
        private LogTimeEntriesAdapter logAdapter;
        private readonly Handler handler = new Handler ();
        private FrameLayout undoBar;
        private Button undoButton;
        private bool isUndoShowed;

        // Recycler setup
        private DividerItemDecoration dividerDecoration;
        private ShadowItemDecoration shadowDecoration;
        private ItemTouchListener itemTouchListener;
        private RecyclerViewScrollDetector scrollListener;

        public override View OnCreateView (LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            var view = inflater.Inflate (Resource.Layout.LogTimeEntriesListFragment, container, false);
            view.FindViewById<TextView> (Resource.Id.EmptyTitleTextView).SetFont (Font.Roboto);
            view.FindViewById<TextView> (Resource.Id.EmptyTextTextView).SetFont (Font.RobotoLight);

            emptyMessageView = view.FindViewById<View> (Resource.Id.EmptyMessageView);
            emptyMessageView.Visibility = ViewStates.Gone;
            recyclerView = view.FindViewById<RecyclerView> (Resource.Id.LogRecyclerView);
            recyclerView.SetLayoutManager (new LinearLayoutManager (Activity));

            undoBar = view.FindViewById<FrameLayout> (Resource.Id.UndoBar);
            undoButton = view.FindViewById<Button> (Resource.Id.UndoButton);
            undoButton.Click += UndoBtnClicked;

            return view;
        }

        public override void OnViewCreated (View view, Bundle savedInstanceState)
        {
            base.OnViewCreated (view, savedInstanceState);

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

        private void EnsureAdapter ()
        {
            if (recyclerView.GetAdapter() == null) {
                var isGrouped = ServiceContainer.Resolve<SettingsStore> ().GroupedTimeEntries;
                if (isGrouped) {
                    logAdapter = new LogTimeEntriesAdapter (recyclerView, new GroupedTimeEntriesView());
                } else {
                    logAdapter = new LogTimeEntriesAdapter (recyclerView, new LogTimeEntriesView());
                }
                recyclerView.SetAdapter (logAdapter);
                SetupRecyclerView ();
            }
        }

        public override void OnDestroyView ()
        {
            var bus = ServiceContainer.Resolve<MessageBus> ();
            if (subscriptionSettingChanged != null) {
                bus.Unsubscribe (subscriptionSettingChanged);
                subscriptionSettingChanged = null;
            }

            ReleaseRecyclerView ();

            base.OnDestroyView ();
        }

        private void SetupRecyclerView ()
        {
            // Touch listeners.
            itemTouchListener = new ItemTouchListener (recyclerView, this);
            recyclerView.AddOnItemTouchListener (itemTouchListener);

            var touchCallback = new RecyclerSwipeCallback (ItemTouchHelper.Up | ItemTouchHelper.Down, ItemTouchHelper.Left);
            var touchHelper = new ItemTouchHelper (touchCallback);
            touchHelper.AttachToRecyclerView (recyclerView);

            // Decorations.
            dividerDecoration = new DividerItemDecoration (Activity, DividerItemDecoration.VerticalList);
            shadowDecoration = new ShadowItemDecoration (Activity);
            recyclerView.AddItemDecoration (dividerDecoration);
            recyclerView.AddItemDecoration (shadowDecoration);

            scrollListener = new RecyclerViewScrollDetector (this);
            recyclerView.AddOnScrollListener (scrollListener);
            recyclerView.GetItemAnimator ().SupportsChangeAnimations = false;
        }

        private void ReleaseRecyclerView ()
        {
            recyclerView.RemoveOnScrollListener (scrollListener);
            recyclerView.RemoveItemDecoration (shadowDecoration);
            recyclerView.RemoveItemDecoration (dividerDecoration);
            recyclerView.RemoveOnItemTouchListener (itemTouchListener);

            recyclerView.GetAdapter ().Dispose ();
            recyclerView.Dispose ();
            logAdapter = null;

            itemTouchListener.Dispose ();
            dividerDecoration.Dispose ();
            shadowDecoration.Dispose ();
            scrollListener.Dispose ();
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

        #region IDismissCallbacks implementation

        public bool CanDismiss (RecyclerView view, int position)
        {
            var adapter = view.GetAdapter ();
            return adapter.GetItemViewType (position) == LogTimeEntriesAdapter.ViewTypeContent;
        }

        public void OnDismiss (RecyclerView view, int position)
        {
            var undoAdapter = recyclerView.GetAdapter () as IUndoCapabilities;
            undoAdapter.RemoveItemWithUndo (position);
            ShowUndoBar ();
        }

        #endregion

        #region IRecyclerViewOnItemClickListener implementation

        public void OnItemClick (RecyclerView parent, View clickedView, int position)
        {
            var intent = new Intent (Activity, typeof (EditTimeEntryActivity));

            IList<string> guids = ((TimeEntryHolder)logAdapter.GetEntry (position)).TimeEntryGuids;
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
            return CanDismiss (view, position);
        }

        #endregion

        #region Undo bar

        private void ShowUndoBar ()
        {
            if (!UndoBarVisible) {
                UndoBarVisible = true;
            }
            handler.RemoveCallbacks (RemoveItemAndHideUndoBar);
            handler.PostDelayed (RemoveItemAndHideUndoBar, UndbarVisibleTime);
        }

        public void RemoveItemAndHideUndoBar ()
        {
            UndoBarVisible = false;

            // Remove item permanently
            var undoAdapter = recyclerView.GetAdapter () as IUndoCapabilities;
            if (undoAdapter != null) {
                undoAdapter.ConfirmItemRemove ();
            }
        }

        private void UndoBtnClicked (object sender, EventArgs e)
        {
            // Undo remove item.
            var undoAdapter = recyclerView.GetAdapter () as IUndoCapabilities;
            undoAdapter.RestoreItemFromUndo ();

            handler.RemoveCallbacks (ShowUndoBar);
            UndoBarVisible = false;
        }

        public bool UndoBarVisible
        {
            get {
                return isUndoShowed;
            } set {
                if (isUndoShowed == value) {
                    return;
                }
                isUndoShowed = value;

                var targetTranY = isUndoShowed ? 0.0f : 160.0f;
                ValueAnimator animator = ValueAnimator.OfFloat (undoBar.TranslationY, targetTranY);
                animator.SetDuration (500);
                animator.Update += (sender, e) => {
                    undoBar.TranslationY = (float)animator.AnimatedValue;
                };
                animator.Start();
            }
        }

        #endregion

        private class RecyclerViewScrollDetector : RecyclerView.OnScrollListener
        {
            private readonly LogTimeEntriesListFragment owner;

            public RecyclerViewScrollDetector (LogTimeEntriesListFragment owner)
            {
                this.owner = owner;
                ScrollThreshold = UndbarScrollThreshold;
            }

            public int ScrollThreshold { get; set; }

            public RecyclerView.OnScrollListener OnScrollListener { get; set; }

            public override void OnScrolled (RecyclerView recyclerView, int dx, int dy)
            {
                if (OnScrollListener != null) {
                    OnScrollListener.OnScrolled (recyclerView, dx, dy);
                }

                var isSignificantDelta = Math.Abs (dy) > ScrollThreshold;
                if (isSignificantDelta) {
                    OnScrollMoved();
                }
            }

            public override void OnScrollStateChanged (RecyclerView recyclerView, int newState)
            {
                if (OnScrollListener != null) {
                    OnScrollListener.OnScrollStateChanged (recyclerView, newState);
                }
                base.OnScrollStateChanged (recyclerView, newState);
            }

            private void OnScrollMoved()
            {
                if (owner.UndoBarVisible) {
                    owner.RemoveItemAndHideUndoBar ();
                }
            }
        }

        private class RecyclerSwipeCallback : ItemTouchHelper.SimpleCallback
        {
            private const float minThreshold = 20;

            private int leftBorderWidth;
            private Drawable backgroundShape;
            private Context ctx;
            private string deleteText;
            private Paint labelPaint;
            private Rect rect = new Rect();

            private Context Ctx
            {
                get {
                    if (ctx == null) {
                        ctx = ServiceContainer.Resolve<Context> ();
                    }
                    return ctx;
                }
            }

            private string DeleteText
            {
                get {
                    if (string.IsNullOrEmpty (deleteText)) {
                        deleteText = ctx.Resources.GetString (Resource.String.SwipeDeleteQuestion);
                    }
                    return deleteText;
                }
            }

            private Drawable BackgroundShape
            {
                get {
                    if (backgroundShape == null) {
                        backgroundShape = Ctx.Resources.GetDrawable (Resource.Drawable.swipe_background_shape);
                    }
                    return backgroundShape;
                }
            }

            private Paint LabelPaint
            {
                get {
                    if (labelPaint == null) {
                        var labelFontSize = TypedValue.ApplyDimension (ComplexUnitType.Sp, 14, ctx.Resources.DisplayMetrics);
                        labelPaint = new Paint {
                            Color = Color.White,
                            TextSize = labelFontSize,
                            AntiAlias = true,
                        };
                        labelPaint.GetTextBounds (DeleteText, 0, DeleteText.Length, rect);
                        leftBorderWidth = (int)TypedValue.ApplyDimension (ComplexUnitType.Dip, 32, ctx.Resources.DisplayMetrics);
                    }
                    return labelPaint;
                }
            }

            public RecyclerSwipeCallback (IntPtr a, Android.Runtime.JniHandleOwnership b) : base (a, b)
            {
            }

            public RecyclerSwipeCallback (int p0, int p1) : base (p0, p1)
            {
            }

            public override bool OnMove (RecyclerView p0, RecyclerView.ViewHolder p1, RecyclerView.ViewHolder p2)
            {
                return false;
            }

            public override int GetSwipeDirs (RecyclerView p0, RecyclerView.ViewHolder p1)
            {
                var adapter = ((LogTimeEntriesAdapter)p0.GetAdapter ());
                if (adapter.GetItemViewType (p1.AdapterPosition) == LogTimeEntriesAdapter.ViewTypeContent) {
                    return ItemTouchHelper.Right;
                }
                return 0;
            }

            public override void OnSwiped (RecyclerView.ViewHolder p0, int p1)
            {
            }

            public override float GetSwipeThreshold (RecyclerView.ViewHolder p0)
            {
                return minThreshold;
            }

            public override void OnChildDraw (Canvas c, RecyclerView recyclerView, RecyclerView.ViewHolder viewHolder, float dX, float dY, int actionState, bool isCurrentlyActive)
            {
                var itemHeight = viewHolder.ItemView.Height;
                BackgroundShape.SetBounds (0, (int)viewHolder.ItemView.GetY (), c.Width, (int)viewHolder.ItemView.GetY () + itemHeight);
                BackgroundShape.Draw (c);
                c.DrawText (DeleteText, leftBorderWidth, viewHolder.ItemView.GetY () + itemHeight /2.0f + rect.Height ()/2.0f, LabelPaint);

                base.OnChildDraw (c, recyclerView, viewHolder, dX, dY, actionState, isCurrentlyActive);
            }

            protected override void Dispose (bool disposing)
            {
                if (disposing && backgroundShape != null) {
                    backgroundShape.Dispose ();
                    labelPaint.Dispose ();
                }

                base.Dispose (disposing);
            }
        }
    }
}
