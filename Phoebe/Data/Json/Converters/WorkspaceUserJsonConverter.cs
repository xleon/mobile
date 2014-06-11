using System;
using System.Threading.Tasks;
using Toggl.Phoebe.Data.DataObjects;

namespace Toggl.Phoebe.Data.Json.Converters
{
    public static class WorkspaceUserJsonConverter
    {
        public static async Task<WorkspaceUserJson> ToJsonAsync (this WorkspaceUserData data)
        {
            var userTask = GetById<UserData> (data.UserId);
            var workspaceIdTask = GetRemoteId<WorkspaceData> (data.WorkspaceId);

            var user = await userTask;
            return new WorkspaceUserJson () {
                Id = data.RemoteId,
                ModifiedAt = data.ModifiedAt,
                IsAdmin = data.IsAdmin,
                IsActive = data.IsActive,
                Name = user.Name,
                Email = user.Email,
                WorkspaceId = await workspaceIdTask,
                UserId = user.RemoteId.Value,
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

        private static Task<T> GetById<T> (Guid id)
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

        private static async Task Merge (WorkspaceUserData data, WorkspaceUserJson json)
        {
            var workspaceIdTask = ResolveRemoteId<WorkspaceData> (json.WorkspaceId);
            var userTask = GetByRemoteId<UserData> (json.UserId);

            var user = await userTask;
            var workspaceId = await workspaceIdTask;

            // Update linked user data:
            if (user == null) {
                user = new UserData () {
                    RemoteId = json.UserId,
                    Name = user.Name,
                    Email = user.Email,
                    DefaultWorkspaceId = workspaceId,
                };
            } else {
                user.Name = json.Name;
                user.Email = json.Email;
            }
            await Put (user);

            data.IsAdmin = json.IsAdmin;
            data.IsActive = json.IsActive;
            data.WorkspaceId = workspaceId;
            data.UserId = user.Id;

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

        public static async Task<WorkspaceUserData> ToDataAsync (this WorkspaceUserJson json)
        {
            var data = await GetByRemoteId<WorkspaceUserData> (json.Id.Value);

            if (data == null || data.ModifiedAt < json.ModifiedAt) {
                if (json.DeletedAt == null) {
                    data = data ?? new WorkspaceUserData ();
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
