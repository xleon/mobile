using Toggl.Phoebe.Data.DataObjects;

namespace Toggl.Phoebe.Data.Merge
{
    public class ClientMerger : CommonMerger<ClientData>
    {
        public ClientMerger (ClientData baseData) : base (baseData)
        {
        }

        protected override ClientData Merge ()
        {
            var data = base.Merge ();

            data.Name = GetValue (d => d.Name);
            data.WorkspaceId = GetValue (d => d.WorkspaceId);

            return data;
        }
    }
}
