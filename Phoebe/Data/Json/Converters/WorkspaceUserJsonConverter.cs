using System;
using System.Threading.Tasks;
using Toggl.Phoebe.Data.DataObjects;

namespace Toggl.Phoebe.Data.Json.Converters
{
    public sealed class WorkspaceUserJsonConverter : BaseJsonConverter
    {
        public async Task<WorkspaceUserJson> Export (WorkspaceUserData data)
        {
            var userTask = DataStore.Table<UserData> ()
                .Take (1).QueryAsync (m => m.Id == data.UserId);
            var workspaceIdTask = GetRemoteId<WorkspaceData> (data.WorkspaceId);

            var userRows = await userTask.ConfigureAwait (false);
            if (userRows.Count == 0) {
                throw new InvalidOperationException (String.Format (
                    "Cannot export data with invalid local relation ({0}#{1}) to JSON.",
                    typeof(UserData).Name, data.UserId
                ));
            }
            var user = userRows [0];
            if (user.RemoteId == null) {
                throw new InvalidOperationException (String.Format (
                    "Cannot export data with local-only relation ({0}#{1}) to JSON.",
                    typeof(UserData).Name, data.UserId
                ));
            }

            return new WorkspaceUserJson () {
                Id = data.RemoteId,
                ModifiedAt = data.ModifiedAt.ToUtc (),
                IsAdmin = data.IsAdmin,
                IsActive = data.IsActive,
                Name = user.Name,
                Email = user.Email,
                WorkspaceId = await workspaceIdTask.ConfigureAwait (false),
                UserId = user.RemoteId.Value,
            };
        }

        private static async Task Merge (WorkspaceUserData data, WorkspaceUserJson json)
        {
            var workspaceIdTask = GetLocalId<WorkspaceData> (json.WorkspaceId);
            var userTask = GetByRemoteId<UserData> (json.UserId);

            var user = await userTask.ConfigureAwait (false);
            var workspaceId = await workspaceIdTask.ConfigureAwait (false);

            // Update linked user data:
            if (user == null) {
                user = new UserData () {
                    RemoteId = json.UserId,
                    Name = json.Name,
                    Email = json.Email,
                    DefaultWorkspaceId = workspaceId,
                };
            } else {
                user.Name = json.Name;
                user.Email = json.Email;
            }
            user = await DataStore.PutAsync (user).ConfigureAwait (false);

            data.IsAdmin = json.IsAdmin;
            data.IsActive = json.IsActive;
            data.WorkspaceId = workspaceId;
            data.UserId = user.Id;

            MergeCommon (data, json);
        }

        public async Task<WorkspaceUserData> Import (WorkspaceUserJson json)
        {
            var data = await GetByRemoteId<WorkspaceUserData> (json.Id.Value).ConfigureAwait (false);

            if (json.DeletedAt.HasValue) {
                if (data != null) {
                    await DataStore.DeleteAsync (data).ConfigureAwait (false);
                    data = null;
                }
            } else if (data == null || data.ModifiedAt < json.ModifiedAt) {
                data = data ?? new WorkspaceUserData ();
                await Merge (data, json).ConfigureAwait (false);
                data = await DataStore.PutAsync (data).ConfigureAwait (false);
            }

            return data;
        }
    }
}
