using System;
using System.Threading.Tasks;
using Toggl.Phoebe.Data.DataObjects;
using System.Collections.Generic;

namespace Toggl.Phoebe.Data.Json.Converters
{
    public static class TimeEntryJsonConverter
    {
        public static async Task<TimeEntryJson> ToJsonAsync (this TimeEntryData data)
        {
            var userIdTask = GetRemoteId<UserData> (data.UserId);
            var workspaceIdTask = GetRemoteId<WorkspaceData> (data.WorkspaceId);
            var projectIdTask = GetRemoteId<ProjectData> (data.ProjectId);
            var taskIdTask = GetRemoteId<TaskData> (data.TaskId);
            var tagsTask = GetTags (data.Id);

            return new TimeEntryJson () {
                Id = data.RemoteId,
                ModifiedAt = data.ModifiedAt,
                Description = data.Description,
                IsBillable = data.IsBillable,
                StartTime = data.StartTime,
                StopTime = data.StopTime,
                DurationOnly = data.DurationOnly,
                Duration = EncodeDuration (data),
                Tags = await tagsTask,
                UserId = await userIdTask,
                WorkspaceId = await workspaceIdTask,
                ProjectId = await projectIdTask,
                TaskId = await taskIdTask,
            };
        }

        private static Task<List<string>> GetTags (Guid id)
        {
            throw new NotImplementedException ();
        }

        private static long EncodeDuration (TimeEntryData data)
        {
            var now = Time.UtcNow;

            // Calculate time entry duration
            TimeSpan duration;
            if (data.StartTime == DateTime.MinValue) {
                duration = TimeSpan.Zero;
            } else {
                duration = (data.StopTime ?? now) - data.StartTime;
                if (duration < TimeSpan.Zero) {
                    duration = TimeSpan.Zero;
                }
            }

            // Encode the duration
            var encoded = (long)duration.TotalSeconds;
            if (data.State == TimeEntryState.Running) {
                encoded = (long)(encoded - now.ToUnix ().TotalSeconds);
            }
            return encoded;
        }

        private static void DecodeDuration (TimeEntryData data, TimeEntryJson json)
        {
            // Decode duration:
            TimeSpan duration;
            if (json.Duration < 0) {
                data.State = TimeEntryState.Running;
                duration = Time.UtcNow.ToUnix () + TimeSpan.FromSeconds (json.Duration);
            } else {
                data.State = TimeEntryState.Finished;
                duration = TimeSpan.FromSeconds (json.Duration);
            }

            // Set start and stop times based on the duration:
            var now = Time.UtcNow;
            if (data.State == TimeEntryState.Finished) {
                data.StartTime = json.StartTime;
                data.StopTime = json.StartTime + duration;
            } else {
                data.StartTime = now - duration;
                data.StopTime = null;
            }

        }

        private static async Task<long> GetRemoteId<T> (Guid id)
            where T : CommonData
        {
            throw new NotImplementedException ();
        }

        private static async Task<long?> GetRemoteId<T> (Guid? id)
            where T : CommonData
        {
            throw new NotImplementedException ();
        }

        private static Task<T> GetByRemoteId<T> (long remoteId)
        {
            throw new NotImplementedException ();
        }

        private static Task Put (object data)
        {
            throw new NotImplementedException ();
        }

        private static Task Delete (object data)
        {
            throw new NotImplementedException ();
        }

        private static Task<Guid> ResolveRemoteId<T> (long remoteId)
        {
            throw new NotImplementedException ();
        }

        private static Task<Guid?> ResolveRemoteId<T> (long? remoteId)
        {
            throw new NotImplementedException ();
        }

        private static async Task Merge (TimeEntryData data, TimeEntryJson json)
        {
            var userIdTask = ResolveRemoteId<UserData> (json.UserId);
            var workspaceIdTask = ResolveRemoteId<WorkspaceData> (json.WorkspaceId);
            var projectIdTask = ResolveRemoteId<ProjectData> (json.ProjectId);
            var taskIdTask = ResolveRemoteId<TaskData> (json.TaskId);

            data.Description = json.Description;
            data.IsBillable = json.IsBillable;
            data.DurationOnly = json.DurationOnly;
            data.UserId = await userIdTask;
            data.WorkspaceId = await workspaceIdTask;
            data.ProjectId = await projectIdTask;
            data.TaskId = await taskIdTask;
            DecodeDuration (data, json);

            MergeCommon (data, json);
        }

        private static Task ResetTags (TimeEntryData data, TimeEntryJson json)
        {
            // Ensure that only the JSON specified tags have many-to-many relation in our dataset.
            throw new NotImplementedException ();
        }

        private static void MergeCommon (CommonData data, CommonJson json)
        {
            data.RemoteId = json.Id;
            data.RemoteRejected = false;
            data.DeletedAt = null;
            data.ModifiedAt = json.ModifiedAt;
            data.IsDirty = false;
        }

        public static async Task<TimeEntryData> ToDataAsync (this TimeEntryJson json)
        {
            var data = await GetByRemoteId<TimeEntryData> (json.Id.Value);

            if (data == null || data.ModifiedAt < json.ModifiedAt) {
                if (json.DeletedAt == null) {
                    data = data ?? new TimeEntryData ();
                    await Merge (data, json);
                    await Put (data);
                    // Also update tags from the JSON we are merging:
                    await ResetTags (data, json);
                } else if (data != null) {
                    await Delete (data);
                    data = null;
                }
            }

            return data;
        }
    }
}
