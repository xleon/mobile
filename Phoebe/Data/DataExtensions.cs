using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Toggl.Phoebe.Data.DataObjects;
using SQLite.Net.Async;

namespace Toggl.Phoebe.Data
{
    public static class DataExtensions
    {
        /// <summary>
        /// Checks if the two objects share the same type and primary key.
        /// </summary>
        /// <param name="data">Data object.</param>
        /// <param name="other">Other data object.</param>
        public static bool Matches (this CommonData data, object other)
        {
            if (data == other) {
                return true;
            }
            if (data == null || other == null) {
                return false;
            }
            if (data.GetType () != other.GetType ()) {
                return false;
            }
            return data.Id == ((CommonData)other).Id;
        }

        public static bool UpdateData<T> (this IList<T> list, T data)
        where T : CommonData
        {
            var updateCount = 0;

            for (var idx = 0; idx < list.Count; idx++) {
                if (data.Matches (list [idx])) {
                    list [idx] = data;
                    updateCount++;
                }
            }

            return updateCount > 0;
        }

        public static string ToIdString (this CommonData data)
        {
            var id = data.RemoteId.HasValue ? data.RemoteId.ToString () : data.Id.ToString ();
            return String.Concat (data.GetType ().Name, "#", id);
        }

        public static async Task<CommonData> PutDataAsync (this IDataStore ds, CommonData data)
        {
            var type = data.GetType ();

            if (type == typeof (ClientData)) {
                return await ds.PutAsync ((ClientData)data).ConfigureAwait (false);
            } else if (type == typeof (ProjectData)) {
                return await ds.PutAsync ((ProjectData)data).ConfigureAwait (false);
            } else if (type == typeof (ProjectUserData)) {
                return await ds.PutAsync ((ProjectUserData)data).ConfigureAwait (false);
            } else if (type == typeof (TagData)) {
                return await ds.PutAsync ((TagData)data).ConfigureAwait (false);
            } else if (type == typeof (TaskData)) {
                return await ds.PutAsync ((TaskData)data).ConfigureAwait (false);
            } else if (type == typeof (TimeEntryData)) {
                return await ds.PutAsync ((TimeEntryData)data).ConfigureAwait (false);
            } else if (type == typeof (UserData)) {
                return await ds.PutAsync ((UserData)data).ConfigureAwait (false);
            } else if (type == typeof (WorkspaceData)) {
                return await ds.PutAsync ((WorkspaceData)data).ConfigureAwait (false);
            } else if (type == typeof (WorkspaceUserData)) {
                return await ds.PutAsync ((WorkspaceUserData)data).ConfigureAwait (false);
            }
            throw new InvalidOperationException (String.Format ("Unknown type of {0}", type));
        }

        public static async Task<bool> ExistWithNameAsync ( this AsyncTableQuery<ClientData> query, string name)
        {
            var rows = await query.Where (r => r.Name == name).ToListAsync().ConfigureAwait (false);
            return rows.Count != 0;
        }

        public static async Task<bool> ExistWithNameAsync ( this AsyncTableQuery<ProjectData> query, string projectName, Guid clientId)
        {
            List<ProjectData> existingProjects;
            if ( clientId != Guid.Empty) {
                existingProjects = await query
                                   .Where (r => r.Name == projectName && r.ClientId == clientId)
                                   .ToListAsync().ConfigureAwait (false);
            } else {
                existingProjects = await query
                                   .Where (r => r.Name == projectName && r.ClientId == null)
                                   .ToListAsync().ConfigureAwait (false);
            }
            return existingProjects.Count != 0;
        }


        public static bool PublicInstancePropertiesEqual<T> (this T self, T to, params string[] ignore) where T : CommonData
        {
            if (self != null && to != null) {
                Type type = typeof (T);
                var ignoreList = new List<string> (ignore);
                foreach (System.Reflection.PropertyInfo pi in type.GetProperties (System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance)) {
                    if (!ignoreList.Contains (pi.Name)) {
                        object selfValue = type.GetProperty (pi.Name).GetValue (self, null);
                        object toValue = type.GetProperty (pi.Name).GetValue (to, null);

                        if (selfValue != toValue && (selfValue == null || !selfValue.Equals (toValue))) {
                            return false;
                        }
                    }
                }
                return true;
            }
            return self == to;
        }

        // TODO: Check also IsBillable, Tags?
        public static bool IsGroupableWith (this TimeEntryData data, TimeEntryData other)
        {
            var groupable = data.ProjectId == other.ProjectId &&
                            string.Compare (data.Description, other.Description, StringComparison.Ordinal) == 0 &&
                            data.TaskId == other.TaskId &&
                            data.UserId == other.UserId &&
                            data.WorkspaceId == other.WorkspaceId;

            if (groupable) {
                var date1 = data.StartTime.ToLocalTime ().Date;
                var date2 = other.StartTime.ToLocalTime ().Date;
                return date1 == date2;
            }
            return false;
        }
    }
}
