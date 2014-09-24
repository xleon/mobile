using System;
using Toggl.Phoebe.Data.DataObjects;
using Toggl.Phoebe.Data.Merge;
using XPlatUtils;

namespace Toggl.Phoebe.Data.Json.Converters
{
    public sealed class ProjectUserJsonConverter : BaseJsonConverter
    {
        private const string Tag = "ProjectUserJsonConverter";

        public ProjectUserJson Export (IDataStoreContext ctx, ProjectUserData data)
        {
            var projectId = GetRemoteId<ProjectData> (ctx, data.ProjectId);
            var userId = GetRemoteId<UserData> (ctx, data.UserId);

            return new ProjectUserJson () {
                Id = data.RemoteId,
                ModifiedAt = data.ModifiedAt.ToUtc (),
                HourlyRate = data.HourlyRate,
                IsManager = data.IsManager,
                ProjectId = projectId,
                UserId = userId,
            };
        }

        private static void ImportJson (IDataStoreContext ctx, ProjectUserData data, ProjectUserJson json)
        {
            var projectId = GetLocalId<ProjectData> (ctx, json.ProjectId);
            var userId = GetLocalId<UserData> (ctx, json.UserId);

            data.HourlyRate = json.HourlyRate;
            data.IsManager = json.IsManager;
            data.ProjectId = projectId;
            data.UserId = userId;

            ImportCommonJson (data, json);
        }

        public ProjectUserData Import (IDataStoreContext ctx, ProjectUserJson json, Guid? localIdHint = null, ProjectUserData mergeBase = null)
        {
            var log = ServiceContainer.Resolve<Logger> ();

            var data = GetByRemoteId<ProjectUserData> (ctx, json.Id.Value, localIdHint);

            var merger = mergeBase != null ? new ProjectUserMerger (mergeBase) : null;
            if (merger != null && data != null) {
                merger.Add (new ProjectUserData (data));
            }

            if (json.DeletedAt.HasValue) {
                if (data != null) {
                    log.Info (Tag, "Deleting local data for {0}.", data.ToIdString ());
                    ctx.Delete (data);
                    data = null;
                }
            } else if (merger != null || ShouldOverwrite (data, json)) {
                data = data ?? new ProjectUserData ();
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
