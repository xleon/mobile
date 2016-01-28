using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Toggl.Phoebe.Analytics;
using Toggl.Phoebe.Data.DataObjects;
using Toggl.Phoebe.Data.Models;
using XPlatUtils;

namespace Toggl.Phoebe.Data.ViewModels
{
    public class CreateClientViewModel : IDisposable
    {
        private ClientModel model;

        CreateClientViewModel (WorkspaceModel workspaceModel)
        {
            model = new ClientModel {
                Workspace = workspaceModel
            };
            ServiceContainer.Resolve<ITracker> ().CurrentScreen = "New Client Screen";
        }

        public static async Task<CreateClientViewModel> Init (Guid workspaceId)
        {
            var workspaceModel = new WorkspaceModel (workspaceId);
            await workspaceModel.LoadAsync ();
            return new CreateClientViewModel (workspaceModel);
        }

        public static CreateClientViewModel Init (WorkspaceModel workspaceModel)
        {
            return new CreateClientViewModel (workspaceModel);
        }

        public void Dispose ()
        {
        }

        public string ClientName { get; set; }

        public async Task<ClientData> SaveNewClient ()
        {
            var store = ServiceContainer.Resolve<IDataStore>();
            var existing = await store.Table<ClientData>()
                           .Where (r => r.WorkspaceId == model.Workspace.Id && r.Name == ClientName)
                           .ToListAsync().ConfigureAwait (false);

            if (existing.Count > 0) {
                model = new ClientModel (existing [0]);
            } else {
                model.Name = ClientName;
            }
            await model.SaveAsync ();
            return model.Data;
        }
    }
}

