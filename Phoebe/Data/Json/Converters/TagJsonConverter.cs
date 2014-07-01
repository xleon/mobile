using System;
using System.Linq;
using System.Threading.Tasks;
using Toggl.Phoebe.Data.DataObjects;

namespace Toggl.Phoebe.Data.Json.Converters
{
    public sealed class TagJsonConverter : BaseJsonConverter
    {
        public async Task<TagJson> Export (TagData data)
        {
            var workspaceIdTask = GetRemoteId<WorkspaceData> (data.WorkspaceId);

            return new TagJson () {
                Id = data.RemoteId,
                ModifiedAt = data.ModifiedAt.ToUtc (),
                Name = data.Name,
                WorkspaceId = await workspaceIdTask.ConfigureAwait (false),
            };
        }

        private static async Task<TagData> Merge (TagData data, TagJson json)
        {
            var workspaceId = await GetLocalId<WorkspaceData> (json.WorkspaceId).ConfigureAwait (false);

            if (data == null) {
                // Fallback to name lookup for unsynced tags:
                var rows = await DataStore.Table<TagData> ().Take (1)
                    .QueryAsync (r => r.WorkspaceId == workspaceId && r.Name == json.Name && r.RemoteId == null)
                    .ConfigureAwait (false);
                data = rows.FirstOrDefault ();
            }

            // As a last chance create new tag:
            data = data ?? new TagData ();

            data.Name = json.Name;
            data.WorkspaceId = workspaceId;

            MergeCommon (data, json);

            return data;
        }

        public async Task<TagData> Import (TagJson json)
        {
            var data = await GetByRemoteId<TagData> (json.Id.Value).ConfigureAwait (false);

            if (json.DeletedAt.HasValue) {
                if (data != null) {
                    await DataStore.DeleteAsync (data).ConfigureAwait (false);
                    data = null;
                }
            } else if (data == null || data.ModifiedAt < json.ModifiedAt) {
                data = await Merge (data, json).ConfigureAwait (false);
                data = await DataStore.PutAsync (data).ConfigureAwait (false);
            }

            return data;
        }
    }
}
