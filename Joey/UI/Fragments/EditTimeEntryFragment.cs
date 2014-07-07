using System;
using System.ComponentModel;
using System.Linq;
using Android.OS;
using Toggl.Phoebe.Data;
using Toggl.Phoebe.Data.DataObjects;
using Toggl.Phoebe.Data.Models;
using XPlatUtils;
using Fragment = Android.Support.V4.App.Fragment;

namespace Toggl.Joey.UI.Fragments
{
    public class EditTimeEntryFragment : BaseEditTimeEntryFragment
    {
        private static readonly string TimeEntryIdArgument = "com.toggl.timer.time_entry_id";
        private static readonly string UseDraftKey = "com.toggl.timer.draft_used";
        private ActiveTimeEntryManager timeEntryManager;
        private bool useDraft;

        public EditTimeEntryFragment ()
        {
        }

        public EditTimeEntryFragment (TimeEntryModel model)
        {
            var args = new Bundle ();
            args.PutString (TimeEntryIdArgument, model.Id.ToString ());

            Arguments = args;
        }

        public EditTimeEntryFragment (IntPtr jref, Android.Runtime.JniHandleOwnership xfer) : base (jref, xfer)
        {
        }

        private Guid TimeEntryId {
            get {
                var id = Guid.Empty;
                if (Arguments != null) {
                    Guid.TryParse (Arguments.GetString (TimeEntryIdArgument), out id);
                }
                return id;
            }
        }

        public override void OnCreate (Bundle savedInstanceState)
        {
            base.OnCreate (savedInstanceState);

            if (savedInstanceState != null) {
                useDraft = savedInstanceState.GetBoolean (UseDraftKey, useDraft);
            }

            if (!useDraft && TimeEntryId != Guid.Empty) {
                LoadRequestedModel ();
            } else {
                ResetModel ();
            }
        }

        public override void OnDestroy ()
        {
            if (timeEntryManager != null) {
                timeEntryManager.PropertyChanged -= OnTimeEntryManagerPropertyChanged;
                timeEntryManager = null;
            }

            base.OnDestroy ();
        }

        public override void OnSaveInstanceState (Bundle outState)
        {
            outState.PutBoolean (UseDraftKey, useDraft);
            base.OnSaveInstanceState (outState);
        }

        private async void LoadRequestedModel ()
        {
            var store = ServiceContainer.Resolve<IDataStore> ();

            var rows = await store.Table<TimeEntryData> ()
                .QueryAsync (r => r.Id == TimeEntryId && r.DeletedAt == null);
            var data = rows.FirstOrDefault ();

            if (data != null) {
                TimeEntry = new TimeEntryModel (data);
            } else {
                ResetModel ();
            }
        }

        private void OnTimeEntryManagerPropertyChanged (object sender, PropertyChangedEventArgs args)
        {
            if (Handle == IntPtr.Zero)
                return;

            if (args.PropertyName == ActiveTimeEntryManager.PropertyDraft) {
                ResetModel ();
            }
        }

        protected override void ResetModel ()
        {
            useDraft = true;

            if (timeEntryManager == null) {
                timeEntryManager = ServiceContainer.Resolve<ActiveTimeEntryManager> ();
                timeEntryManager.PropertyChanged += OnTimeEntryManagerPropertyChanged;
            }

            if (timeEntryManager.Draft == null) {
                TimeEntry = null;
            } else {
                TimeEntry = new TimeEntryModel (timeEntryManager.Draft);
            }
        }
    }
}
