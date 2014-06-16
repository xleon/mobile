using System;
using System.Collections.Generic;
using System.Linq;
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

        public static async Task<List<string>> GetTimeEntryTagNames (this IDataStore ds, Guid timeEntryId)
        {
            var tagTbl = ds.GetTableName (typeof(TagData));
            var timeEntryTagTbl = ds.GetTableName (typeof(TimeEntryTagData));
            var q = String.Concat (
                        "SELECT t.Name AS Value FROM ", tagTbl, " AS t ",
                        "LEFT JOIN ", timeEntryTagTbl, " AS tet ON tet.TagId=t.Id ",
                        "WHERE tet.DeletedAt IS NULL AND tet.TimeEntryId=?");
            var res = await ds.QueryAsync<ColumnRow<string>> (q, timeEntryId).ConfigureAwait (false);
            return res.Select (v => v.Value).ToList ();
        }

        public static Guid? GetTagIdFromName (this IDataStoreContext ctx, Guid workspaceId, string name)
        {
            var con = ctx.Connection;
            var tagTbl = con.GetMapping (typeof(TagData)).TableName;
            var q = String.Concat ("SELECT Id AS Value FROM ", tagTbl, " WHERE WorkspaceId=? AND Name=?");
            return con.ExecuteScalar<Guid?> (q, workspaceId, name);
        }

        private class ColumnRow<T>
        {
            public T Value { get; set; }
        }
    }
}
