using System;
using System.Threading.Tasks;
using Toggl.Phoebe.Data.DataObjects;

namespace Toggl.Phoebe.Data.Json.Converters
{
    public sealed class ProjectUserJsonConverter : BaseJsonConverter
    {
        public async Task<ProjectUserJson> Export (ProjectUserData data)
        {
            var projectIdTask = GetRemoteId<ProjectData> (data.ProjectId);
            var userIdTask = GetRemoteId<UserData> (data.UserId);

            return new ProjectUserJson () {
                Id = data.RemoteId,
                ModifiedAt = data.ModifiedAt.ToUtc (),
                HourlyRate = data.HourlyRate,
                IsManager = data.IsManager,
                ProjectId = await projectIdTask.ConfigureAwait (false),
                UserId = await userIdTask.ConfigureAwait (false),
            };
        }

        private static async Task Merge (ProjectUserData data, ProjectUserJson json)
        {
            var projectIdTask = GetLocalId<ProjectData> (json.ProjectId);
            var userIdTask = GetLocalId<UserData> (json.UserId);

            data.HourlyRate = json.HourlyRate;
            data.IsManager = json.IsManager;
            data.ProjectId = await projectIdTask.ConfigureAwait (false);
            data.UserId = await userIdTask.ConfigureAwait (false);

            MergeCommon (data, json);
        }

        public async Task<ProjectUserData> Import (ProjectUserJson json, Guid? localIdHint = null, bool forceUpdate = false)
        {
            var data = await GetByRemoteId<ProjectUserData> (json.Id.Value, localIdHint).ConfigureAwait (false);

            if (json.DeletedAt.HasValue) {
                if (data != null) {
                    await DataStore.DeleteAsync (data).ConfigureAwait (false);
                    data = null;
                }
            } else if (data == null || forceUpdate || data.ModifiedAt < json.ModifiedAt) {
                data = data ?? new ProjectUserData ();
                await Merge (data, json).ConfigureAwait (false);
                data = await DataStore.PutAsync (data).ConfigureAwait (false);
            }

            return data;
        }
    }
}
