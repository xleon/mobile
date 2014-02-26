using System;
using System.Linq;
using System.Text;
using Android.Content;
using Android.Graphics;
using System.Collections.Generic;
using Android.OS;
using Android.Views;
using Android.Widget;
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

        private class HeaderPosition
        {
            public int Position { get; set; }

            public DateTime Date { get; set; }

            public HeaderPosition (int position, DateTime date)
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

        private HeaderPosition GetHeaderAt (int position)
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
            if (modelIndex >= ModelsView.Models.Count ())
                return null;
            
            return ModelsView.Models.ElementAt (modelIndex);
        }

        public override int GetItemViewType (int position)
        {
            if (GetIndexForPosition (position) == ModelsView.Count && ModelsView.IsLoading)
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
                var holder = (TimeEntryListItemHolder)view.Tag;
                holder.Bind (model);
            }

            return view;
        }

        private class TimeEntryListItemHolder : Java.Lang.Object
        {
            private readonly StringBuilder stringBuilder = new StringBuilder ();
            #pragma warning disable 0414
            private readonly object subscriptionModelChanged;
            #pragma warning restore 0414
            private TimeEntryModel model;

            public View ColorView { get; private set; }

            public TextView ProjectTextView { get; private set; }

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
                        || msg.PropertyName == TimeEntryModel.PropertyState
                        || msg.PropertyName == TimeEntryModel.PropertyDescription
                        || msg.PropertyName == TimeEntryModel.PropertyProjectId
                        || msg.PropertyName == TimeEntryModel.PropertyTaskId)
                        Rebind ();
                } else if (model.ProjectId.HasValue && model.ProjectId == msg.Model.Id) {
                    if (msg.PropertyName == ProjectModel.PropertyName
                        || msg.PropertyName == ProjectModel.PropertyColor)
                        Rebind ();
                } else if (model.ProjectId.HasValue && model.Project != null
                           && model.Project.ClientId.HasValue
                           && model.Project.ClientId == msg.Model.Id) {
                    if (msg.PropertyName == ClientModel.PropertyName)
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

            private string GetProjectText ()
            {
                var ctx = ServiceContainer.Resolve<Context> ();
                stringBuilder.Clear ();

                if (model.Project == null) {
                    return ctx.GetString (Resource.String.RecentTimeEntryNoProject);
                }

                if (model.Project.Client != null
                    && !String.IsNullOrWhiteSpace (model.Project.Client.Name)) {
                    stringBuilder.Append (model.Project.Client.Name);
                }

                if (!String.IsNullOrWhiteSpace (model.Project.Name)) {
                    if (stringBuilder.Length > 0) {
                        stringBuilder.Append (" - ");
                    }
                    stringBuilder.Append (model.Project.Name);
                }

                if (model.Task != null
                    && !String.IsNullOrWhiteSpace (model.Task.Name)) {
                    if (stringBuilder.Length > 0) {
                        stringBuilder.Append (" | ");
                    }
                    stringBuilder.Append (model.Task.Name);
                }

                return stringBuilder.ToString ();
            }

            private void Rebind ()
            {
                var ctx = ServiceContainer.Resolve<Context> ();

                ProjectTextView.Text = GetProjectText ();

                var color = Color.Transparent;
                if (model.Project != null) {
                    color = Color.ParseColor (model.Project.GetHexColor ());
                }
                ColorView.SetBackgroundColor (color);

                if (String.IsNullOrWhiteSpace (model.Description)) {
                    DescriptionTextView.Text = ctx.GetString (Resource.String.RecentTimeEntryNoDescription);
                } else {
                    DescriptionTextView.Text = model.Description;
                }

                TagsTextView.Visibility = model.Tags.HasNonDefault ? ViewStates.Visible : ViewStates.Gone;
                BillableTextView.Visibility = model.IsBillable ? ViewStates.Visible : ViewStates.Gone;
            }
        }
    }
}
