using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Toggl.Phoebe.Analytics;
using Toggl.Phoebe.Data.DataObjects;
using Toggl.Phoebe.Data.Utils;
using Toggl.Phoebe.Data.ViewModels;
using XPlatUtils;

namespace Toggl.Phoebe.Data.ViewModels
{
    public class EditTimeEntryViewModel : IViewModel<TimeEntryGroup>
    {
        private bool isLoading;
        private TimeEntryGroup model;
        private IList<TimeEntryData> timeEntryList;

        public EditTimeEntryViewModel (IList<TimeEntryData> timeEntryList)
        {
            this.timeEntryList = timeEntryList;
            ServiceContainer.Resolve<ITracker> ().CurrentScreen = "Edit Grouped Time Entry";
        }

        public void Init ()
        {
            IsLoading = true;

            model = new TimeEntryGroup (timeEntryList);
            model.LoadAsync ();

            // Ensure that the model exists
            if (model.Workspace == null || model.Workspace.Id == Guid.Empty) {
                model = null;
            }

            IsLoading = false;
        }

        public void Dispose ()
        {
            model = null;
        }

        public Task SaveAsync ()
        {
            return model.SaveAsync ();
        }



        public TimeEntryGroup Model
        {
            get {
                return model;
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
    }
}

