using System;
using System.Collections.Generic;
using System.Linq;
using Android.App;
using Android.Content;
using Android.OS;
using Toggl.Phoebe.Data;
using Toggl.Phoebe.Data.DataObjects;
using Toggl.Phoebe.Data.Models;
using XPlatUtils;

namespace Toggl.Joey.UI.Fragments
{
    public class DeleteTimeEntriesPromptDialogFragment : BaseDialogFragment
    {
        private static readonly string TimeEntryIdsArgument = "com.toggl.timer.time_entry_ids";

        public DeleteTimeEntriesPromptDialogFragment ()
        {
        }

        public DeleteTimeEntriesPromptDialogFragment (IntPtr javaRef, Android.Runtime.JniHandleOwnership transfer)
        : base (javaRef, transfer)
        {
        }

        public DeleteTimeEntriesPromptDialogFragment (IEnumerable<TimeEntryModel> models)
        {
            var ids = new List<string> ();
            foreach (var model in models) {
                ids.Add (model.Id.ToString ());
            }

            var args = new Bundle ();
            args.PutStringArrayList (TimeEntryIdsArgument, ids);

            Arguments = args;
        }

        public DeleteTimeEntriesPromptDialogFragment (IEnumerable<TimeEntryData> dataObjects)
        {
            var ids = new List<string> ();
            foreach (var model in dataObjects) {
                ids.Add (model.Id.ToString ());
            }

            var args = new Bundle ();
            args.PutStringArrayList (TimeEntryIdsArgument, ids);

            Arguments = args;
        }

        private IEnumerable<string> TimeEntryIds
        {
            get {
                var arr = Arguments != null ? Arguments.GetStringArrayList (TimeEntryIdsArgument) : null;
                return arr ?? Enumerable.Empty<string> ();
            }
        }

        private List<TimeEntryModel> models;

        public override void OnCreate (Bundle savedInstanceState)
        {
            base.OnCreate (savedInstanceState);

            // TODO: Really shouldn't use synchronous here, but ...
            var dataStore = ServiceContainer.Resolve<IDataStore> ();
            var ids = TimeEntryIds.Select (id => Guid.Parse (id)).ToList ();
            models = dataStore.Table<TimeEntryData> ()
                     .QueryAsync (r => r.DeletedAt == null && ids.Contains (r.Id))
                     .Result
                     .Select (data => new TimeEntryModel (data))
                     .ToList ();
        }

        public override Dialog OnCreateDialog (Bundle savedInstanceState)
        {
            var msg = Resources.GetQuantityString (
                          Resource.Plurals.DeleteTimeEntriesDialogMessage,
                          models.Count, models.Count
                      );

            return new AlertDialog.Builder (Activity)
                   .SetIcon (Resource.Drawable.IcDialogAlertHoloLight)
                   .SetTitle (Resource.String.DeleteTimeEntriesDialogTitle)
                   .SetMessage (msg)
                   .SetPositiveButton (Resource.String.DeleteTimeEntriesDialogDeleteButton, OnDeleteButtonClicked)
                   .SetNegativeButton (Resource.String.DeleteTimeEntriesDialogCancelButton, OnCancelButtonClicked)
                   .Create ();
        }

        public override void OnStart ()
        {
            base.OnStart ();

            if (models.Count == 0) {
                Dismiss ();
            }
        }

        private void OnDeleteButtonClicked (object sender, DialogClickEventArgs args)
        {
            models.Select (m => m.DeleteAsync ()).ToList ();
            models.Clear ();
            Dismiss ();
        }

        private void OnCancelButtonClicked (object sender, DialogClickEventArgs args)
        {
            Dismiss ();
        }
    }
}
