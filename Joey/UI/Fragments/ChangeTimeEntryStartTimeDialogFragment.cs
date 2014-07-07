using System;
using Toggl.Phoebe.Data.Models;

namespace Toggl.Joey.UI.Fragments
{
    public class ChangeTimeEntryStartTimeDialogFragment : BaseDateTimeDialogFragment
    {
        public ChangeTimeEntryStartTimeDialogFragment (TimeEntryModel model) : base (model)
        {
        }

        public ChangeTimeEntryStartTimeDialogFragment ()
        {
        }

        public ChangeTimeEntryStartTimeDialogFragment (IntPtr jref, Android.Runtime.JniHandleOwnership xfer) : base (jref, xfer)
        {
        }

        protected override DateTime GetInitialDate ()
        {
            var date = Toggl.Phoebe.Time.Now;
            if (Model != null && Model.StartTime != DateTime.MinValue) {
                date = Model.StartTime.ToLocalTime ().Date;
            }
            return date;
        }

        protected override DateTime GetInitialTime ()
        {
            var time = Toggl.Phoebe.Time.Now;
            if (Model != null && Model.StartTime != DateTime.MinValue) {
                time = Model.StartTime.ToLocalTime ();
            }
            return time;
        }

        protected override async void UpdateDate (DateTime dateTime)
        {
            Model.StartTime = dateTime;
            await Model.SaveAsync ();
        }
    }
}
