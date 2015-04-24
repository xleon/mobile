using System;
using System.Collections.Specialized;
using System.Linq;
using Android.Content;
using Android.Graphics;
using Android.Graphics.Drawables;
using Android.OS;
using Android.Support.V7.Widget;
using Android.Util;
using Android.Views;
using Android.Widget;
using Toggl.Joey.UI.Utils;
using Toggl.Joey.UI.Views;
using Toggl.Phoebe;
using Toggl.Phoebe.Data;
using Toggl.Phoebe.Data.DataObjects;
using Toggl.Phoebe.Data.Models;
using Toggl.Phoebe.Data.Views;
using XPlatUtils;

namespace Toggl.Joey.UI.Adapters
{
    public class LogTimeEntriesAdapter : RecycledDataViewAdapter<object>
    {
        protected static readonly int ViewTypeLoaderPlaceholder = 0;
        protected static readonly int ViewTypeContent = 1;
        protected static readonly int ViewTypeDateHeader = ViewTypeContent + 1;
        private readonly Handler handler = new Handler ();

        public LogTimeEntriesAdapter () : base (new LogTimeEntriesView())
        {
        }

        public Action<TimeEntryModel> HandleTimeEntryDeletion { get; set; }

        public Action<TimeEntryModel> HandleTimeEntryEditing { get; set; }

        public Action<TimeEntryModel> HandleTimeEntryContinue { get; set; }

        public Action<TimeEntryModel> HandleTimeEntryStop { get; set; }

        protected override void CollectionChanged (NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Reset) {
                NotifyDataSetChanged();
            }

            if (e.Action == NotifyCollectionChangedAction.Add) {

                if (e.NewItems.Count == 0) {
                    return;
                }

                NotifyItemRangeInserted (e.NewStartingIndex, e.NewItems.Count);
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

        protected override RecyclerView.ViewHolder GetViewHolder (ViewGroup parent, int viewType)
        {
            View view;
            RecyclerView.ViewHolder holder;

            if (viewType == ViewTypeDateHeader) {
                view = LayoutInflater.FromContext (ServiceContainer.Resolve<Context> ()).Inflate (Resource.Layout.LogTimeEntryListSectionHeader, parent, false);
                holder = new HeaderListItemHolder (handler, view);
            } else {
                view = new LogTimeEntryItem (ServiceContainer.Resolve<Context> (), (IAttributeSet)null);
                holder = new TimeEntryListItemHolder (handler, this, view);
            }
            return holder;
        }

        protected override void BindHolder (RecyclerView.ViewHolder holder, int position)
        {
            if (GetItemViewType (position) == ViewTypeDateHeader) {
                var headerHolder = (HeaderListItemHolder)holder;
                headerHolder.Bind ((LogTimeEntriesView.DateGroup) GetEntry (position));
            } else {
                var entryHolder = (TimeEntryListItemHolder)holder;
                var model = new TimeEntryModel ((TimeEntryData) GetEntry (position));
                entryHolder.Bind (model);
            }
        }

        public override int GetItemViewType (int position)
        {
            if (position == DataView.Count) {
                return ViewTypeLoaderPlaceholder;
            }

            var obj = GetEntry (position);
            if (obj is LogTimeEntriesView.DateGroup) {
                return ViewTypeDateHeader;
            }
            return ViewTypeContent;
        }

        private class HeaderListItemHolder : RecycledBindableViewHolder<LogTimeEntriesView.DateGroup>
        {
            private readonly Handler handler;

            public TextView DateGroupTitleTextView { get; private set; }

            public TextView DateGroupDurationTextView { get; private set; }

            public HeaderListItemHolder (Handler handler, View root) : base (root)
            {
                this.handler = handler;
                DateGroupTitleTextView = root.FindViewById<TextView> (Resource.Id.DateGroupTitleTextView).SetFont (Font.RobotoLight);
                DateGroupDurationTextView = root.FindViewById<TextView> (Resource.Id.DateGroupDurationTextView).SetFont (Font.RobotoLight);
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

                var models = DataSource.DataObjects.Select (data => new TimeEntryModel (data)).ToList ();
                var duration = TimeSpan.FromSeconds (models.Sum (m => m.GetDuration ().TotalSeconds));
                DateGroupDurationTextView.Text = duration.ToString (@"hh\:mm\:ss");

                var runningModel = models.FirstOrDefault (m => m.State == TimeEntryState.Running);
                if (runningModel != null) {
                    handler.RemoveCallbacks (RebindDuration);
                    handler.PostDelayed (RebindDuration, 1000 - runningModel.GetDuration ().Milliseconds);
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

        private class TimeEntryListItemHolder : RecycledModelViewHolder<TimeEntryModel>, View.IOnClickListener
        {
            private readonly Handler handler;
            private readonly LogTimeEntriesAdapter adapter;
            private TimeEntryTagsView tagsView;

            public View ColorView { get; private set; }

            public TextView ProjectTextView { get; private set; }

            public TextView ClientTextView { get; private set; }

            public TextView TaskTextView { get; private set; }

            public TextView DescriptionTextView { get; private set; }

            public NotificationImageView TagsView { get; private set; }

            public View BillableView { get; private set; }

            public TextView DurationTextView { get; private set; }

            public ImageButton ContinueImageButton { get; private set; }

            public TimeEntryListItemHolder (Handler handler, LogTimeEntriesAdapter adapter, View root) : base (root)
            {
                this.handler = handler;
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

                root.SetOnClickListener (this);
                ContinueImageButton.Click += OnContinueButtonClicked;
            }

            private void OnContinueButtonClicked (object sender, EventArgs e)
            {
                if (DataSource == null) {
                    return;
                }

                if (DataSource.State == TimeEntryState.Running) {
                    adapter.OnStopTimeEntry (DataSource);
                    return;
                }
                adapter.OnContinueTimeEntry (DataSource);
            }

            protected override void OnDataSourceChanged ()
            {
                // Clear out old
                if (tagsView != null) {
                    tagsView.Updated -= OnTagsUpdated;
                    tagsView = null;
                }

                if (DataSource != null) {
                    tagsView = new TimeEntryTagsView (DataSource.Id);
                    tagsView.Updated += OnTagsUpdated;
                }

                RebindTags ();

                base.OnDataSourceChanged ();
            }

            protected override void Dispose (bool disposing)
            {
                if (disposing) {
                    if (tagsView != null) {
                        tagsView.Updated -= OnTagsUpdated;
                        tagsView = null;
                    }
                }
                base.Dispose (disposing);
            }

            private void OnTagsUpdated (object sender, EventArgs args)
            {
                RebindTags ();
            }

            protected override void ResetTrackedObservables ()
            {
                Tracker.MarkAllStale ();

                if (DataSource != null) {
                    Tracker.Add (DataSource, HandleTimeEntryPropertyChanged);

                    if (DataSource.Project != null) {
                        Tracker.Add (DataSource.Project, HandleProjectPropertyChanged);

                        if (DataSource.Project.Client != null) {
                            Tracker.Add (DataSource.Project.Client, HandleClientPropertyChanged);
                        }
                    }

                    if (DataSource.Task != null) {
                        Tracker.Add (DataSource.Task, HandleTaskPropertyChanged);
                    }
                }

                Tracker.ClearStale ();
            }

            private void HandleTimeEntryPropertyChanged (string prop)
            {
                if (prop == TimeEntryModel.PropertyProject
                        || prop == TimeEntryModel.PropertyTask
                        || prop == TimeEntryModel.PropertyState
                        || prop == TimeEntryModel.PropertyStartTime
                        || prop == TimeEntryModel.PropertyStopTime
                        || prop == TimeEntryModel.PropertyDescription
                        || prop == TimeEntryModel.PropertyIsBillable) {
                    Rebind ();
                }
            }

            private void HandleProjectPropertyChanged (string prop)
            {
                if (prop == ProjectModel.PropertyClient
                        || prop == ProjectModel.PropertyColor
                        || prop == ProjectModel.PropertyName) {
                    Rebind ();
                }
            }

            private void HandleClientPropertyChanged (string prop)
            {
                if (prop == ProjectModel.PropertyName) {
                    Rebind ();
                }
            }

            private void HandleTaskPropertyChanged (string prop)
            {
                if (prop == TaskModel.PropertyName) {
                    Rebind ();
                }
            }

            protected override void Rebind ()
            {
                // Protect against Java side being GCed
                if (Handle == IntPtr.Zero) {
                    return;
                }

                ResetTrackedObservables ();

                if (DataSource == null) {
                    return;
                }
                var ctx = ServiceContainer.Resolve<Context> ();

                if (DataSource.Project != null && DataSource.Project.Client != null) {
                    ClientTextView.Text = String.Format ("{0} • ", DataSource.Project.Client.Name);
                    ClientTextView.Visibility = ViewStates.Visible;
                } else {
                    ClientTextView.Text = String.Empty;
                    ClientTextView.Visibility = ViewStates.Gone;
                }

                if (DataSource.Task != null) {
                    TaskTextView.Text = String.Format ("{0} • ", DataSource.Task.Name);
                    TaskTextView.Visibility = ViewStates.Visible;
                } else {
                    TaskTextView.Text = String.Empty;
                    TaskTextView.Visibility = ViewStates.Gone;
                }

                var color = Color.Transparent;
                if (DataSource.Project != null) {
                    color = Color.ParseColor (DataSource.Project.GetHexColor ());
                    ProjectTextView.SetTextColor (color);
                    if (String.IsNullOrWhiteSpace (DataSource.Project.Name)) {
                        ProjectTextView.Text = ctx.GetString (Resource.String.RecentTimeEntryNamelessProject);
                    } else {
                        ProjectTextView.Text = DataSource.Project.Name;
                    }
                } else {
                    ProjectTextView.Text = ctx.GetString (Resource.String.RecentTimeEntryNoProject);
                    ProjectTextView.SetTextColor (ctx.Resources.GetColor (Resource.Color.dark_gray_text));
                }

                var shape = ColorView.Background as GradientDrawable;
                if (shape != null) {
                    shape.SetColor (color);
                }

                if (String.IsNullOrWhiteSpace (DataSource.Description)) {
                    if (DataSource.Task == null) {
                        DescriptionTextView.Text = ctx.GetString (Resource.String.RecentTimeEntryNoDescription);
                        DescriptionTextView.Visibility = ViewStates.Visible;
                    } else {
                        DescriptionTextView.Visibility = ViewStates.Gone;
                    }
                } else {
                    DescriptionTextView.Text = DataSource.Description;
                    DescriptionTextView.Visibility = ViewStates.Visible;
                }

                BillableView.Visibility = DataSource.IsBillable ? ViewStates.Visible : ViewStates.Gone;

                RebindDuration ();
            }

            private void RebindDuration ()
            {
                if (DataSource == null || Handle == IntPtr.Zero) {
                    return;
                }

                var duration = DataSource.GetDuration ();
                DurationTextView.Text = DataSource.GetFormattedDuration ();

                if (DataSource.State == TimeEntryState.Running) {
                    handler.RemoveCallbacks (RebindDuration);
                    handler.PostDelayed (RebindDuration, 1000 - duration.Milliseconds);
                }
                ShowStopButton ();
            }

            private void ShowStopButton ()
            {
                if (DataSource == null || Handle == IntPtr.Zero) {
                    return;
                }

                if (DataSource.State == TimeEntryState.Running) {
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

                var showTags = tagsView != null && tagsView.HasNonDefault;
                if (showTags) {
                    TagsView.BubbleCount = (int)tagsView.Count;
                }
                TagsView.Visibility = showTags ? ViewStates.Visible : ViewStates.Gone;
            }

            public void OnClick (View v)
            {
                // Temporal solution
                adapter.OnEditTimeEntry (DataSource);
            }
        }
    }
}
