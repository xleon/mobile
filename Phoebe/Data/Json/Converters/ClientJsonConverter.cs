using System;
using System.Threading.Tasks;
using Toggl.Phoebe.Data.DataObjects;

namespace Toggl.Phoebe.Data.Json.Converters
{
    public sealed class ClientJsonConverter : BaseJsonConverter
    {
        public async Task<ClientJson> Export (ClientData data)
        {
            var workspaceId = await GetRemoteId<WorkspaceData> (data.WorkspaceId).ConfigureAwait (false);

            return new ClientJson () {
                Id = data.RemoteId,
                ModifiedAt = data.ModifiedAt,
                Name = data.Name,
                WorkspaceId = workspaceId,
            };
        }

        private static async Task Merge (ClientData data, ClientJson json)
        {
            data.Name = json.Name;
            data.WorkspaceId = await GetLocalId<WorkspaceData> (json.WorkspaceId).ConfigureAwait (false);
            MergeCommon (data, json);
        }

        public async Task<ClientData> Import (ClientJson json)
        {
            var data = await GetByRemoteId<ClientData> (json.Id.Value).ConfigureAwait (false);

            if (data == null || data.ModifiedAt < json.ModifiedAt) {
                if (json.DeletedAt == null) {
                    data = data ?? new ClientData ();
                    await Merge (data, json).ConfigureAwait (false);
                    data = await DataStore.PutAsync (data).ConfigureAwait (false);
                } else if (data != null) {
                    await DataStore.DeleteAsync (data).ConfigureAwait (false);
                    data = null;
                }
            }

            return data;
        }
    }
}
