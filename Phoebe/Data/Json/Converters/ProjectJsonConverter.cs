using System;
using System.Threading.Tasks;
using Toggl.Phoebe.Data.DataObjects;
using Toggl.Phoebe.Data.Models;

namespace Toggl.Phoebe.Data.Json.Converters
{
    public sealed class ProjectJsonConverter : BaseJsonConverter
    {
        public async Task<ProjectJson> Export (ProjectData data)
        {
            var workspaceIdTask = GetRemoteId<WorkspaceData> (data.WorkspaceId);
            var clientIdTask = GetRemoteId<ClientData> (data.ClientId);

            return new ProjectJson () {
                Id = data.RemoteId,
                ModifiedAt = data.ModifiedAt,
                Name = data.Name,
                Color = data.Color.ToString (),
                IsActive = data.IsActive,
                IsBillable = data.IsBillable,
                IsPrivate = data.IsPrivate,
                IsTemplate = data.IsTemplate,
                UseTasksEstimate = data.UseTasksEstimate,
                WorkspaceId = await workspaceIdTask.ConfigureAwait (false),
                ClientId = await clientIdTask.ConfigureAwait (false),
            };
        }

        private static async Task Merge (ProjectData data, ProjectJson json)
        {
            var workspaceIdTask = GetLocalId<WorkspaceData> (json.WorkspaceId);
            var clientIdTask = GetLocalId<ClientData> (json.ClientId);

            data.Name = json.Name;
            try {
                data.Color = Convert.ToInt32 (json.Color);
            } catch {
                data.Color = ProjectModel.DefaultColor;
            }
            data.IsActive = json.IsActive;
            data.IsBillable = json.IsBillable;
            data.IsPrivate = json.IsPrivate;
            data.IsTemplate = json.IsTemplate;
            data.UseTasksEstimate = json.UseTasksEstimate;
            data.WorkspaceId = await workspaceIdTask.ConfigureAwait (false);
            data.ClientId = await clientIdTask.ConfigureAwait (false);

            MergeCommon (data, json);
        }

        public async Task<ProjectData> Import (ProjectJson json)
        {
            var data = await GetByRemoteId<ProjectData> (json.Id.Value).ConfigureAwait (false);

            if (json.DeletedAt.HasValue) {
                if (data != null) {
                    await DataStore.DeleteAsync (data).ConfigureAwait (false);
                    data = null;
                }
            } else if (data == null || data.ModifiedAt < json.ModifiedAt) {
                data = data ?? new ProjectData ();
                await Merge (data, json).ConfigureAwait (false);
                data = await DataStore.PutAsync (data).ConfigureAwait (false);
            }

            return data;
        }
    }
}
