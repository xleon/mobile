using System;
using System.ComponentModel;
using Android.OS;
using Toggl.Phoebe.Data;
using Toggl.Phoebe.Data.Models;
using XPlatUtils;
using Fragment = Android.Support.V4.App.Fragment;

namespace Toggl.Joey.UI.Fragments
{
    public class EditCurrentTimeEntryFragment : BaseEditTimeEntryFragment
    {
        private ActiveTimeEntryManager timeEntryManager;

        public EditCurrentTimeEntryFragment ()
        {
        }

        public EditCurrentTimeEntryFragment (IntPtr jref, Android.Runtime.JniHandleOwnership xfer) : base (jref, xfer)
        {
        }

        private void OnTimeEntryManagerPropertyChanged (object sender, PropertyChangedEventArgs args)
        {
            if (Handle == IntPtr.Zero)
                return;

            if (args.PropertyName == ActiveTimeEntryManager.PropertyActive) {
                ResetModel ();
            }
        }

        public override void OnCreate (Bundle savedInstanceState)
        {
            base.OnCreate (savedInstanceState);

            if (timeEntryManager == null) {
                timeEntryManager = ServiceContainer.Resolve<ActiveTimeEntryManager> ();
                timeEntryManager.PropertyChanged += OnTimeEntryManagerPropertyChanged;
            }

            ResetModel ();
        }

        public override void OnDestroy ()
        {
            if (timeEntryManager != null) {
                timeEntryManager.PropertyChanged -= OnTimeEntryManagerPropertyChanged;
                timeEntryManager = null;
            }

            base.OnDestroy ();
        }

        protected override void ResetModel ()
        {
            if (timeEntryManager.Active == null) {
                TimeEntry = null;
            } else {
                TimeEntry = new TimeEntryModel (timeEntryManager.Active);
            }
        }
    }
}
