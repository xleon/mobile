using System;
using System.Threading.Tasks;
using Toggl.Phoebe.Data.DataObjects;

namespace Toggl.Phoebe.Data.Json.Converters
{
    public static class ProjectJsonConverter
    {
        public static async Task<ProjectJson> ToJsonAsync (this ProjectData data)
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
                WorkspaceId = await workspaceIdTask,
                ClientId = await clientIdTask,
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

        private static async Task Merge (ProjectData data, ProjectJson json)
        {
            var workspaceIdTask = ResolveRemoteId<WorkspaceData> (json.WorkspaceId);
            var clientIdTask = ResolveRemoteId<ClientData> (json.ClientId);

            data.Name = json.Name;
            try {
                data.Color = Convert.ToInt32 (json.Color);
            } catch {
                // TODO: Use `ProjectModel.HexColors.Length - 1`
                data.Color = 0;
            }
            data.IsActive = json.IsActive;
            data.IsBillable = json.IsBillable;
            data.IsPrivate = json.IsPrivate;
            data.IsTemplate = json.IsTemplate;
            data.UseTasksEstimate = json.UseTasksEstimate;
            data.WorkspaceId = await workspaceIdTask;
            data.ClientId = await clientIdTask;

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

        public static async Task<ProjectData> ToDataAsync (this ProjectJson json)
        {
            var data = await GetByRemoteId<ProjectData> (json.Id.Value);

            if (data == null || data.ModifiedAt < json.ModifiedAt) {
                if (json.DeletedAt == null) {
                    data = data ?? new ProjectData ();
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

