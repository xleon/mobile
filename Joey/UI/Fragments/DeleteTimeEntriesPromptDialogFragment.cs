using System;
using System.Collections.Generic;
using System.Linq;
using Android.App;
using Android.Content;
using Android.OS;
using Toggl.Phoebe.Data;
using Toggl.Phoebe.Data.Models;
using DialogFragment = Android.Support.V4.App.DialogFragment;

namespace Toggl.Joey.UI.Fragments
{
    public class DeleteTimeEntriesPromptDialogFragment : DialogFragment
    {
        private static readonly string TimeEntryIdsArgument = "com.toggl.timer.time_entry_ids";

        public DeleteTimeEntriesPromptDialogFragment () : base ()
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
                if (!model.IsShared || model.DeletedAt != null)
                    continue;
                // Need to ensure the model IsPersisted, as we need to be able to access this model even
                // if the system suspends our process
                model.IsPersisted = true;
                ids.Add (model.Id.Value.ToString ());
            }

            var args = new Bundle ();
            args.PutStringArrayList (TimeEntryIdsArgument, ids);

            Arguments = args;
        }

        private IEnumerable<string> TimeEntryIds {
            get {
                var arr = Arguments != null ? Arguments.GetStringArrayList (TimeEntryIdsArgument) : null;
                return arr ?? Enumerable.Empty<string> ();
            }
        }

        private List<TimeEntryModel> models;

        public override void OnCreate (Bundle savedInstanceState)
        {
            base.OnCreate (savedInstanceState);

            models = TimeEntryIds
                .Select ((id) => Model.ById<TimeEntryModel> (Guid.Parse (id)))
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
            foreach (var model in models) {
                model.Delete ();
            }
            models.Clear ();
            Dismiss ();
        }

        private void OnCancelButtonClicked (object sender, DialogClickEventArgs args)
        {
            Dismiss ();
        }
    }
}
