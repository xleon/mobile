using System;

namespace Toggl.Phoebe.Data.Views
{
    /// <summary>
    /// This view combines IModelStore data and data from ITogglClient for time views. It tries to load data from
    /// web, but always falls back to data from the local store.
    /// </summary>
    public class AllTimeEntriesView : ObservableObject, IModelsView<TimeEntryModel>
    {
        public AllTimeEntriesView ()
        {
        }

        public void Reload ()
        {
            throw new NotImplementedException ();
        }

        public void LoadMore ()
        {
            throw new NotImplementedException ();
        }

        public System.Collections.Generic.IEnumerable<TimeEntryModel> Models {
            get {
                throw new NotImplementedException ();
            }
        }

        public long Count {
            get {
                throw new NotImplementedException ();
            }
        }

        public long? TotalCount {
            get {
                throw new NotImplementedException ();
            }
        }

        public bool HasMore {
            get {
                throw new NotImplementedException ();
            }
        }

        public bool IsLoading {
            get {
                throw new NotImplementedException ();
            }
        }

        public bool HasError {
            get {
                throw new NotImplementedException ();
            }
        }
    }
}
