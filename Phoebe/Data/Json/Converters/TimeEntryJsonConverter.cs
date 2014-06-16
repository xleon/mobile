using System;
using System.Threading.Tasks;
using Toggl.Phoebe.Data.DataObjects;
using System.Collections.Generic;

namespace Toggl.Phoebe.Data.Json.Converters
{
    public sealed class TimeEntryJsonConverter : BaseJsonConverter
    {
        public async Task<TimeEntryJson> Export (TimeEntryData data)
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
                Tags = await tagsTask.ConfigureAwait (false),
                UserId = await userIdTask.ConfigureAwait (false),
                WorkspaceId = await workspaceIdTask.ConfigureAwait (false),
                ProjectId = await projectIdTask.ConfigureAwait (false),
                TaskId = await taskIdTask.ConfigureAwait (false),
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

        private static async Task Merge (TimeEntryData data, TimeEntryJson json)
        {
            var userIdTask = GetLocalId<UserData> (json.UserId);
            var workspaceIdTask = GetLocalId<WorkspaceData> (json.WorkspaceId);
            var projectIdTask = GetLocalId<ProjectData> (json.ProjectId);
            var taskIdTask = GetLocalId<TaskData> (json.TaskId);

            data.Description = json.Description;
            data.IsBillable = json.IsBillable;
            data.DurationOnly = json.DurationOnly;
            data.UserId = await userIdTask.ConfigureAwait (false);
            data.WorkspaceId = await workspaceIdTask.ConfigureAwait (false);
            data.ProjectId = await projectIdTask.ConfigureAwait (false);
            data.TaskId = await taskIdTask.ConfigureAwait (false);
            DecodeDuration (data, json);

            MergeCommon (data, json);
        }

        private static Task ResetTags (TimeEntryData data, TimeEntryJson json)
        {
            // Ensure that only the JSON specified tags have many-to-many relation in our dataset.
            throw new NotImplementedException ();
        }

        public async Task<TimeEntryData> Import (TimeEntryJson json)
        {
            var data = await GetByRemoteId<TimeEntryData> (json.Id.Value).ConfigureAwait (false);

            if (data == null || data.ModifiedAt < json.ModifiedAt) {
                if (json.DeletedAt == null) {
                    data = data ?? new TimeEntryData ();
                    await Merge (data, json).ConfigureAwait (false);
                    await DataStore.PutAsync (data).ConfigureAwait (false);
                    // Also update tags from the JSON we are merging:
                    await ResetTags (data, json).ConfigureAwait (false);
                } else if (data != null) {
                    await DataStore.DeleteAsync (data).ConfigureAwait (false);
                    data = null;
                }
            }

            return data;
        }
    }
}
