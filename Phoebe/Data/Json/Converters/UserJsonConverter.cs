using System;
using System.Threading.Tasks;
using Toggl.Phoebe.Data.DataObjects;

namespace Toggl.Phoebe.Data.Json.Converters
{
    public sealed class UserJsonConverter : BaseJsonConverter
    {
        public async Task<UserJson> Export (UserData data)
        {
            var defaultWorkspaceIdTask = GetRemoteId<WorkspaceData> (data.DefaultWorkspaceId);

            return new UserJson () {
                Id = data.RemoteId,
                ModifiedAt = data.ModifiedAt,
                Name = data.Name,
                Email = data.Email,
                StartOfWeek = data.StartOfWeek,
                DateFormat = data.DateFormat,
                TimeFormat = data.TimeFormat,
                ImageUrl = data.ImageUrl,
                Locale = data.Locale,
                Timezone = data.Timezone,
                SendProductEmails = data.SendProductEmails,
                SendTimerNotifications = data.SendTimerNotifications,
                SendWeeklyReport = data.SendWeeklyReport,
                StoreStartAndStopTime = data.TrackingMode == TrackingMode.StartNew,
                DefaultWorkspaceId = await defaultWorkspaceIdTask.ConfigureAwait (false),
            };
        }

        private static async Task Merge (UserData data, UserJson json)
        {
            var defaultWorkspaceIdTask = GetLocalId<WorkspaceData> (json.DefaultWorkspaceId);

            data.Name = json.Name;
            data.Email = json.Email;
            data.StartOfWeek = json.StartOfWeek;
            data.DateFormat = json.DateFormat;
            data.TimeFormat = json.TimeFormat;
            data.ImageUrl = json.ImageUrl;
            data.Locale = json.Locale;
            data.Timezone = json.Timezone;
            data.SendProductEmails = json.SendProductEmails;
            data.SendTimerNotifications = json.SendTimerNotifications;
            data.SendWeeklyReport = json.SendWeeklyReport;
            data.TrackingMode = json.StoreStartAndStopTime ? TrackingMode.StartNew : TrackingMode.Continue;
            data.DefaultWorkspaceId = await defaultWorkspaceIdTask.ConfigureAwait (false);

            MergeCommon (data, json);
        }

        public async Task<UserData> Import (UserJson json)
        {
            var data = await GetByRemoteId<UserData> (json.Id.Value).ConfigureAwait (false);

            if (data == null || data.ModifiedAt < json.ModifiedAt) {
                if (json.DeletedAt == null) {
                    data = data ?? new UserData ();
                    await Merge (data, json).ConfigureAwait (false);
                    data = await DataStore.PutAsync (data).ConfigureAwait (false);
                } else if (data != null) {
                    await DataStore.DeleteAsync (data).ConfigureAwait (false);
                    data = null;
                }
            }

            return data;
        }
    }
}
