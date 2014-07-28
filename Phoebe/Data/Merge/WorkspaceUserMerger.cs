using Toggl.Phoebe.Data.DataObjects;

namespace Toggl.Phoebe.Data.Merge
{
    public class WorkspaceUserMerger : CommonMerger<WorkspaceUserData>
    {
        public WorkspaceUserMerger (WorkspaceUserData baseData) : base (baseData)
        {
        }

        protected override WorkspaceUserData Merge ()
        {
            var data = base.Merge ();

            data.IsAdmin = GetValue (d => d.IsAdmin);
            data.IsActive = GetValue (d => d.IsActive);
            data.WorkspaceId = GetValue (d => d.WorkspaceId);
            data.UserId = GetValue (d => d.UserId);

            return data;
        }
    }
}
