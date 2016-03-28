﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SQLite.Net.Async;
using Toggl.Phoebe._Data.Models;

namespace Toggl.Phoebe._Helpers
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
            return string.Concat (data.GetType ().Name, "#", id);
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
                                   .Where (r => r.Name == projectName)
                                   .ToListAsync().ConfigureAwait (false);
            }
            return existingProjects.Count != 0;
        }

        // TODO: Check also IsBillable, Tags?
        public static bool IsGroupableWith (this ITimeEntryData data, ITimeEntryData other)
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

        public static TimeSpan GetDuration (this ITimeEntryData data)
        {
            var now = Time.UtcNow;
            if (data.StartTime.IsMinValue ()) {
                return TimeSpan.Zero;
            }

            var duration = (data.StopTime ?? now) - data.StartTime;
            if (duration < TimeSpan.Zero) {
                duration = TimeSpan.Zero;
            }
            return duration;
        }

        /// <summary>
        /// Change duration of a time entry.
        /// </summary>
        public static void SetDuration (this TimeEntryData data, TimeSpan value)
        {
            var now = Time.UtcNow;

            if (data.State == TimeEntryState.Finished) {
                data.StopTime = data.StartTime + value;
            } else if (data.State == TimeEntryState.New) {
                if (value == TimeSpan.Zero) {
                    data.StartTime = DateTime.MinValue;
                    data.StopTime = null;
                } else if (data.StopTime.HasValue) {
                    data.StartTime = data.StopTime.Value - value;
                } else {
                    data.StartTime = now - value;
                    data.StopTime = now;
                }
            } else {
                data.StartTime = now - value;
            }

            data.StartTime = data.StartTime.Truncate (TimeSpan.TicksPerSecond);
            data.StopTime = data.StopTime.Truncate (TimeSpan.TicksPerSecond);
        }

        /// <summary>
        /// Change StartTime to a TimeEntryData
        /// </summary>
        public static TimeEntryData ChangeStartTime (this TimeEntryData data, DateTime newValue)
        {
            newValue = newValue.ToUtc ().Truncate (TimeSpan.TicksPerSecond);
            var duration = data.GetDuration ();
            data.StartTime = newValue;

            if (data.State != TimeEntryState.Running) {
                if (data.StopTime.HasValue) {
                    data.StopTime = data.StartTime + duration;
                } else {
                    var now = Time.UtcNow;

                    data.StopTime = data.StartTime.Date
                                    .AddHours (now.Hour)
                                    .AddMinutes (now.Minute)
                                    .AddSeconds (data.StartTime.Second);

                    if (data.StopTime < data.StartTime) {
                        data.StopTime = data.StartTime + duration;
                    }
                }

                data.StartTime = data.StartTime.Truncate (TimeSpan.TicksPerSecond);
                data.StopTime = data.StopTime.Truncate (TimeSpan.TicksPerSecond);
            }
            return data;
        }

        public static bool PublicInstancePropertiesEqual<T> (this T self, T to, params string[] ignore) where T : ICommonData
        {
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

        /// <summary>
        /// Change StopTime to a TimeEntryData
        /// </summary>
        public static TimeEntryData ChangeStoptime (this TimeEntryData data, DateTime? newValue)
        {
            newValue = newValue.ToUtc ().Truncate (TimeSpan.TicksPerSecond);
            data.StopTime = newValue;
            return data;
        }
    }
}
