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
            if (Handle == IntPtr.Zero) {
                return;
            }

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
            // Need to be careful when updating model data as the logic in BaseEditTimeEntries uses
            // Id changes to detect deletions. This would result in recursive loop with this function.
            var model = TimeEntry;
            var data = timeEntryManager.Active;

            if (model == null || data == null || model.Id != data.Id) {
                TimeEntry = (TimeEntryModel)data;
            } else {
                model.Data = data;
            }
        }
    }
}
