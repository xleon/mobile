using System;
using System.Linq;
using System.Threading.Tasks;
using Toggl.Phoebe.Analytics;
using Toggl.Phoebe.Data.DataObjects;
using Toggl.Phoebe.Data.Models;
using XPlatUtils;

namespace Toggl.Phoebe.Data.ViewModels
{
    public class CreateTagViewModel : IDisposable
    {
        Guid workspaceId;

        public CreateTagViewModel (Guid workspaceId)
        {
            this.workspaceId = workspaceId;
            ServiceContainer.Resolve<ITracker> ().CurrentScreen = "New Tag Screen";
        }

        public void Dispose ()
        {
        }

        public async Task<TagData> SaveTagModel (string tagName)
        {
            var store = ServiceContainer.Resolve<IDataStore>();
            var existing = await store.Table<TagData>()
                           .Where (r => r.WorkspaceId == workspaceId && r.Name == tagName)
                           .ToListAsync().ConfigureAwait (false);

            TagData tag;
            if (existing.Count > 0) {
                tag = existing.FirstOrDefault ();
            } else {
                tag = await TagModel.AddTag (workspaceId, tagName);
            }
            return tag;
        }
    }
}
