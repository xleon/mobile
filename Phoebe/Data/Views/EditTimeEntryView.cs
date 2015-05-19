using System;
using System.ComponentModel;
using Toggl.Phoebe.Analytics;
using Toggl.Phoebe.Data.DataObjects;
using Toggl.Phoebe.Data.Models;
using Toggl.Phoebe.Data.ViewModels;
using XPlatUtils;

namespace Toggl.Phoebe.Data.Views
{
    public class EditTimeEntryView : IViewModel<TimeEntryModel>
    {
        private ActiveTimeEntryManager timeEntryManager;
        private TimeEntryModel model;
        private bool isLoading;
        private TimeEntryData timeEntryData;

        public EditTimeEntryView (TimeEntryData timeEntryData)
        {
            this.timeEntryData = timeEntryData;
            ServiceContainer.Resolve<ITracker> ().CurrentScreen = "Edit Time Entry";
        }

        public void Dispose ()
        {
            if (model != null) {
                model.PropertyChanged -= OnPropertyChange;
                model = null;
            }
        }

        private bool isDraft;

        public bool IsDraft
        {
            get {
                return isDraft;
            }
        }

        public event EventHandler OnModelChanged;

        public TimeEntryModel Model
        {
            get {
                return model;
            }

            private set {

                model = value;

                if (OnModelChanged != null) {
                    OnModelChanged (this, EventArgs.Empty);
                }
            }

        }

        public event EventHandler OnIsLoadingChanged;

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

        public async void Init (bool isDraft)
        {
            IsLoading  = true;

            this.isDraft = isDraft;

            if (!isDraft) {
                if (timeEntryData != null) {
                    Model = new TimeEntryModel (timeEntryData);
                } else {
                    ResetModel ();
                }
            } else {
                ResetModel ();
            }

            IsLoading = false;
        }

        public void ResetModel ()
        {
            isDraft = true;

            if (timeEntryManager == null) {
                timeEntryManager = ServiceContainer.Resolve<ActiveTimeEntryManager> ();
                timeEntryManager.PropertyChanged += OnTimeEntryManagerPropertyChanged;
            }

            if (timeEntryManager.Draft == null) {
                Model = null;
            } else {
                Model = new TimeEntryModel (timeEntryManager.Draft);
            }
        }

        private void OnTimeEntryManagerPropertyChanged (object sender, PropertyChangedEventArgs args)
        {
            if (args.PropertyName == ActiveTimeEntryManager.PropertyDraft) {
                ResetModel ();
            }
        }

        private void OnPropertyChange (object sender, EventArgs e)
        {
            if (Model.Id == Guid.Empty) {
                Dispose ();
            }

            if (timeEntryManager != null) {
                timeEntryManager.PropertyChanged -= OnTimeEntryManagerPropertyChanged;
                timeEntryManager = null;
            }

        }
    }
}

