using System;
using System.Collections.Generic;
using System.Linq;
using Toggl.Phoebe.Analytics;
using Toggl.Phoebe.Data.DataObjects;
using Toggl.Phoebe.Data.Utils;
using XPlatUtils;

namespace Toggl.Phoebe.Data.Views
{
    public class EditTimeEntryGroupView : IView<TimeEntryGroup>
    {
        private TimeEntryGroup model;
        private bool isLoading;
        private string[] timeEntryIds;

        public EditTimeEntryGroupView (string[] timeEntryIds)
        {
            ServiceContainer.Resolve<ITracker> ().CurrentScreen = "Edit Grouped Time Entry";
            this.timeEntryIds = timeEntryIds;
        }

        public void Dispose ()
        {
            if (model != null) {
                model.PropertyChanged -= OnPropertyChange;
                model = null;
            }
        }

        public event EventHandler OnIsLoadingChanged;

        public TimeEntryGroup Model
        {
            get {
                return model;
            }
        }

        public bool IsLoading
        {
            get {
                return isLoading;
            }
            private set {

                if (isLoading  == value) {
                    return;
                }

                isLoading = value;

                if (OnIsLoadingChanged != null) {
                    OnIsLoadingChanged (this, EventArgs.Empty);
                }
            }
        }

        public void Init ()
        {
            CreateModel (timeEntryIds);
        }

        private async void CreateModel (string[] ids)
        {
            IsLoading = true;

            var store = ServiceContainer.Resolve<IDataStore> ();

            foreach (string guidString in ids) {

                var guid = new Guid (guidString);
                var rows = await store.Table<TimeEntryData> ()
                           .QueryAsync (r => r.Id == guid && r.DeletedAt == null);
                var data = rows.FirstOrDefault ();

                if (model == null) {
                    model = new TimeEntryGroup (data);
                } else {
                    model.Add (data);
                }
            }

            // Ensure that the model exists
            if (model.Model.Workspace == null || model.Model.Workspace.Id == Guid.Empty) {
                model = null;
            } else {
                model.Model.PropertyChanged += OnPropertyChange;
            }

            IsLoading = false;
        }

        private void OnPropertyChange (object sender, EventArgs e)
        {
            if (model.Id == Guid.Empty) {
                Dispose ();
            }
        }

    }
}

