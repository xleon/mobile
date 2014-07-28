using Toggl.Phoebe.Data.DataObjects;

namespace Toggl.Phoebe.Data.Merge
{
    public class TimeEntryMerger : CommonMerger<TimeEntryData>
    {
        public TimeEntryMerger (TimeEntryData baseData) : base (baseData)
        {
        }

        protected override TimeEntryData Merge ()
        {
            var data = base.Merge ();

            data.State = GetValue (d => d.State);
            data.Description = GetValue (d => d.Description);
            data.StartTime = GetValue (d => d.StartTime);
            data.StopTime = GetValue (d => d.StopTime);
            data.DurationOnly = GetValue (d => d.DurationOnly);
            data.IsBillable = GetValue (d => d.IsBillable);
            data.UserId = GetValue (d => d.UserId);

            // Merge relations such that we wouldn't cause invalid final state.
            // When the taskId, projectId or workspaceId (checkd in that order) have changed, we use the data object 
            // where a single field changed for each of those fields.
            var relationsMaster = GetData (d => d.TaskId);
            if (relationsMaster == Base)
                relationsMaster = GetData (d => d.ProjectId);
            if (relationsMaster == Base)
                relationsMaster = GetData (d => d.WorkspaceId);
            data.WorkspaceId = relationsMaster.WorkspaceId;
            data.ProjectId = relationsMaster.ProjectId;
            data.TaskId = relationsMaster.TaskId;

            return data;
        }
    }
}
