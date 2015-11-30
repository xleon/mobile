using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Toggl.Phoebe.Analytics;
using Toggl.Phoebe.Data.DataObjects;
using Toggl.Phoebe.Data.Models;
using Toggl.Phoebe.Data.Utils;
using Toggl.Phoebe.Data.ViewModels;
using XPlatUtils;

namespace Toggl.Phoebe.Data.ViewModels
{
    public class ClientListViewModel : IViewModel<ClientModel>
    {
        private Guid workspaceId;

        public ClientListViewModel (Guid workspaceId)
        {
            this.workspaceId = workspaceId;
            ServiceContainer.Resolve<ITracker> ().CurrentScreen = "Select Client";
        }

        public async Task Init ()
        {
            IsLoading = true;

            ClientDataCollection = new ObservableRangeCollection<ClientData> ();

            var store = ServiceContainer.Resolve<IDataStore> ();
            var clients = await store.Table<ClientData> ()
                          .Where (r => r.DeletedAt == null && r.WorkspaceId == workspaceId)
                          .ToListAsync();
            Sort (clients);

            ClientDataCollection.AddRange (clients);
            IsLoading = false;
        }

        public void Dispose ()
        {
        }

        public bool IsLoading { get; set; }

        public ObservableRangeCollection<ClientData> ClientDataCollection { get; set;}

        private void Sort (List<ClientData> clients)
        {
            clients.Sort ((a, b) => String.Compare (
                              a.Name ?? String.Empty,
                              b.Name ?? String.Empty,
                              StringComparison.OrdinalIgnoreCase
                          ));
        }
    }
}
