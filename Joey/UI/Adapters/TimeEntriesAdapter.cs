using System;
using System.Collections.Specialized;
using Android.Content;
using Android.Graphics;
using Android.OS;
using Android.Support.V7.Widget;
using Android.Util;
using Android.Views;
using Android.Widget;
using Toggl.Joey.UI.Utils;
using Toggl.Joey.UI.Views;
using Toggl.Phoebe;
using Toggl.Phoebe.Data.Models;
using Toggl.Phoebe.Data.Utils;
using Toggl.Phoebe.Data.Views;
using XPlatUtils;
using Android.Views.Animations;
using Toggl.Phoebe.Data.DataObjects;

namespace Toggl.Joey.UI.Adapters
{
    public class TimeEntriesAdapter : RecyclerView.Adapter
    {
        protected static readonly int ViewTypeLoaderPlaceholder = 0;
        protected static readonly int ViewTypeContent = 1;
        protected static readonly int ViewTypeDateHeader = 2;

        private readonly Handler handler = new Handler ();
        private AllTimeEntriesViewModel viewModel;
        private ObservableRangeCollection<object> items;

        public TimeEntriesAdapter (AllTimeEntriesViewModel viewModel)
        {
            this.viewModel = viewModel;
            items = viewModel.CollectionData;
        }

        public override void OnAttachedToRecyclerView (RecyclerView p0)
        {
            items.CollectionChanged += OnCollectionChanged;
        }

        private void OnCollectionChanged (object sender, NotifyCollectionChangedEventArgs e)
        {
            // Protect against Java side being GCed
            if (Handle == IntPtr.Zero) {
                return;
            }

            if (e.Action == NotifyCollectionChangedAction.Reset) {
                NotifyDataSetChanged();
            }

            if (e.Action == NotifyCollectionChangedAction.Add) {
                if (e.NewItems.Count > 1) {
                    NotifyItemRangeInserted (e.NewStartingIndex, e.NewItems.Count);
                } else {
                    NotifyItemInserted (e.NewStartingIndex);
                }
            }

            if (e.Action == NotifyCollectionChangedAction.Replace) {
                NotifyItemChanged (e.NewStartingIndex);
            }

            if (e.Action == NotifyCollectionChangedAction.Remove) {
                NotifyItemRemoved (e.OldStartingIndex);
            }

            if (e.Action == NotifyCollectionChangedAction.Move) {
                NotifyItemMoved (e.OldStartingIndex, e.NewStartingIndex);
            }
        }

        private void OnDeleteTimeEntry (TimeEntryModel model)
        {
            var handler = HandleTimeEntryDeletion;
            if (handler != null) {
                handler (model);
            }
        }

        private void OnEditTimeEntry (TimeEntryModel model)
        {
            var handler = HandleTimeEntryEditing;
            if (handler != null) {
                handler (model);
            }
        }

        private void OnContinueTimeEntry (TimeEntryModel model)
        {
            var handler = HandleTimeEntryContinue;
            if (handler != null) {
                handler (model);
            }
        }

        private void OnStopTimeEntry (TimeEntryModel model)
        {
            var handler = HandleTimeEntryStop;
            if (handler != null) {
                handler (model);
            }
        }

        public Action<TimeEntryModel> HandleTimeEntryDeletion { get; set; }

        public Action<TimeEntryModel> HandleTimeEntryEditing { get; set; }

        public Action<TimeEntryModel> HandleTimeEntryContinue { get; set; }

        public Action<TimeEntryModel> HandleTimeEntryStop { get; set; }

        public override RecyclerView.ViewHolder OnCreateViewHolder (ViewGroup parent, int viewType)
        {
            View view;
            RecyclerView.ViewHolder holder;

            if (viewType == ViewTypeLoaderPlaceholder) {
                view = GetLoadIndicatorView (parent);
                holder = new SpinnerHolder (view);
            } else if (viewType == ViewTypeDateHeader) {
                view = LayoutInflater.FromContext (ServiceContainer.Resolve<Context> ()).Inflate (Resource.Layout.LogTimeEntryListSectionHeader, parent, false);
                holder = new HeaderListItemHolder (view);
            } else {
                view = new LogTimeEntryItem (ServiceContainer.Resolve<Context> (), (IAttributeSet)null);
                holder = new TimeEntryListItemHolder (this, view);
            }
            return holder;
        }

        public override void OnBindViewHolder (RecyclerView.ViewHolder holder, int position)
        {
            if (GetItemViewType (position) == ViewTypeLoaderPlaceholder) {
                return;
            }

            if (GetItemViewType (position) == ViewTypeDateHeader) {
                var headerHolder = (HeaderListItemHolder)holder;
                headerHolder.Bind ((AllTimeEntriesViewModel.DateGroup)items[position]);
            } else {
                var entryHolder = (TimeEntryListItemHolder)holder;
                var model = new TimeEntryModel ((TimeEntryData)items[position]);
                entryHolder.Bind (model);
            }
        }

        public override int ItemCount
        {
            get {
                if (viewModel.IsLoading) {
                    return items.Count + 1;
                }
                return items.Count;
            }
        }

        public override int GetItemViewType (int position)
        {
            if (position == items.Count && viewModel.IsLoading) {
                return ViewTypeLoaderPlaceholder;
            }

            var obj = items[position];
            if (obj is AllTimeEntriesViewModel.DateGroup) {
                return ViewTypeDateHeader;
            }
            return ViewTypeContent;
        }

        public class HeaderListItemHolder : RecyclerView.ViewHolder
        {
            public TextView DateGroupTitleTextView { get; private set; }

            public TextView DateGroupDurationTextView { get; private set; }

            public HeaderListItemHolder (View root) : base (root)
            {
                DateGroupTitleTextView = root.FindViewById<TextView> (Resource.Id.DateGroupTitleTextView).SetFont (Font.RobotoLight);
                DateGroupDurationTextView = root.FindViewById<TextView> (Resource.Id.DateGroupDurationTextView).SetFont (Font.RobotoLight);
            }

            public void Bind (AllTimeEntriesViewModel.DateGroup dateGroup)
            {
                DateGroupTitleTextView.Text = GetRelativeDateString (dateGroup.Date);
                //DateGroupDurationTextView.Text = dateGroup.Duration.ToString (@"hh\:mm\:ss");
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

        private class TimeEntryListItemHolder : RecyclerView.ViewHolder
        {
            private readonly TimeEntriesAdapter adapter;

            public View ColorView { get; private set; }

            public TextView ProjectTextView { get; private set; }

            public TextView ClientTextView { get; private set; }

            public TextView TaskTextView { get; private set; }

            public TextView DescriptionTextView { get; private set; }

            public NotificationImageView TagsView { get; private set; }

            public View BillableView { get; private set; }

            public TextView DurationTextView { get; private set; }

            public ImageButton ContinueImageButton { get; private set; }

            public TimeEntryListItemHolder (TimeEntriesAdapter adapter, View root) : base (root)
            {
                this.adapter = adapter;

                ColorView = root.FindViewById<View> (Resource.Id.ColorView);
                ProjectTextView = root.FindViewById<TextView> (Resource.Id.ProjectTextView).SetFont (Font.Roboto);
                ClientTextView = root.FindViewById<TextView> (Resource.Id.ClientTextView).SetFont (Font.Roboto);
                TaskTextView = root.FindViewById<TextView> (Resource.Id.TaskTextView).SetFont (Font.Roboto);
                DescriptionTextView = root.FindViewById<TextView> (Resource.Id.DescriptionTextView).SetFont (Font.RobotoLight);
                TagsView = root.FindViewById<NotificationImageView> (Resource.Id.TagsIcon);
                BillableView = root.FindViewById<View> (Resource.Id.BillableIcon);
                DurationTextView = root.FindViewById<TextView> (Resource.Id.DurationTextView).SetFont (Font.RobotoLight);
                ContinueImageButton = root.FindViewById<ImageButton> (Resource.Id.ContinueImageButton);

                ContinueImageButton.Click += OnContinueButtonClicked;
            }

            public void Bind (TimeEntryModel model)
            {
                // Protect against Java side being GCed
                if (Handle == IntPtr.Zero) {
                    return;
                }

                if (model == null) {
                    return;
                }
                var ctx = ServiceContainer.Resolve<Context> ();

                if (model.Project != null && model.Project.Client != null) {
                    ClientTextView.Text = String.Format ("{0} • ", model.Project.Client.Name);
                    ClientTextView.Visibility = ViewStates.Visible;
                } else {
                    ClientTextView.Text = String.Empty;
                    ClientTextView.Visibility = ViewStates.Gone;
                }

                if (model.Task != null) {
                    TaskTextView.Text = String.Format ("{0} • ", model.Task.Name);
                    TaskTextView.Visibility = ViewStates.Visible;
                } else {
                    TaskTextView.Text = String.Empty;
                    TaskTextView.Visibility = ViewStates.Gone;
                }

                var color = Color.Transparent;
                if (model.Project != null) {
                    color = Color.ParseColor (model.Project.GetHexColor ());
                    ProjectTextView.SetTextColor (color);
                    if (String.IsNullOrWhiteSpace (model.Project.Name)) {
                        ProjectTextView.Text = ctx.GetString (Resource.String.RecentTimeEntryNamelessProject);
                    } else {
                        ProjectTextView.Text = model.Project.Name;
                    }
                } else {
                    ProjectTextView.Text = ctx.GetString (Resource.String.RecentTimeEntryNoProject);
                    ProjectTextView.SetTextColor (ctx.Resources.GetColor (Resource.Color.dark_gray_text));
                }

                var shape = ColorView.Background as Android.Graphics.Drawables.GradientDrawable;
                if (shape != null) {
                    shape.SetColor (color);
                }

                if (String.IsNullOrWhiteSpace (model.Description)) {
                    if (model.Task == null) {
                        DescriptionTextView.Text = ctx.GetString (Resource.String.RecentTimeEntryNoDescription);
                        DescriptionTextView.Visibility = ViewStates.Visible;
                    } else {
                        DescriptionTextView.Visibility = ViewStates.Gone;
                    }
                } else {
                    DescriptionTextView.Text = model.Description;
                    DescriptionTextView.Visibility = ViewStates.Visible;
                }

                BillableView.Visibility = model.IsBillable ? ViewStates.Visible : ViewStates.Gone;

                RebindDuration (model);
            }

            private void RebindDuration (TimeEntryModel model)
            {
                if (model == null || Handle == IntPtr.Zero) {
                    return;
                }

                var duration = model.GetDuration ();
                DurationTextView.Text = model.GetFormattedDuration ();
                ShowStopButton (model);
            }

            private void ShowStopButton (TimeEntryModel model)
            {
                if (model.State == Toggl.Phoebe.Data.TimeEntryState.Running) {
                    ContinueImageButton.SetImageResource (Resource.Drawable.IcStop);
                } else {
                    ContinueImageButton.SetImageResource (Resource.Drawable.IcPlayArrowGrey);
                }
            }

            private void OnContinueButtonClicked (object sender, EventArgs e)
            {

            }
        }

        private class SpinnerHolder : RecyclerView.ViewHolder
        {
            public SpinnerHolder (View root) : base (root)
            {
            }
        }

        private View GetLoadIndicatorView (ViewGroup parent)
        {
            var view = LayoutInflater.FromContext (parent.Context).Inflate (
                           Resource.Layout.TimeEntryListLoadingItem, parent, false);

            ImageView spinningImage = view.FindViewById<ImageView> (Resource.Id.LoadingImageView);
            Animation spinningImageAnimation = AnimationUtils.LoadAnimation (parent.Context, Resource.Animation.SpinningAnimation);
            spinningImage.StartAnimation (spinningImageAnimation);

            return view;
        }
    }
}
