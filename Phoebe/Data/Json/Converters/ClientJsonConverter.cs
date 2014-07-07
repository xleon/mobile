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
                ModifiedAt = data.ModifiedAt.ToUtc (),
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

        public async Task<ClientData> Import (ClientJson json, Guid? localIdHint = null, bool forceUpdate = false)
        {
            var data = await GetByRemoteId<ClientData> (json.Id.Value, localIdHint).ConfigureAwait (false);

            if (json.DeletedAt.HasValue) {
                if (data != null) {
                    await DataStore.DeleteAsync (data).ConfigureAwait (false);
                    data = null;
                }
            } else if (data == null || forceUpdate || data.ModifiedAt < json.ModifiedAt) {
                data = data ?? new ClientData ();
                await Merge (data, json).ConfigureAwait (false);
                data = await DataStore.PutAsync (data).ConfigureAwait (false);
            }

            return data;
        }
    }
}
