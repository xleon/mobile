using Toggl.Phoebe.Data.DataObjects;

namespace Toggl.Phoebe.Data.Merge
{
    public class TagMerger : CommonMerger<TagData>
    {
        public TagMerger (TagData baseData) : base (baseData)
        {
        }

        protected override TagData Merge ()
        {
            var data = base.Merge ();

            data.Name = GetValue (d => d.Name);
            data.WorkspaceId = GetValue (d => d.WorkspaceId);

            return data;
        }
    }
}
