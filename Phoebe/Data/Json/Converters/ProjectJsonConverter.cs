using System;
using Toggl.Phoebe.Data.DataObjects;
using Toggl.Phoebe.Data.Models;

namespace Toggl.Phoebe.Data.Json.Converters
{
    public sealed class ProjectJsonConverter : BaseJsonConverter
    {
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

        private static void Merge (IDataStoreContext ctx, ProjectData data, ProjectJson json)
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

            MergeCommon (data, json);
        }

        public ProjectData Import (IDataStoreContext ctx, ProjectJson json, Guid? localIdHint = null, bool forceUpdate = false)
        {
            var data = GetByRemoteId<ProjectData> (ctx, json.Id.Value, localIdHint);

            if (json.DeletedAt.HasValue) {
                if (data != null) {
                    ctx.Delete (data);
                    data = null;
                }
            } else if (data == null || forceUpdate || data.ModifiedAt.ToUtc () < json.ModifiedAt.ToUtc ()) {
                data = data ?? new ProjectData ();
                Merge (ctx, data, json);
                data = ctx.Put (data);
            }

            return data;
        }
    }
}
