using System;
using Toggl.Phoebe.Data.DataObjects;
using Toggl.Phoebe.Data.Merge;
using Toggl.Phoebe.Logging;
using XPlatUtils;

namespace Toggl.Phoebe.Data.Json.Converters
{
    public sealed class ClientJsonConverter : BaseJsonConverter
    {
        private const string Tag = "ClientJsonConverter";

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

        private static void ImportJson (IDataStoreContext ctx, ClientData data, ClientJson json)
        {
            data.Name = json.Name;
            data.WorkspaceId = GetLocalId<WorkspaceData> (ctx, json.WorkspaceId);
            ImportCommonJson (data, json);
        }

        public ClientData Import (IDataStoreContext ctx, ClientJson json, Guid? localIdHint = null, ClientData mergeBase = null)
        {
            var log = ServiceContainer.Resolve<ILogger> ();

            var data = GetByRemoteId<ClientData> (ctx, json.Id.Value, localIdHint);

            var merger = mergeBase != null ? new ClientMerger (mergeBase) : null;
            if (merger != null && data != null) {
                merger.Add (new ClientData (data));
            }

            if (json.DeletedAt.HasValue) {
                if (data != null) {
                    log.Info (Tag, "Deleting local data for {0}.", data.ToIdString ());
                    ctx.Delete (data);
                    data = null;
                }
            } else if (merger != null || ShouldOverwrite (data, json)) {
                data = data ?? new ClientData ();
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
