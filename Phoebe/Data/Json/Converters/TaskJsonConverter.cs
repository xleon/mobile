using System;
using Toggl.Phoebe.Data.DataObjects;
using Toggl.Phoebe.Data.Merge;
using XPlatUtils;

namespace Toggl.Phoebe.Data.Json.Converters
{
    public sealed class TaskJsonConverter : BaseJsonConverter
    {
        private const string Tag = "TaskJsonConverter";

        public TaskJson Export (IDataStoreContext ctx, TaskData data)
        {
            var projectId = GetRemoteId<ProjectData> (ctx, data.ProjectId);
            var workspaceId = GetRemoteId<WorkspaceData> (ctx, data.WorkspaceId);

            return new TaskJson () {
                Id = data.RemoteId,
                ModifiedAt = data.ModifiedAt.ToUtc (),
                Name = data.Name,
                IsActive = data.IsActive,
                Estimate = data.Estimate,
                ProjectId = projectId,
                WorkspaceId = workspaceId,
            };
        }

        private static void ImportJson (IDataStoreContext ctx, TaskData data, TaskJson json)
        {
            var projectId = GetLocalId<ProjectData> (ctx, json.ProjectId);
            var workspaceId = GetLocalId<WorkspaceData> (ctx, json.WorkspaceId);

            data.Name = json.Name;
            data.IsActive = json.IsActive;
            data.Estimate = json.Estimate;
            data.ProjectId = projectId;
            data.WorkspaceId = workspaceId;

            ImportCommonJson (data, json);
        }

        public TaskData Import (IDataStoreContext ctx, TaskJson json, Guid? localIdHint = null, TaskData mergeBase = null)
        {
            var log = ServiceContainer.Resolve<Logger> ();

            var data = GetByRemoteId<TaskData> (ctx, json.Id.Value, localIdHint);

            var merger = mergeBase != null ? new TaskMerger (mergeBase) : null;
            if (merger != null && data != null) {
                merger.Add (new TaskData (data));
            }

            if (json.DeletedAt.HasValue) {
                if (data != null) {
                    log.Info (Tag, "Deleting local data for {0}.", data.ToIdString ());
                    ctx.Delete (data);
                    data = null;
                }
            } else if (merger != null || ShouldOverwrite (data, json)) {
                data = data ?? new TaskData ();
                ImportJson (ctx, data, json);

                if (merger != null) {
                    merger.Add (data);
                    data = merger.Result;
                }

                if (merger != null) {
                    log.Info (Tag, "Importing {0}, merging with local data.", data.ToIdString ());
                } else {
                    log.Info (Tag, "Importing {0}, replacing local data.", data.ToIdString ());
                }

                data = ctx.Put (data);
            } else {
                log.Info (Tag, "Skipping import of {0}.", json.ToIdString ());
            }

            return data;
        }
    }
}
