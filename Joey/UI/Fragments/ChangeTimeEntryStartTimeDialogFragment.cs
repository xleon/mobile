using System;
using Toggl.Phoebe.Analytics;
using Toggl.Phoebe.Data.Models;
using XPlatUtils;

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

        public override void OnStart ()
        {
            base.OnStart ();
            ServiceContainer.Resolve<ITracker> ().CurrentScreen = "Change Start Time";
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

        protected override int DialogTitleId ()
        {
            return Resource.String.ChangeTimeEntryStartTimeDialogTitle;
        }
    }
}
