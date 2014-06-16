using System;
using System.Threading.Tasks;
using Toggl.Phoebe.Data.DataObjects;

namespace Toggl.Phoebe.Data.Json.Converters
{
    public sealed class TagJsonConverter : BaseJsonConverter
    {
        public async Task<TagJson> ToJsonAsync (TagData data)
        {
            var workspaceIdTask = GetRemoteId<WorkspaceData> (data.WorkspaceId);

            return new TagJson () {
                Id = data.RemoteId,
                ModifiedAt = data.ModifiedAt,
                Name = data.Name,
                WorkspaceId = await workspaceIdTask.ConfigureAwait (false),
            };
        }

        private static async Task Merge (TagData data, TagJson json)
        {
            var workspaceIdTask = GetLocalId<WorkspaceData> (json.WorkspaceId);

            data.Name = json.Name;
            data.WorkspaceId = await workspaceIdTask.ConfigureAwait (false);

            MergeCommon (data, json);
        }

        public static async Task<TagData> Import (TagJson json)
        {
            var data = await GetByRemoteId<TagData> (json.Id.Value).ConfigureAwait (false);
            // TODO: Should fall back to name lookup?

            if (data == null || data.ModifiedAt < json.ModifiedAt) {
                if (json.DeletedAt == null) {
                    data = data ?? new TagData ();
                    await Merge (data, json).ConfigureAwait (false);
                    await DataStore.PutAsync (data).ConfigureAwait (false);
                } else if (data != null) {
                    await DataStore.DeleteAsync (data).ConfigureAwait (false);
                    data = null;
                }
            }

            return data;
        }
    }
}
