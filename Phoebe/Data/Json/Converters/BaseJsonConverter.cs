using System;
using System.Linq;
using System.Threading.Tasks;
using Toggl.Phoebe.Data.DataObjects;
using XPlatUtils;

namespace Toggl.Phoebe.Data.Json.Converters
{
    public abstract class BaseJsonConverter
    {
        protected static void MergeCommon (CommonData data, CommonJson json)
        {
            data.RemoteId = json.Id;
            data.RemoteRejected = false;
            data.DeletedAt = null;
            data.ModifiedAt = json.ModifiedAt;
            data.IsDirty = false;
        }

        protected static async Task<T> GetByRemoteId<T> (long remoteId)
            where T : CommonData, new()
        {
            var res = await DataStore.Table<T> ()
                .QueryAsync (m => m.RemoteId == remoteId)
                .ConfigureAwait (false);
            return res.FirstOrDefault ();
        }

        protected static async Task<long> GetRemoteId<T> (Guid id)
            where T : CommonData
        {
            var remoteId = await DataStore.GetRemoteId<T> (id).ConfigureAwait (false);
            // TODO: Should we throw an exception here when remoteId not found?
            return remoteId ?? 0;
        }

        protected static async Task<long?> GetRemoteId<T> (Guid? id)
            where T : CommonData
        {
            if (id == null)
                return null;
            return await DataStore.GetRemoteId<T> (id.Value).ConfigureAwait (false);
        }

        protected static async Task<Guid> GetLocalId<T> (long remoteId)
            where T : CommonData
        {
            var id = await DataStore.GetLocalId<T> (remoteId).ConfigureAwait (false);
            // TODO: Should we throw an exception here when remoteId not found?
            return id ?? Guid.Empty;
        }

        protected static async Task<Guid?> GetLocalId<T> (long? remoteId)
            where T : CommonData
        {
            if (remoteId == null)
                return null;
            return await DataStore.GetLocalId<T> (remoteId.Value).ConfigureAwait (false);
        }

        protected static IDataStore DataStore {
            get { return ServiceContainer.Resolve<IDataStore> (); }
        }
    }
}
