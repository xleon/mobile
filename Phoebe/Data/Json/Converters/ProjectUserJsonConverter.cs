using System;
using Toggl.Phoebe.Data.DataObjects;

namespace Toggl.Phoebe.Data.Json.Converters
{
    public sealed class ProjectUserJsonConverter : BaseJsonConverter
    {
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

        private static void Merge (IDataStoreContext ctx, ProjectUserData data, ProjectUserJson json)
        {
            var projectId = GetLocalId<ProjectData> (ctx, json.ProjectId);
            var userId = GetLocalId<UserData> (ctx, json.UserId);

            data.HourlyRate = json.HourlyRate;
            data.IsManager = json.IsManager;
            data.ProjectId = projectId;
            data.UserId = userId;

            MergeCommon (data, json);
        }

        public ProjectUserData Import (IDataStoreContext ctx, ProjectUserJson json, Guid? localIdHint = null, bool forceUpdate = false)
        {
            var data = GetByRemoteId<ProjectUserData> (ctx, json.Id.Value, localIdHint);

            if (json.DeletedAt.HasValue) {
                if (data != null) {
                    ctx.Delete (data);
                    data = null;
                }
            } else if (data == null || forceUpdate || data.ModifiedAt.ToUtc () < json.ModifiedAt.ToUtc ()) {
                data = data ?? new ProjectUserData ();
                Merge (ctx, data, json);
                data = ctx.Put (data);
            }

            return data;
        }
    }
}
