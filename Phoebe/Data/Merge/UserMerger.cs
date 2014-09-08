using Toggl.Phoebe.Data.DataObjects;

namespace Toggl.Phoebe.Data.Merge
{
    public class UserMerger : CommonMerger<UserData>
    {
        public UserMerger (UserData baseData) : base (baseData)
        {
        }

        protected override UserData Merge ()
        {
            var data = base.Merge ();

            data.Name = GetValue (d => d.Name);
            data.Email = GetValue (d => d.Email);
            data.StartOfWeek = GetValue (d => d.StartOfWeek);
            data.DateFormat = GetValue (d => d.DateFormat);
            data.TimeFormat = GetValue (d => d.TimeFormat);
            data.ImageUrl = GetValue (d => d.ImageUrl);
            data.Locale = GetValue (d => d.Locale);
            data.Timezone = GetValue (d => d.Timezone);
            data.SendProductEmails = GetValue (d => d.SendProductEmails);
            data.SendTimerNotifications = GetValue (d => d.SendTimerNotifications);
            data.SendWeeklyReport = GetValue (d => d.SendWeeklyReport);
            data.TrackingMode = GetValue (d => d.TrackingMode);
            data.DefaultWorkspaceId = GetValue (d => d.DefaultWorkspaceId);
            data.DurationFormat = GetValue (d => d.DurationFormat);

            return data;
        }
    }
}
