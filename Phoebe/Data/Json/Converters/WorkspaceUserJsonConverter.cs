using System;
using System.Linq;
using Toggl.Phoebe.Data.DataObjects;
using Toggl.Phoebe.Data.Merge;
using Toggl.Phoebe.Logging;
using XPlatUtils;

namespace Toggl.Phoebe.Data.Json.Converters
{
    public sealed class WorkspaceUserJsonConverter : BaseJsonConverter
    {
        private const string Tag = "WorkspaceUserJsonConverter";

        public WorkspaceUserJson Export (IDataStoreContextSync ctx, WorkspaceUserData data)
        {
            var userRows = ctx.Connection.Table<UserData> ()
                           .Where (m => m.Id == data.UserId).Take (1).ToList ();
            if (userRows.Count == 0) {
                throw new InvalidOperationException (String.Format (
                        "Cannot export data with invalid local relation ({0}#{1}) to JSON.",
                        typeof (UserData).Name, data.UserId
                                                     ));
            }
            var user = userRows [0];
            if (user.RemoteId == null) {
                throw new RelationRemoteIdMissingException (typeof (UserData), data.UserId);
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

        private static void ImportJson (IDataStoreContextSync ctx, WorkspaceUserData data, WorkspaceUserJson json)
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

        public WorkspaceUserData Import (IDataStoreContextSync ctx, WorkspaceUserJson json, Guid? localIdHint = null, WorkspaceUserData mergeBase = null)
        {
            var log = ServiceContainer.Resolve<ILogger> ();

            var data = GetByRemoteId<WorkspaceUserData> (ctx, json.Id.Value, localIdHint);

            var merger = mergeBase != null ? new WorkspaceUserMerger (mergeBase) : null;
            if (merger != null && data != null) {
                merger.Add (new WorkspaceUserData (data));
            }

            if (json.DeletedAt.HasValue) {
                if (data != null) {
                    log.Info (Tag, "Deleting local data for {0}.", data.ToIdString ());
                    ctx.Delete (data);
                    data = null;
                }
            } else if (merger != null || ShouldOverwrite (data, json)) {
                data = data ?? new WorkspaceUserData ();
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
