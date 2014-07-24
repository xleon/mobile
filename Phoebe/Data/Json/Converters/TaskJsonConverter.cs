using System;
using Toggl.Phoebe.Data.DataObjects;

namespace Toggl.Phoebe.Data.Json.Converters
{
    public sealed class TaskJsonConverter : BaseJsonConverter
    {
        public TaskJson Export (IDataStoreContext ctx, TaskData data)
        {
            var projectId = GetRemoteId<ProjectData> (ctx, data.ProjectId);
            var workspaceId = GetRemoteId<WorkspaceData> (ctx, data.WorkspaceId);

            return new TaskJson () {
                Id = data.RemoteId,
                ModifiedAt = data.ModifiedAt.ToUtc (),
                Name = data.Name,
                IsActive = data.IsActive,
                Estimate = data.Estimate,
                ProjectId = projectId,
                WorkspaceId = workspaceId,
            };
        }

        private static void Merge (IDataStoreContext ctx, TaskData data, TaskJson json)
        {
            var projectId = GetLocalId<ProjectData> (ctx, json.ProjectId);
            var workspaceId = GetLocalId<WorkspaceData> (ctx, json.WorkspaceId);

            data.Name = json.Name;
            data.IsActive = json.IsActive;
            data.Estimate = json.Estimate;
            data.ProjectId = projectId;
            data.WorkspaceId = workspaceId;

            MergeCommon (data, json);
        }

        public TaskData Import (IDataStoreContext ctx, TaskJson json, Guid? localIdHint = null, bool forceUpdate = false)
        {
            var data = GetByRemoteId<TaskData> (ctx, json.Id.Value, localIdHint);

            if (json.DeletedAt.HasValue) {
                if (data != null) {
                    ctx.Delete (data);
                    data = null;
                }
            } else if (data == null || forceUpdate || data.ModifiedAt.ToUtc () < json.ModifiedAt.ToUtc ()) {
                data = data ?? new TaskData ();
                Merge (ctx, data, json);
                data = ctx.Put (data);
            }

            return data;
        }
    }
}
