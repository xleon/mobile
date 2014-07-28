using Toggl.Phoebe.Data.DataObjects;

namespace Toggl.Phoebe.Data.Merge
{
    public class ProjectUserMerger : CommonMerger<ProjectUserData>
    {
        public ProjectUserMerger (ProjectUserData baseData) : base (baseData)
        {
        }

        protected override ProjectUserData Merge ()
        {
            var data = base.Merge ();

            data.IsManager = GetValue (d => d.IsManager);
            data.HourlyRate = GetValue (d => d.HourlyRate);
            data.ProjectId = GetValue (d => d.ProjectId);
            data.UserId = GetValue (d => d.UserId);

            return data;
        }
    }
}
