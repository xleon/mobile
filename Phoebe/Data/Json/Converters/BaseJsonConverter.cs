using System;
using System.Linq;
using Toggl.Phoebe.Data.DataObjects;

namespace Toggl.Phoebe.Data.Json.Converters
{
    public abstract class BaseJsonConverter
    {
        protected static void MergeCommon (CommonData data, CommonJson json)
        {
            data.RemoteId = json.Id;
            data.RemoteRejected = false;
            data.DeletedAt = null;
            data.ModifiedAt = json.ModifiedAt.ToUtc ();
            data.IsDirty = false;
        }

        protected static T GetByRemoteId<T> (IDataStoreContext ctx, long remoteId, Guid? localIdHint)
            where T : CommonData, new()
        {
            var query = ctx.Connection.Table<T> ();
            if (localIdHint != null) {
                var localId = localIdHint.Value;
                query = query.Where (r => r.Id == localId || r.RemoteId == remoteId);
            } else {
                query = query.Where (r => r.RemoteId == remoteId);
            }

            var res = query.ToList ();
            return res.FirstOrDefault (data => data.RemoteId == remoteId) ?? res.FirstOrDefault ();
        }

        protected static long GetRemoteId<T> (IDataStoreContext ctx, Guid id)
            where T : CommonData
        {
            var remoteId = ctx.GetRemoteId<T> (id);
            if (remoteId == 0) {
                throw new RelationRemoteIdMissingException (typeof(T), id);
            }
            return remoteId;
        }

        protected static long? GetRemoteId<T> (IDataStoreContext ctx, Guid? id)
            where T : CommonData
        {
            if (id == null)
                return null;
            var remoteId = ctx.GetRemoteId<T> (id.Value);
            if (remoteId == 0) {
                throw new RelationRemoteIdMissingException (typeof(T), id.Value);
            }
            return remoteId;
        }

        protected static Guid GetLocalId<T> (IDataStoreContext ctx, long remoteId)
            where T : CommonData, new()
        {
            if (remoteId == 0) {
                throw new ArgumentException ("Remote Id cannot be zero.", "remoteId");
            }
            var id = ctx.GetLocalId<T> (remoteId);
            if (id == Guid.Empty)
                id = CreatePlaceholder<T> (ctx, remoteId);
            return id;
        }

        protected static Guid? GetLocalId<T> (IDataStoreContext ctx, long? remoteId)
            where T : CommonData, new()
        {
            if (remoteId == null)
                return null;
            var id = ctx.GetLocalId<T> (remoteId.Value);
            if (id == Guid.Empty)
                id = CreatePlaceholder<T> (ctx, remoteId.Value);
            return id;
        }

        private static Guid CreatePlaceholder<T> (IDataStoreContext ctx, long remoteId)
            where T : CommonData, new()
        {
            var data = ctx.Put (new T () {
                RemoteId = remoteId,
                ModifiedAt = DateTime.MinValue,
            });
            return data.Id;
        }
    }
}
