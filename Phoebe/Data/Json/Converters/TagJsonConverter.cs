using System;
using Toggl.Phoebe.Data.DataObjects;

namespace Toggl.Phoebe.Data.Json.Converters
{
    public sealed class TagJsonConverter : BaseJsonConverter
    {
        public TagJson Export (IDataStoreContext ctx, TagData data)
        {
            var workspaceId = GetRemoteId<WorkspaceData> (ctx, data.WorkspaceId);

            return new TagJson () {
                Id = data.RemoteId,
                ModifiedAt = data.ModifiedAt.ToUtc (),
                Name = data.Name,
                WorkspaceId = workspaceId,
            };
        }

        private static TagData Merge (IDataStoreContext ctx, TagData data, TagJson json)
        {
            var workspaceId = GetLocalId<WorkspaceData> (ctx, json.WorkspaceId);

            if (data == null) {
                // Fallback to name lookup for unsynced tags:
                var rows = ctx.Connection.Table<TagData> ().Take (1)
                    .Where (r => r.WorkspaceId == workspaceId && r.Name == json.Name && r.RemoteId == null);
                data = rows.FirstOrDefault ();
            }

            // As a last chance create new tag:
            data = data ?? new TagData ();

            data.Name = json.Name;
            data.WorkspaceId = workspaceId;

            MergeCommon (data, json);

            return data;
        }

        public TagData Import (IDataStoreContext ctx, TagJson json, Guid? localIdHint = null, bool forceUpdate = false)
        {
            var data = GetByRemoteId<TagData> (ctx, json.Id.Value, localIdHint);

            if (json.DeletedAt.HasValue) {
                if (data != null) {
                    ctx.Delete (data);
                    data = null;
                }
            } else if (data == null || forceUpdate || data.ModifiedAt.ToUtc () < json.ModifiedAt.ToUtc ()) {
                data = Merge (ctx, data, json);
                data = ctx.Put (data);
            }

            return data;
        }
    }
}
