using System;
using Toggl.Phoebe.Data.DataObjects;
using Toggl.Phoebe.Data.Merge;
using XPlatUtils;

namespace Toggl.Phoebe.Data.Json.Converters
{
    public sealed class TagJsonConverter : BaseJsonConverter
    {
        private const string Tag = "TagJsonConverter";

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
            var log = ServiceContainer.Resolve<Logger> ();

            var data = GetByRemoteId<TagData> (ctx, json.Id.Value, localIdHint);

            var merger = mergeBase != null ? new TagMerger (mergeBase) : null;
            if (merger != null && data != null)
                merger.Add (new TagData (data));

            if (json.DeletedAt.HasValue) {
                if (data != null) {
                    log.Info (Tag, "Deleting local data for {0}.", data.ToIdString ());
                    ctx.Delete (data);
                    data = null;
                }
            } else if (merger != null || ShouldOverwrite (data, json)) {
                data = ImportJson (ctx, data, json);

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
