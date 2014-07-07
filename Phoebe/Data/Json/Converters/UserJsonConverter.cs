using System;
using Toggl.Phoebe.Data.DataObjects;

namespace Toggl.Phoebe.Data.Json.Converters
{
    public sealed class UserJsonConverter : BaseJsonConverter
    {
        public UserJson Export (IDataStoreContext ctx, UserData data)
        {
            var defaultWorkspaceId = GetRemoteId<WorkspaceData> (ctx, data.DefaultWorkspaceId);

            return new UserJson () {
                Id = data.RemoteId,
                ModifiedAt = data.ModifiedAt.ToUtc (),
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
                DefaultWorkspaceId = defaultWorkspaceId,
            };
        }

        private static void Merge (IDataStoreContext ctx, UserData data, UserJson json)
        {
            var defaultWorkspaceId = GetLocalId<WorkspaceData> (ctx, json.DefaultWorkspaceId);

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
            data.DefaultWorkspaceId = defaultWorkspaceId;

            MergeCommon (data, json);
        }

        public UserData Import (IDataStoreContext ctx, UserJson json, Guid? localIdHint = null, bool forceUpdate = false)
        {
            var data = GetByRemoteId<UserData> (ctx, json.Id.Value, localIdHint);

            if (json.DeletedAt.HasValue) {
                if (data != null) {
                    ctx.Delete (data);
                    data = null;
                }
            } else if (data == null || forceUpdate || data.ModifiedAt < json.ModifiedAt) {
                data = data ?? new UserData ();
                Merge (ctx, data, json);
                data = ctx.Put (data);
            }

            return data;
        }
    }
}
