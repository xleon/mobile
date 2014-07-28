using System;
using System.Linq;
using Toggl.Phoebe.Data.DataObjects;
using Toggl.Phoebe.Data.Merge;

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

        private static void ImportJson (IDataStoreContext ctx, WorkspaceUserData data, WorkspaceUserJson json)
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

            ImportCommonJson (data, json);
        }

        public WorkspaceUserData Import (IDataStoreContext ctx, WorkspaceUserJson json, Guid? localIdHint = null, WorkspaceUserData mergeBase = null)
        {
            var data = GetByRemoteId<WorkspaceUserData> (ctx, json.Id.Value, localIdHint);

            var merger = mergeBase != null ? new WorkspaceUserMerger (mergeBase) : null;
            if (merger != null && data != null)
                merger.Add (new WorkspaceUserData (data));

            if (json.DeletedAt.HasValue) {
                if (data != null) {
                    ctx.Delete (data);
                    data = null;
                }
            } else {
                data = data ?? new WorkspaceUserData ();
                ImportJson (ctx, data, json);

                if (merger != null) {
                    merger.Add (data);
                    data = merger.Result;
                }

                data = ctx.Put (data);
            }

            return data;
        }
    }
}
