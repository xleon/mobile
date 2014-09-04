using System;
using Toggl.Phoebe.Data.DataObjects;
using Toggl.Phoebe.Data.Merge;
using XPlatUtils;

namespace Toggl.Phoebe.Data.Json.Converters
{
    public sealed class UserJsonConverter : BaseJsonConverter
    {
        private const string Tag = "UserJsonConverter";

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
                DurationFormat = data.DurationFormat
            };
        }

        private static void ImportJson (IDataStoreContext ctx, UserData data, UserJson json)
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
            data.DurationFormat = json.DurationFormat;

            ImportCommonJson (data, json);
        }

        public UserData Import (IDataStoreContext ctx, UserJson json, Guid? localIdHint = null, UserData mergeBase = null)
        {
            var log = ServiceContainer.Resolve<Logger> ();

            var data = GetByRemoteId<UserData> (ctx, json.Id.Value, localIdHint);

            var merger = mergeBase != null ? new UserMerger (mergeBase) : null;
            if (merger != null && data != null)
                merger.Add (new UserData (data));

            if (json.DeletedAt.HasValue) {
                if (data != null) {
                    log.Info (Tag, "Deleting local data for {0}.", data.ToIdString ());
                    ctx.Delete (data);
                    data = null;
                }
            } else if (merger != null || ShouldOverwrite (data, json)) {
                data = data ?? new UserData ();
                ImportJson (ctx, data, json);

                if (merger != null) {
                    merger.Add (data);
                    data = merger.Result;
                }

                if (merger != null) {
                    log.Info (Tag, "Importing {0}, merging with local data.", data.ToIdString ());
                } else {
                    log.Info (Tag, "Importing {0}, replacing local data.", data.ToIdString ());
                }

                data = ctx.Put (data);
            } else {
                log.Info (Tag, "Skipping import of {0}.", json.ToIdString ());
            }

            return data;
        }
    }
}
