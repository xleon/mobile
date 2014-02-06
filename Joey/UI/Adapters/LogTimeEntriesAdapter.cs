using System;
using System.Linq;
using System.Collections.Generic;
using Android.OS;
using Android.Views;
using Android.Widget;
using Android.Graphics;
using Toggl.Phoebe;
using Toggl.Phoebe.Data;
using Toggl.Phoebe.Data.Models;
using Toggl.Phoebe.Data.Views;
using XPlatUtils;

namespace Toggl.Joey.UI.Adapters
{
    public class LogTimeEntriesAdapter : BaseModelsViewAdapter<TimeEntryModel>
    {
        protected static readonly int ViewTypeDateHeader = ViewTypeContent + 1;

        private readonly List<HeaderPosition> headers = new List<HeaderPosition> ();

        private class HeaderPosition{ 
            public int Position { get; set; }
            public DateTime Date { get; set; }

            public HeaderPosition(int position, DateTime date)
            {
                this.Position = position;
                this.Date = date;
            }
        }

        public LogTimeEntriesAdapter () : base (new AllTimeEntriesView ())
        {
            UpdateHeaders ();
        }

        public override void NotifyDataSetChanged ()
        {
            UpdateHeaders ();
            base.NotifyDataSetChanged ();
        }

        private void UpdateHeaders ()
        {
            headers.Clear ();

            if (ModelsView.Count == 0)
                return;

            TimeEntryModel model = ModelsView.Models.ElementAt (0);

            headers.Add (new HeaderPosition (0, model.StartTime));

            TimeEntryModel prevModel = model;

            for (int i = 1; i < ModelsView.Count; i++) {
                model = ModelsView.Models.ElementAt (i);
                var date = model.StartTime.Date;
                var isNewDay = prevModel.StartTime.Date.CompareTo (date) != 0;
                if (isNewDay) {
                    headers.Add (new HeaderPosition (i + headers.Count, date));
                }
                prevModel = model;
            }
        }

        private HeaderPosition GetHeaderAt(int position)
        {
            return headers.FirstOrDefault ((h) => h.Position == position);
        }

        private int GetIndexForPosition (int position)
        {
            if (GetHeaderAt (position) != null) // This is header position, there is no index for that
                return GetIndexForPosition (position + 1); // Return next position (which is probably an actual item)

            int numberSections = 0;
            foreach (HeaderPosition header in headers) {
                if (position > header.Position) {
                    numberSections++;
                }
            }

            return position - numberSections;
        }

        public override bool IsEnabled (int position)
        {
            return GetHeaderAt (position) == null;
        }

        public override TimeEntryModel GetModel (int position)
        {
            var modelIndex = GetIndexForPosition (position);
            if (modelIndex >= ModelsView.Models.Count())
                return null;
            
            return ModelsView.Models.ElementAt (modelIndex);
        }

        public override int GetItemViewType (int position)
        {
            if (GetIndexForPosition(position) == ModelsView.Count && ModelsView.IsLoading)
                return ViewTypeLoaderPlaceholder;

            if (GetHeaderAt (position) == null)
                return ViewTypeContent;
            else
                return ViewTypeDateHeader;
        }

        public override int ViewTypeCount {
            get { return base.ViewTypeCount + 1; }
        }

        public override int Count {
            get {
                return base.Count + headers.Count;
            }
        }

        protected override View GetModelView (int position, View convertView, ViewGroup parent)
        {
            View view = convertView;

            var model = GetModel (position);

            if (GetHeaderAt (position) != null) {
                if (view == null) {
                    view = LayoutInflater.FromContext (parent.Context).Inflate (
                        Resource.Layout.LogTimeEntryListSectionHeader, parent, false);
                }
                view.FindViewById<TextView> (Resource.Id.DateGroupTitleTextView).Text = GetHeaderAt (position).Date.ToShortDateString ();
            } else {
                if (view == null) {
                    view = LayoutInflater.FromContext (parent.Context).Inflate (
                        Resource.Layout.LogTimeEntryListItem, parent, false);
                    view.Tag = new TimeEntryListItemHolder (view);
                }
                var holder = (TimeEntryListItemHolder) view.Tag;
                holder.Bind (model);
            }

            return view;
        }

        private class TimeEntryListItemHolder : Java.Lang.Object
        {
            #pragma warning disable 0414
            private readonly object subscriptionModelChanged;
            #pragma warning restore 0414
            private TimeEntryModel model;

            public View ColorView { get; private set; }

            public TextView ProjectTextView { get; private set; }

            public TextView DateTextView { get; private set; }

            public TextView DescriptionTextView { get; private set; }

            public TextView TagsTextView { get; private set; }

            public TextView BillableTextView { get; private set; }

            public TimeEntryListItemHolder (View root)
            {
                FindViews (root);

                // Cannot use model.OnPropertyChanged callback directly as it would most probably leak memory,
                // thus the global ModelChangedMessage is used instead.
                var bus = ServiceContainer.Resolve<MessageBus> ();
                subscriptionModelChanged = bus.Subscribe<ModelChangedMessage> (OnModelChanged);
            }

            private void FindViews (View root)
            {
                ColorView = root.FindViewById<View> (Resource.Id.ColorView);
                ProjectTextView = root.FindViewById<TextView> (Resource.Id.ProjectTextView);
                DateTextView = root.FindViewById<TextView> (Resource.Id.DateTextView);
                DescriptionTextView = root.FindViewById<TextView> (Resource.Id.DescriptionTextView);
                TagsTextView = root.FindViewById<TextView> (Resource.Id.TagsTextView);
                BillableTextView = root.FindViewById<TextView> (Resource.Id.BillableTextView);
            }

            private void OnModelChanged (ModelChangedMessage msg)
            {
                if (model == null)
                    return;

                if (model == msg.Model) {
                    if (msg.PropertyName == TimeEntryModel.PropertyStartTime
                        || msg.PropertyName == TimeEntryModel.PropertyIsBillable
                        || msg.PropertyName == TimeEntryModel.PropertyIsRunning
                        || msg.PropertyName == TimeEntryModel.PropertyDescription
                        || msg.PropertyName == TimeEntryModel.PropertyProjectId
                        || msg.PropertyName == TimeEntryModel.PropertyTaskId)
                        Rebind ();
                } else if (model.ProjectId.HasValue && model.ProjectId == msg.Model.Id) {
                    if (msg.PropertyName == ProjectModel.PropertyName
                        || msg.PropertyName == ProjectModel.PropertyColor)
                        Rebind ();
                } else if (model.TaskId.HasValue && model.TaskId == msg.Model.Id) {
                    if (msg.PropertyName == TaskModel.PropertyName)
                        Rebind ();
                }
            }

            public void Bind (TimeEntryModel model)
            {
                this.model = model;
                Rebind ();
            }

            private void Rebind ()
            {
                var ctx = ProjectTextView.Context;

                if (model.Project == null) {
                    ProjectTextView.Text = ctx.GetString (Resource.String.RecentTimeEntryNoProject);
                } else if (model.Task != null) {
                    ProjectTextView.Text = String.Format ("{0} | {1}", model.Project.Name, model.Task.Name);
                } else {
                    ProjectTextView.Text = model.Project.Name;
                }

                var color = Color.Transparent;
                if (model.Project != null) {
                    color = Color.ParseColor (model.Project.GetHexColor());
                }
                ColorView.SetBackgroundColor (color);

                if (String.IsNullOrWhiteSpace (model.Description)) {
                    DescriptionTextView.Text = ctx.GetString (Resource.String.RecentTimeEntryNoDescription);
                } else {
                    DescriptionTextView.Text = model.Description;
                }

                // TODO: Use user defined date format
                DateTextView.Text = model.StartTime.ToShortDateString ();

                TagsTextView.Visibility = model.Tags.HasNonDefault ? ViewStates.Visible : ViewStates.Gone;
                BillableTextView.Visibility = model.IsBillable ? ViewStates.Visible : ViewStates.Gone;
            }
        }
    }
}
