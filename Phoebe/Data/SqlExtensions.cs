using System;
using System.Threading.Tasks;
using Toggl.Phoebe.Data.DataObjects;

namespace Toggl.Phoebe.Data
{
    /// <summary>
    /// This extension class contains everything that deals with raw SQL.
    /// </summary>
    public static class SqlExtensions
    {
        public static Task<long?> GetRemoteId<T> (this IDataStore ds, Guid id)
            where T : CommonData
        {
            var tbl = ds.GetTableName (typeof(T));
            var q = String.Concat ("SELECT RemoteId FROM ", tbl, " WHERE Id=?");
            return ds.ExecuteScalarAsync<long?> (q, id);
        }

        public static Task<Guid?> GetLocalId<T> (this IDataStore ds, long remoteId)
            where T : CommonData
        {
            var tbl = ds.GetTableName (typeof(T));
            var q = String.Concat ("SELECT Id FROM ", tbl, " WHERE RemoteId=?");
            return ds.ExecuteScalarAsync<Guid?> (q, remoteId);
        }
    }
}
