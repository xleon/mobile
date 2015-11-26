using System;
using Toggl.Phoebe.Data.DataObjects;
using Toggl.Phoebe.Data.Merge;
using Toggl.Phoebe.Data.Models;
using Toggl.Phoebe.Logging;
using XPlatUtils;

namespace Toggl.Phoebe.Data.Json.Converters
{
    public sealed class ProjectJsonConverter : BaseJsonConverter
    {
        private const string Tag = "ProjectJsonConverter";

        public ProjectJson Export (IDataStoreContext ctx, ProjectData data)
        {
            var workspaceId = GetRemoteId<WorkspaceData> (ctx, data.WorkspaceId);
            var clientId = GetRemoteId<ClientData> (ctx, data.ClientId);

            return new ProjectJson () {
                Id = data.RemoteId,
                ModifiedAt = data.ModifiedAt.ToUtc (),
                Name = data.Name,
                Color = data.Color.ToString (),
                IsActive = data.IsActive,
                IsBillable = data.IsBillable,
                IsPrivate = data.IsPrivate,
                IsTemplate = data.IsTemplate,
                UseTasksEstimate = data.UseTasksEstimate,
                WorkspaceId = workspaceId,
                ClientId = clientId,
            };
        }

        private static void ImportJson (IDataStoreContext ctx, ProjectData data, ProjectJson json)
        {
            var workspaceId = GetLocalId<WorkspaceData> (ctx, json.WorkspaceId);
            var clientId = GetLocalId<ClientData> (ctx, json.ClientId);

            data.Name = json.Name;
            try {
                data.Color = Convert.ToInt32 (json.Color);
            } catch {
                data.Color = ProjectModel.DefaultColor;
            }
            data.IsActive = json.IsActive;
            data.IsBillable = json.IsBillable;
            data.IsPrivate = json.IsPrivate;
            data.IsTemplate = json.IsTemplate;
            data.UseTasksEstimate = json.UseTasksEstimate;
            data.WorkspaceId = workspaceId;
            data.ClientId = clientId;

            ImportCommonJson (data, json);
        }

        public ProjectData Import (IDataStoreContext ctx, ProjectJson json, Guid? localIdHint = null, ProjectData mergeBase = null)
        {
            var log = ServiceContainer.Resolve<ILogger> ();

            var data = GetByRemoteId<ProjectData> (ctx, json.Id.Value, localIdHint);

            var merger = mergeBase != null ? new ProjectMerger (mergeBase) : null;
            if (merger != null && data != null) {
                merger.Add (new ProjectData (data));
            }

            if (json.DeletedAt.HasValue) {
                if (data != null) {
                    log.Info (Tag, "Deleting local data for {0}.", data.ToIdString ());
                    ctx.Delete (data);
                    data = null;
                }
            } else if (merger != null || ShouldOverwrite (data, json)) {
                data = data ?? new ProjectData ();
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
