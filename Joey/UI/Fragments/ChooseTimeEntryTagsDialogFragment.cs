using System;
using System.Collections.Generic;
using System.Linq;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Widget;
using Toggl.Phoebe;
using Toggl.Phoebe.Data;
using Toggl.Phoebe.Data.Models;
using Toggl.Phoebe.Data.Views;
using Toggl.Phoebe.Net;
using XPlatUtils;
using Toggl.Joey.UI.Adapters;

namespace Toggl.Joey.UI.Fragments
{
    public class ChooseTimeEntryTagsDialogFragment : BaseDialogFragment
    {
        private static readonly string TimeEntryIdArgument = "com.toggl.timer.time_entry_id";
        #pragma warning disable 0414
        private Subscription<ModelChangedMessage> subscriptionModelChanged;
        #pragma warning restore 0414
        private WorkspaceTagsView modelsView;
        private ListView listView;

        public ChooseTimeEntryTagsDialogFragment (TimeEntryModel model) : base ()
        {
            var args = new Bundle ();
            args.PutString (TimeEntryIdArgument, model.Id.ToString ());

            Arguments = args;
        }

        public ChooseTimeEntryTagsDialogFragment ()
        {
        }

        public ChooseTimeEntryTagsDialogFragment (IntPtr jref, Android.Runtime.JniHandleOwnership xfer) : base (jref, xfer)
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

        private TimeEntryModel model;

        public override void OnCreate (Bundle state)
        {
            base.OnCreate (state);

            model = Model.ById<TimeEntryModel> (TimeEntryId);
            if (model == null) {
                Dismiss ();
            }
        }

        private Guid WorkspaceId {
            get {
                if (model.WorkspaceId != null)
                    return model.WorkspaceId.Value;
                var user = ServiceContainer.Resolve<AuthManager> ().User;
                if (user != null && user.DefaultWorkspaceId != null)
                    return user.DefaultWorkspaceId.Value;
                return Guid.Empty;
            }
        }

        public override Dialog OnCreateDialog (Bundle state)
        {
            modelsView = new WorkspaceTagsView (WorkspaceId);

            var dia = new AlertDialog.Builder (Activity)
                .SetTitle (Resource.String.ChooseTimeEntryTagsDialogTitle)
                .SetAdapter (new TagsAdapter (modelsView), (IDialogInterfaceOnClickListener)null)
                .SetNegativeButton (Resource.String.ChooseTimeEntryTagsDialogCancel, OnCancelButtonClicked)
                .SetPositiveButton (Resource.String.ChooseTimeEntryTagsDialogOk, OnOkButtonClicked)
                .Create ();

            listView = dia.ListView;
            listView.ItemsCanFocus = false;
            listView.ChoiceMode = ChoiceMode.Multiple;
            // Reset the item click listener such that the dialog wouldn't be closed on selecting a tag
            listView.OnItemClickListener = null;

            return dia;
        }

        public override void OnStart ()
        {
            // TODO: Remove workaround after support library upgrade!
            // base.OnStart ();
            Android.Runtime.JNIEnv.CallNonvirtualVoidMethod (Handle, ThresholdClass,
                Android.Runtime.JNIEnv.GetMethodID (ThresholdClass, "onStart", "()V"));
            // End of workaround

            modelsView.WorkspaceId = WorkspaceId;

            // Setting tags like this makes the selection break when modelsView changes...
            var tags = model.Tags.Select ((inter) => inter.To).ToList ();
            var i = 0;
            listView.ClearChoices ();
            foreach (var tag in modelsView.Data) {
                if (tags.Contains (tag)) {
                    listView.SetItemChecked (i, true);
                }
                i++;
            }

            var bus = ServiceContainer.Resolve<MessageBus> ();
            subscriptionModelChanged = bus.Subscribe<ModelChangedMessage> (OnModelChanged);
        }

        public override void OnStop ()
        {
            // TODO: Remove workaround after support library upgrade!
            // base.OnStop ();
            Android.Runtime.JNIEnv.CallNonvirtualVoidMethod (Handle, ThresholdClass,
                Android.Runtime.JNIEnv.GetMethodID (ThresholdClass, "onStop", "()V"));
            // End of workaround

            if (subscriptionModelChanged != null) {
                var bus = ServiceContainer.Resolve<MessageBus> ();
                bus.Unsubscribe (subscriptionModelChanged);
                subscriptionModelChanged = null;
            }
        }

        private void OnCancelButtonClicked (object sender, DialogClickEventArgs args)
        {
            Dismiss ();
        }

        private void OnOkButtonClicked (object sender, DialogClickEventArgs args)
        {
            var selected = listView.CheckedItemPositions;
            var tags = modelsView.Data.Where ((tag, idx) => selected.Get (idx, false)).ToList ();

            // Store tags
            foreach (var inter in model.Tags.ToList()) {
                if (!tags.Remove (inter.To)) {
                    model.Tags.Remove (inter.To);
                }
            }
            foreach (var tag in tags) {
                model.Tags.Add (tag);
            }

            Dismiss ();
        }

        private void OnModelChanged (ModelChangedMessage msg)
        {
            // Protect against Java side being GCed
            if (Handle == IntPtr.Zero)
                return;

            if (msg.Model == model
                && msg.PropertyName == TimeEntryModel.PropertyWorkspaceId) {
                modelsView.WorkspaceId = WorkspaceId;
            } else if (msg.Model is UserModel
                       && msg.PropertyName == UserModel.PropertyDefaultWorkspaceId) {
                modelsView.WorkspaceId = WorkspaceId;
            }
        }
    }
}
