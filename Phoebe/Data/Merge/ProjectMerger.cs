using Toggl.Phoebe.Data.DataObjects;

namespace Toggl.Phoebe.Data.Merge
{
    public class ProjectMerger : CommonMerger<ProjectData>
    {
        public ProjectMerger (ProjectData baseData) : base (baseData)
        {
        }

        protected override ProjectData Merge ()
        {
            var data = base.Merge ();

            data.Name = GetValue (d => d.Name);
            data.Color = GetValue (d => d.Color);
            data.IsActive = GetValue (d => d.IsActive);
            data.IsBillable = GetValue (d => d.IsBillable);
            data.IsPrivate = GetValue (d => d.IsPrivate);
            data.IsTemplate = GetValue (d => d.IsTemplate);
            data.UseTasksEstimate = GetValue (d => d.UseTasksEstimate);

            // Merge relations such that we wouldn't cause invalid final state.
            // When the clientId or workspaceId (checkd in that order) have changed, we use the data object 
            // where a single field changed for each of those fields.
            var relationsMaster = GetData (d => d.ClientId);
            if (relationsMaster == Base)
                relationsMaster = GetData (d => d.WorkspaceId);
            data.WorkspaceId = relationsMaster.WorkspaceId;
            data.ClientId = relationsMaster.ClientId;

            return data;
        }
    }
}
