using System;
using Android.Content;
using Android.Graphics;
using Android.Graphics.Drawables;
using Android.OS;
using Android.Support.V7.Widget;
using Android.Views;
using Android.Widget;
using GalaSoft.MvvmLight.Helpers;
using Toggl.Joey.UI.Utils;
using Toggl.Joey.UI.Views;
using Toggl.Phoebe;
using Toggl.Phoebe.Data;
using Toggl.Phoebe.Data.Models;
using Toggl.Phoebe.Data.Utils;
using Toggl.Phoebe.Data.ViewModels;
using XPlatUtils;

namespace Toggl.Joey.UI.Adapters
{
    public class LogTimeEntriesAdapter : RecyclerCollectionDataAdapter<IHolder>
    {
        public const int ViewTypeDateHeader = ViewTypeContent + 1;

        private readonly Handler handler = new Handler ();
        private static readonly int ContinueThreshold = 1;
        private DateTime lastTimeEntryContinuedTime;
        protected LogTimeEntriesViewModel ViewModel { get; set; }

        public LogTimeEntriesAdapter (IntPtr a, Android.Runtime.JniHandleOwnership b) : base (a, b)
        {
        }

        public LogTimeEntriesAdapter (RecyclerView owner, LogTimeEntriesViewModel viewModel)
        : base (owner, viewModel.Collection)
        {
            ViewModel = viewModel;
            lastTimeEntryContinuedTime = Time.UtcNow;
            this.SetBinding (() => ViewModel.HasMore).WhenSourceChanges (() => {
                HasMoreItems = ViewModel.HasMore;
            });
        }

        private async void OnContinueTimeEntry (RecyclerView.ViewHolder viewHolder)
        {
            // Don't continue a new TimeEntry before
            // x seconds has passed.
            if (Time.UtcNow < lastTimeEntryContinuedTime + TimeSpan.FromSeconds (ContinueThreshold)) {
                return;
            }
            lastTimeEntryContinuedTime = Time.UtcNow;

            await ViewModel.ContinueTimeEntryAsync (viewHolder.AdapterPosition);
        }

        protected override RecyclerView.ViewHolder GetViewHolder (ViewGroup parent, int viewType)
        {
            View view;
            RecyclerView.ViewHolder holder;

            if (viewType == ViewTypeDateHeader) {
                view = LayoutInflater.FromContext (ServiceContainer.Resolve<Context> ()).Inflate (Resource.Layout.LogTimeEntryListSectionHeader, parent, false);
                holder = new HeaderListItemHolder (handler, view);
            } else {
                view = LayoutInflater.FromContext (ServiceContainer.Resolve<Context> ()).Inflate (Resource.Layout.LogTimeEntryListItem, parent, false);
                holder = new TimeEntryListItemHolder (handler, this, view);
            }

            return holder;
        }

        protected override void BindHolder (RecyclerView.ViewHolder holder, int position)
        {
            var headerListItemHolder = holder as HeaderListItemHolder;
            if (headerListItemHolder != null) {
                headerListItemHolder.Bind ((DateHolder) GetItem (position));
                return;
            }

            var timeEntryListItemHolder = holder as TimeEntryListItemHolder;
            if (timeEntryListItemHolder != null) {
                timeEntryListItemHolder.Bind ((ITimeEntryHolder) GetItem (position));
            }
        }

        public override int GetItemViewType (int position)
        {
            var type = base.GetItemViewType (position);
            if (type != ViewTypeLoaderPlaceholder) {
                type = GetItem (position) is DateHolder ? ViewTypeDateHeader : ViewTypeContent;
            }
            return type;
        }

        public override void OnViewDetachedFromWindow (Java.Lang.Object holder)
        {
            if (holder is TimeEntryListItemHolder) {
                var mHolder = (TimeEntryListItemHolder)holder;
                mHolder.DataSource = null;
            } else if (holder is HeaderListItemHolder) {
                var mHolder = (HeaderListItemHolder)holder;
                mHolder.DisposeDataSource ();
            }
            base.OnViewDetachedFromWindow (holder);
        }

        [Shadow (ShadowAttribute.Mode.Top | ShadowAttribute.Mode.Bottom)]
        public class HeaderListItemHolder : RecycledBindableViewHolder<DateHolder>
        {
            private readonly Handler handler;

            public TextView DateGroupTitleTextView { get; private set; }

            public TextView DateGroupDurationTextView { get; private set; }

            public HeaderListItemHolder (IntPtr a, Android.Runtime.JniHandleOwnership b) : base (a, b)
            {
            }

            public HeaderListItemHolder (Handler handler, View root) : base (root)
            {
                this.handler = handler;
                DateGroupTitleTextView = root.FindViewById<TextView> (Resource.Id.DateGroupTitleTextView).SetFont (Font.RobotoMedium);
                DateGroupDurationTextView = root.FindViewById<TextView> (Resource.Id.DateGroupDurationTextView).SetFont (Font.Roboto);
            }

            protected override void Rebind ()
            {
                DateGroupTitleTextView.Text = GetRelativeDateString (DataSource.Date);
                RebindDuration ();
            }

            private void RebindDuration ()
            {
                if (DataSource == null || Handle == IntPtr.Zero) {
                    return;
                }

                var duration = DataSource.TotalDuration;
                DateGroupDurationTextView.Text = duration.ToString (@"hh\:mm\:ss");

                if (DataSource.IsRunning) {
                    handler.RemoveCallbacks (RebindDuration);
                    handler.PostDelayed (RebindDuration, 1000 - duration.Milliseconds);
                } else {
                    handler.RemoveCallbacks (RebindDuration);
                }
            }

            private static string GetRelativeDateString (DateTime dateTime)
            {
                var ctx = ServiceContainer.Resolve<Context> ();
                var ts = Time.Now.Date - dateTime.Date;
                switch (ts.Days) {
                case 0:
                    return ctx.Resources.GetString (Resource.String.Today);
                case 1:
                    return ctx.Resources.GetString (Resource.String.Yesterday);
                case -1:
                    return ctx.Resources.GetString (Resource.String.Tomorrow);
                default:
                    return dateTime.ToDeviceDateString ();
                }
            }
        }

        private class TimeEntryListItemHolder : RecyclerView.ViewHolder, View.IOnTouchListener
        {
            private readonly Handler handler;
            private readonly LogTimeEntriesAdapter owner;

            public ITimeEntryHolder DataSource { get; set; }
            public View ColorView { get; private set; }
            public TextView ProjectTextView { get; private set; }
            public TextView ClientTextView { get; private set; }
            public TextView TaskTextView { get; private set; }
            public TextView DescriptionTextView { get; private set; }
            public NotificationImageView TagsView { get; private set; }
            public View BillableView { get; private set; }
            public TextView DurationTextView { get; private set; }
            public ImageButton ContinueImageButton { get; private set; }

            public TimeEntryListItemHolder (IntPtr a, Android.Runtime.JniHandleOwnership b) : base (a, b)
            {
            }

            public TimeEntryListItemHolder (Handler handler, LogTimeEntriesAdapter owner, View root) : base (root)
            {
                this.handler = handler;
                this.owner = owner;

                ColorView = root.FindViewById<View> (Resource.Id.ColorView);
                ProjectTextView = root.FindViewById<TextView> (Resource.Id.ProjectTextView).SetFont (Font.RobotoMedium);
                ClientTextView = root.FindViewById<TextView> (Resource.Id.ClientTextView).SetFont (Font.RobotoMedium);
                TaskTextView = root.FindViewById<TextView> (Resource.Id.TaskTextView).SetFont (Font.RobotoMedium);
                DescriptionTextView = root.FindViewById<TextView> (Resource.Id.DescriptionTextView).SetFont (Font.Roboto);
                TagsView = root.FindViewById<NotificationImageView> (Resource.Id.TagsIcon);
                BillableView = root.FindViewById<View> (Resource.Id.BillableIcon);
                DurationTextView = root.FindViewById<TextView> (Resource.Id.DurationTextView).SetFont (Font.RobotoLight);
                ContinueImageButton = root.FindViewById<ImageButton> (Resource.Id.ContinueImageButton);
                ContinueImageButton.SetOnTouchListener (this);
            }

            public bool OnTouch (View v, MotionEvent e)
            {
                switch (e.Action) {
                case MotionEventActions.Up:
                    owner.OnContinueTimeEntry (this);
                    return false;
                }

                return false;
            }

            public void Bind (ITimeEntryHolder datasource)
            {
                DataSource = datasource;

                if (DataSource == null || Handle == IntPtr.Zero) {
                    return;
                }

                var color = Color.Transparent;
                var ctx = ServiceContainer.Resolve<Context> ();

                var info = DataSource.Info;
                if (!String.IsNullOrWhiteSpace (info.ProjectData.Name)) {
                    color = Color.ParseColor (ProjectModel.HexColors [info.Color % ProjectModel.HexColors.Length]);
                    ProjectTextView.SetTextColor (color);
                    ProjectTextView.Text = info.ProjectData.Name;
                } else {
                    ProjectTextView.Text = ctx.GetString (Resource.String.RecentTimeEntryNoProject);
                    ProjectTextView.SetTextColor (ctx.Resources.GetColor (Resource.Color.dark_gray_text));
                }

                if (String.IsNullOrWhiteSpace (info.ClientData.Name)) {
                    ClientTextView.Text = String.Empty;
                    ClientTextView.Visibility = ViewStates.Gone;
                } else {
                    ClientTextView.Text = String.Format ("{0} • ", info.ClientData.Name);
                    ClientTextView.Visibility = ViewStates.Visible;
                }

                if (String.IsNullOrWhiteSpace (info.TaskData.Name)) {
                    TaskTextView.Text = String.Empty;
                    TaskTextView.Visibility = ViewStates.Gone;
                } else {
                    TaskTextView.Text = String.Format ("{0} • ", info.TaskData.Name);
                    TaskTextView.Visibility = ViewStates.Visible;
                }

                if (String.IsNullOrWhiteSpace (info.Description)) {
                    DescriptionTextView.Text = ctx.GetString (Resource.String.RecentTimeEntryNoDescription);
                } else {
                    DescriptionTextView.Text = info.Description;
                }

                BillableView.Visibility = info.IsBillable ? ViewStates.Visible : ViewStates.Gone;


                var shape = ColorView.Background as GradientDrawable;
                if (shape != null) {
                    shape.SetColor (color);
                }

                RebindTags ();
                RebindDuration ();
            }

            private void RebindDuration ()
            {
                if (DataSource == null || Handle == IntPtr.Zero) {
                    return;
                }

                var duration = DataSource.GetDuration ();
                DurationTextView.Text = TimeEntryModel.GetFormattedDuration (duration);

                if (DataSource.Data.State == TimeEntryState.Running) {
                    handler.RemoveCallbacks (RebindDuration);
                    handler.PostDelayed (RebindDuration, 1000 - duration.Milliseconds);
                } else {
                    handler.RemoveCallbacks (RebindDuration);
                }

                ShowStopButton ();
            }

            private void ShowStopButton ()
            {
                if (DataSource == null || Handle == IntPtr.Zero) {
                    return;
                }

                if (DataSource.Data.State == TimeEntryState.Running) {
                    ContinueImageButton.SetImageResource (Resource.Drawable.IcStop);
                } else {
                    ContinueImageButton.SetImageResource (Resource.Drawable.IcPlayArrowGrey);
                }
            }

            private void RebindTags ()
            {
                // Protect against Java side being GCed
                if (Handle == IntPtr.Zero) {
                    return;
                }

                var numberOfTags = DataSource.Info.NumberOfTags;
                TagsView.BubbleCount = numberOfTags;
                TagsView.Visibility = numberOfTags > 0 ? ViewStates.Visible : ViewStates.Gone;
            }
        }
    }
}
