using System;
using System.Linq;
using Toggl.Phoebe.Data.DataObjects;

namespace Toggl.Phoebe.Data.Json.Converters
{
    public abstract class BaseJsonConverter
    {
        protected static void ImportCommonJson (CommonData data, CommonJson json)
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
            if (remoteId == null) {
                throw new RelationRemoteIdMissingException (typeof(T), id);
            }
            return remoteId.Value;
        }

        protected static long? GetRemoteId<T> (IDataStoreContext ctx, Guid? id)
        where T : CommonData, new()
        {
            if (id == null) {
                return null;
            }
            var remoteId = ctx.GetRemoteId<T> (id.Value);
            if (remoteId == null) {
                // Check that the relation is actually non-existent, not just remoteId unset
                var hasRelation = ctx.Connection.Table<T> ().Count (r => r.Id == id) > 0;
                if (hasRelation) {
                    throw new RelationRemoteIdMissingException (typeof(T), id.Value);
                }
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
            if (id == Guid.Empty) {
                id = CreatePlaceholder<T> (ctx, remoteId);
            }
            return id;
        }

        protected static Guid? GetLocalId<T> (IDataStoreContext ctx, long? remoteId)
        where T : CommonData, new()
        {
            if (remoteId == null) {
                return null;
            }
            var id = ctx.GetLocalId<T> (remoteId.Value);
            if (id == Guid.Empty) {
                id = CreatePlaceholder<T> (ctx, remoteId.Value);
            }
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

        protected static bool ShouldOverwrite (CommonData data, CommonJson json)
        {
            if (data == null) {
                return true;
            }

            if (!data.IsDirty || data.RemoteRejected) {
                return true;
            }

            if (data.ModifiedAt.ToUtc () < json.ModifiedAt.ToUtc ()) {
                return true;
            }

            return false;
        }
    }
}
