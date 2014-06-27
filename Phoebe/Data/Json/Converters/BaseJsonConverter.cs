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
            return remoteId;
        }

        protected static async Task<long?> GetRemoteId<T> (Guid? id)
            where T : CommonData
        {
            if (id == null)
                return null;
            var remoteId = await DataStore.GetRemoteId<T> (id.Value).ConfigureAwait (false);
            if (remoteId == 0)
                return null;
            return remoteId;
        }

        protected static async Task<Guid> GetLocalId<T> (long remoteId)
            where T : CommonData, new()
        {
            if (remoteId == 0) {
                throw new ArgumentException ("Remote Id cannot be zero.", "remoteId");
            }
            var id = await DataStore.GetLocalId<T> (remoteId).ConfigureAwait (false);
            if (id == Guid.Empty)
                id = await CreatePlaceholder<T> (remoteId);
            return id;
        }

        protected static async Task<Guid?> GetLocalId<T> (long? remoteId)
            where T : CommonData, new()
        {
            if (remoteId == null)
                return null;
            var id = await DataStore.GetLocalId<T> (remoteId.Value).ConfigureAwait (false);
            if (id == Guid.Empty)
                id = await CreatePlaceholder<T> (remoteId.Value);
            return id;
        }

        private static async Task<Guid> CreatePlaceholder<T> (long remoteId)
            where T : CommonData, new()
        {
            var data = await DataStore.PutAsync (new T () {
                RemoteId = remoteId,
            });
            return data.Id;
        }

        protected static IDataStore DataStore {
            get { return ServiceContainer.Resolve<IDataStore> (); }
        }
    }
}
