using System;
using Toggl.Phoebe.Data.DataObjects;
using Toggl.Phoebe.Data.Merge;

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

        private static TagData ImportJson (IDataStoreContext ctx, TagData data, TagJson json)
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

            ImportCommonJson (data, json);

            return data;
        }

        public TagData Import (IDataStoreContext ctx, TagJson json, Guid? localIdHint = null, TagData mergeBase = null)
        {
            var data = GetByRemoteId<TagData> (ctx, json.Id.Value, localIdHint);

            var merger = mergeBase != null ? new TagMerger (mergeBase) : null;
            if (merger != null && data != null)
                merger.Add (new TagData (data));

            if (json.DeletedAt.HasValue) {
                if (data != null) {
                    ctx.Delete (data);
                    data = null;
                }
            } else {
                data = ImportJson (ctx, data, json);

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
