using System;
using System.Linq;
using Toggl.Phoebe.Data.DataObjects;

namespace Toggl.Phoebe.Data.Json.Converters
{
    public sealed class WorkspaceUserJsonConverter : BaseJsonConverter
    {
        public WorkspaceUserJson Export (IDataStoreContext ctx, WorkspaceUserData data)
        {
            var userRows = ctx.Connection.Table<UserData> ()
                .Take (1).Where (m => m.Id == data.UserId).ToList ();
            if (userRows.Count == 0) {
                throw new InvalidOperationException (String.Format (
                    "Cannot export data with invalid local relation ({0}#{1}) to JSON.",
                    typeof(UserData).Name, data.UserId
                ));
            }
            var user = userRows [0];
            if (user.RemoteId == null) {
                throw new RelationRemoteIdMissingException (typeof(UserData), data.UserId);
            }

            var workspaceId = GetRemoteId<WorkspaceData> (ctx, data.WorkspaceId);

            return new WorkspaceUserJson () {
                Id = data.RemoteId,
                ModifiedAt = data.ModifiedAt.ToUtc (),
                IsAdmin = data.IsAdmin,
                IsActive = data.IsActive,
                Name = user.Name,
                Email = user.Email,
                WorkspaceId = workspaceId,
                UserId = user.RemoteId.Value,
            };
        }

        private static void Merge (IDataStoreContext ctx, WorkspaceUserData data, WorkspaceUserJson json)
        {
            var workspaceId = GetLocalId<WorkspaceData> (ctx, json.WorkspaceId);
            var user = GetByRemoteId<UserData> (ctx, json.UserId, null);

            // Update linked user data:
            if (user == null) {
                user = new UserData () {
                    RemoteId = json.UserId,
                    Name = json.Name,
                    Email = json.Email,
                    DefaultWorkspaceId = workspaceId,
                    ModifiedAt = DateTime.MinValue,
                };
            } else {
                user.Name = json.Name;
                user.Email = json.Email;
            }
            user = ctx.Put (user);

            data.IsAdmin = json.IsAdmin;
            data.IsActive = json.IsActive;
            data.WorkspaceId = workspaceId;
            data.UserId = user.Id;

            MergeCommon (data, json);
        }

        public WorkspaceUserData Import (IDataStoreContext ctx, WorkspaceUserJson json, Guid? localIdHint = null, bool forceUpdate = false)
        {
            var data = GetByRemoteId<WorkspaceUserData> (ctx, json.Id.Value, localIdHint);

            if (json.DeletedAt.HasValue) {
                if (data != null) {
                    ctx.Delete (data);
                    data = null;
                }
            } else if (data == null || forceUpdate || data.ModifiedAt.ToUtc () < json.ModifiedAt.ToUtc ()) {
                data = data ?? new WorkspaceUserData ();
                Merge (ctx, data, json);
                data = ctx.Put (data);
            }

            return data;
        }
    }
}
