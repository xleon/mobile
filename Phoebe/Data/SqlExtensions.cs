using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Toggl.Phoebe.Data.DataObjects;
using Toggl.Phoebe.Data.Models;

namespace Toggl.Phoebe.Data
{
    /// <summary>
    /// This extension class contains everything that deals with raw SQL.
    /// </summary>
    public static class SqlExtensions
    {
        public static long? GetRemoteId<T> (this IDataStoreContext ctx, Guid id)
            where T : CommonData
        {
            var tbl = ctx.Connection.GetMapping<T> ().TableName;
            var q = String.Concat ("SELECT RemoteId FROM ", tbl, " WHERE Id=?");
            try {
                var remoteId = ctx.Connection.ExecuteScalar<long> (q, id);
                if (remoteId == 0) {
                    // Relation doesn't exist or RemoteId is set to zero
                    return null;
                }
                return remoteId;
            } catch (NullReferenceException) {
                // This happens when the RemoteId of the relation is set to null
                return null;
            }
        }

        public static Guid GetLocalId<T> (this IDataStoreContext ctx, long remoteId)
            where T : CommonData
        {
            var tbl = ctx.Connection.GetMapping<T> ().TableName;
            var q = String.Concat ("SELECT Id FROM ", tbl, " WHERE RemoteId=?");
            return ctx.Connection.ExecuteScalar<Guid> (q, remoteId);
        }

        public static List<string> GetTimeEntryTagNames (this IDataStoreContext ctx, Guid timeEntryId)
        {
            var tagTbl = ctx.Connection.GetMapping<TagData> ().TableName;
            var timeEntryTagTbl = ctx.Connection.GetMapping<TimeEntryTagData> ().TableName;
            var q = String.Concat (
                        "SELECT t.Name AS Value FROM ", tagTbl, " AS t ",
                        "LEFT JOIN ", timeEntryTagTbl, " AS tet ON tet.TagId=t.Id ",
                        "WHERE tet.DeletedAt IS NULL AND tet.TimeEntryId=?");
            var res = ctx.Connection.Query<ColumnRow<string>> (q, timeEntryId);
            return res.Select (v => v.Value).ToList ();
        }

        public static Guid GetTagIdFromName (this IDataStoreContext ctx, Guid workspaceId, string name)
        {
            var con = ctx.Connection;
            var tagTbl = con.GetMapping (typeof(TagData)).TableName;
            var q = String.Concat ("SELECT Id AS Value FROM ", tagTbl, " WHERE WorkspaceId=? AND Name=?");
            return con.ExecuteScalar<Guid> (q, workspaceId, name);
        }

        public static int GetProjectColorFromName (this IDataStoreContext ctx, Guid workspaceId, string name)
        {
            var con = ctx.Connection;
            var tagTbl = con.GetMapping (typeof(ProjectData)).TableName;
            var q = String.Concat ("SELECT Color AS Value FROM ", tagTbl, " WHERE WorkspaceId=? AND Name=?");
            return con.ExecuteScalar<int> (q, workspaceId, name);
        }

        public static Task<List<ProjectData>> GetUserAccessibleProjects (this IDataStore ds, Guid userId)
        {
            var projectTbl = ds.GetTableName (typeof(ProjectData));
            var projectUserTbl = ds.GetTableName (typeof(ProjectUserData));
            var q = String.Concat (
                        "SELECT p.* FROM ", projectTbl, " AS p ",
                        "LEFT JOIN ", projectUserTbl, " AS pu ON pu.ProjectId = p.Id AND pu.UserId=? ",
                        "WHERE p.DeletedAt IS NULL AND p.IsActive != 0 AND ",
                        "(p.IsPrivate == 0 OR pu.UserId IS NOT NULL)");
            return ds.QueryAsync<ProjectData> (q, userId);
        }

        public static Task<long> CountUserAccessibleProjects (this IDataStore ds, Guid userId)
        {
            var projectTbl = ds.GetTableName (typeof(ProjectData));
            var projectUserTbl = ds.GetTableName (typeof(ProjectUserData));
            var q = String.Concat (
                        "SELECT COUNT(*) FROM ", projectTbl, " AS p ",
                        "LEFT JOIN ", projectUserTbl, " AS pu ON pu.ProjectId = p.Id AND pu.UserId=? ",
                        "WHERE p.DeletedAt IS NULL AND p.IsActive != 0 AND ",
                        "(p.IsPrivate == 0 OR pu.UserId IS NOT NULL)");
            return ds.ExecuteScalarAsync<long> (q, userId);
        }

        public static Task<List<TagData>> GetTimeEntryTags (this IDataStore ds, Guid timeEntryId)
        {
            var tagTbl = ds.GetTableName (typeof(TagData));
            var timeEntryTagTbl = ds.GetTableName (typeof(TimeEntryTagData));
            var q = String.Concat (
                        "SELECT t.* FROM ", tagTbl, " AS t ",
                        "INNER JOIN ", timeEntryTagTbl, " AS tet ON tet.TagId = t.Id ",
                        "WHERE t.DeletedAt IS NULL AND tet.DeletedAt IS NULL ",
                        "AND tet.TimeEntryId=?");
            return ds.QueryAsync<TagData> (q, timeEntryId);
        }

        public static Task ResetAllModificationTimes (this IDataStore ds)
        {
            return ds.ExecuteInTransactionAsync (ctx => {
                foreach (var type in SqliteDataStore.DiscoverDataObjectTypes()) {
                    var con = ctx.Connection;
                    var tbl = con.GetMapping (type).TableName;

                    if (type.IsSubclassOf (typeof(CommonData))) {
                        var q = String.Concat (
                                    "UPDATE ", tbl, " SET ModifiedAt = ? ",
                                    "WHERE RemoteId IS NOT NULL AND DeletedAt IS NULL ",
                                    "AND (IsDirty = 0 OR RemoteRejected = 1)");
                        ctx.Connection.Execute (q, DateTime.MinValue);
                    }
                }
            });
        }

        public static int PurgeDatedTimeCorrections (this IDataStoreContext ctx, DateTime time)
        {
            var tbl = ctx.Connection.GetMapping<TimeCorrectionData> ().TableName;
            var q = String.Concat ("DELETE FROM ", tbl, " WHERE MeasuredAt < ?");
            return ctx.Connection.Execute (q, time);
        }

        private class ColumnRow<T>
        {
            public T Value { get; set; }
        }
    }
}
