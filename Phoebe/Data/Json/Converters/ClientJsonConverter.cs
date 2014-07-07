using System;
using Toggl.Phoebe.Data.DataObjects;

namespace Toggl.Phoebe.Data.Json.Converters
{
    public sealed class ClientJsonConverter : BaseJsonConverter
    {
        public ClientJson Export (IDataStoreContext ctx, ClientData data)
        {
            var workspaceId = GetRemoteId<WorkspaceData> (ctx, data.WorkspaceId);

            return new ClientJson () {
                Id = data.RemoteId,
                ModifiedAt = data.ModifiedAt.ToUtc (),
                Name = data.Name,
                WorkspaceId = workspaceId,
            };
        }

        private static void Merge (IDataStoreContext ctx, ClientData data, ClientJson json)
        {
            data.Name = json.Name;
            data.WorkspaceId = GetLocalId<WorkspaceData> (ctx, json.WorkspaceId);
            MergeCommon (data, json);
        }

        public ClientData Import (IDataStoreContext ctx, ClientJson json, Guid? localIdHint = null, bool forceUpdate = false)
        {
            var data = GetByRemoteId<ClientData> (ctx, json.Id.Value, localIdHint);

            if (json.DeletedAt.HasValue) {
                if (data != null) {
                    ctx.Delete (data);
                    data = null;
                }
            } else if (data == null || forceUpdate || data.ModifiedAt < json.ModifiedAt) {
                data = data ?? new ClientData ();
                Merge (ctx, data, json);
                data = ctx.Put (data);
            }

            return data;
        }
    }
}
