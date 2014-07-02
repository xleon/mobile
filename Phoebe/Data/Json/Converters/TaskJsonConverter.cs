using System;
using System.Threading.Tasks;
using Toggl.Phoebe.Data.DataObjects;

namespace Toggl.Phoebe.Data.Json.Converters
{
    public sealed class TaskJsonConverter : BaseJsonConverter
    {
        public async Task<TaskJson> Export (TaskData data)
        {
            var projectIdTask = GetRemoteId<ProjectData> (data.ProjectId);
            var workspaceIdTask = GetRemoteId<WorkspaceData> (data.WorkspaceId);

            return new TaskJson () {
                Id = data.RemoteId,
                ModifiedAt = data.ModifiedAt.ToUtc (),
                Name = data.Name,
                IsActive = data.IsActive,
                Estimate = data.Estimate,
                ProjectId = await projectIdTask.ConfigureAwait (false),
                WorkspaceId = await workspaceIdTask.ConfigureAwait (false),
            };
        }

        private static async Task Merge (TaskData data, TaskJson json)
        {
            var projectIdTask = GetLocalId<ProjectData> (json.ProjectId);
            var workspaceIdTask = GetLocalId<WorkspaceData> (json.WorkspaceId);

            data.Name = json.Name;
            data.IsActive = json.IsActive;
            data.Estimate = json.Estimate;
            data.ProjectId = await projectIdTask.ConfigureAwait (false);
            data.WorkspaceId = await workspaceIdTask.ConfigureAwait (false);

            MergeCommon (data, json);
        }

        public async Task<TaskData> Import (TaskJson json, Guid? localIdHint = null, bool forceUpdate = false)
        {
            var data = await GetByRemoteId<TaskData> (json.Id.Value, localIdHint).ConfigureAwait (false);

            if (json.DeletedAt.HasValue) {
                if (data != null) {
                    await DataStore.DeleteAsync (data).ConfigureAwait (false);
                    data = null;
                }
            } else if (data == null || forceUpdate || data.ModifiedAt < json.ModifiedAt) {
                data = data ?? new TaskData ();
                await Merge (data, json).ConfigureAwait (false);
                data = await DataStore.PutAsync (data).ConfigureAwait (false);
            }

            return data;
        }
    }
}
