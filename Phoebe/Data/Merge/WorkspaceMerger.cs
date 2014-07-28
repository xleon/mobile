using Toggl.Phoebe.Data.DataObjects;

namespace Toggl.Phoebe.Data.Merge
{
    public class WorkspaceMerger : CommonMerger<WorkspaceData>
    {
        public WorkspaceMerger (WorkspaceData baseData) : base (baseData)
        {
        }

        protected override WorkspaceData Merge ()
        {
            var data = base.Merge ();

            data.Name = GetValue (d => d.Name);
            data.IsPremium = GetValue (d => d.IsPremium);
            data.DefaultRate = GetValue (d => d.DefaultRate);
            data.DefaultCurrency = GetValue (d => d.DefaultCurrency);
            data.ProjectCreationPrivileges = GetValue (d => d.ProjectCreationPrivileges);
            data.BillableRatesVisibility = GetValue (d => d.BillableRatesVisibility);
            data.RoundingMode = GetValue (d => d.RoundingMode);
            data.RoundingPercision = GetValue (d => d.RoundingPercision);
            data.LogoUrl = GetValue (d => d.LogoUrl);

            return data;
        }
    }
}
