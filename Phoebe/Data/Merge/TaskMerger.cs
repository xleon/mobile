using Toggl.Phoebe.Data.DataObjects;

namespace Toggl.Phoebe.Data.Merge
{
    public class TaskMerger : CommonMerger<TaskData>
    {
        public TaskMerger (TaskData baseData) : base (baseData)
        {
        }

        protected override TaskData Merge ()
        {
            var data = base.Merge ();

            data.Name = GetValue (d => d.Name);
            data.IsActive = GetValue (d => d.IsActive);
            data.Estimate = GetValue (d => d.Estimate);

            // Merge relations such that we wouldn't cause invalid final state.
            // When the projectId or workspaceId (checkd in that order) have changed, we use the data object
            // where a single field changed for each of those fields.
            var relationsMaster = GetData (d => d.ProjectId);
            if (relationsMaster == Base) {
                relationsMaster = GetData (d => d.WorkspaceId);
            }
            data.WorkspaceId = relationsMaster.WorkspaceId;
            data.ProjectId = relationsMaster.ProjectId;

            return data;
        }
    }
}
