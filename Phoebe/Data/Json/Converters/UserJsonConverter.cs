using System;
using System.Threading.Tasks;
using Toggl.Phoebe.Data.DataObjects;

namespace Toggl.Phoebe.Data.Json.Converters
{
    public static class UserJsonConverter
    {
        public static async Task<UserJson> ToJsonAsync (this UserData data)
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
                DefaultWorkspaceId = await defaultWorkspaceIdTask,
            };
        }

        private static async Task<long> GetRemoteId<T> (Guid id)
            where T : CommonData
        {
            throw new NotImplementedException ();
        }

        private static async Task<long?> GetRemoteId<T> (Guid? id)
            where T : CommonData
        {
            throw new NotImplementedException ();
        }

        private static Task<T> GetByRemoteId<T> (long remoteId)
        {
            throw new NotImplementedException ();
        }

        private static Task Put (object data)
        {
            throw new NotImplementedException ();
        }

        private static Task Delete (object data)
        {
            throw new NotImplementedException ();
        }

        private static Task<Guid> ResolveRemoteId<T> (long remoteId)
        {
            throw new NotImplementedException ();
        }

        private static Task<Guid?> ResolveRemoteId<T> (long? remoteId)
        {
            throw new NotImplementedException ();
        }

        private static async Task Merge (UserData data, UserJson json)
        {
            var defaultWorkspaceIdTask = ResolveRemoteId<WorkspaceData> (json.DefaultWorkspaceId);

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
            data.DefaultWorkspaceId = await defaultWorkspaceIdTask;

            MergeCommon (data, json);
        }

        private static void MergeCommon (CommonData data, CommonJson json)
        {
            data.RemoteId = json.Id;
            data.RemoteRejected = false;
            data.DeletedAt = null;
            data.ModifiedAt = json.ModifiedAt;
            data.IsDirty = false;
        }

        public static async Task<UserData> ToDataAsync (this UserJson json)
        {
            var data = await GetByRemoteId<UserData> (json.Id.Value);

            if (data == null || data.ModifiedAt < json.ModifiedAt) {
                if (json.DeletedAt == null) {
                    data = data ?? new UserData ();
                    await Merge (data, json);
                    await Put (data);
                } else if (data != null) {
                    await Delete (data);
                    data = null;
                }
            }

            return data;
        }
    }
}
