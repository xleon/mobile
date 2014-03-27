using Toggl.Phoebe.Data;
using Toggl.Phoebe.Data.Models;
using Fragment = Android.Support.V4.App.Fragment;

namespace Toggl.Joey.UI.Fragments
{
    public class EditCurrentTimeEntryFragment : EditTimeEntryFragment
    {
        public override void OnStart ()
        {
            TimeEntry = TimeEntryModel.FindRunning () ?? TimeEntryModel.GetDraft ();

            base.OnStart ();
        }

        protected override void OnModelChanged (ModelChangedMessage msg)
        {
            base.OnModelChanged (msg);

            if (msg.Model != TimeEntry && msg.Model is TimeEntryModel) {
                // When some other time entry becomes IsRunning we need to switch over to that
                if (msg.PropertyName == TimeEntryModel.PropertyState
                    || msg.PropertyName == TimeEntryModel.PropertyIsShared) {
                    var entry = (TimeEntryModel)msg.Model;
                    if (entry.State == TimeEntryState.Running && ForCurrentUser (entry)) {
                        TimeEntry = entry;
                        Rebind ();
                    }
                }
            }
        }

        protected override void Rebind ()
        {
            if (TimeEntry == null || !CanRebind)
                return;

            if (TimeEntry.State == TimeEntryState.Finished || TimeEntry.DeletedAt.HasValue) {
                TimeEntry = TimeEntryModel.GetDraft ();
            }

            base.Rebind ();
        }
    }
}
