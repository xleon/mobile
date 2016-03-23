using System;
using System.Collections.Generic;
using System.Linq;
using Toggl.Phoebe._Data.Models;
using Toggl.Phoebe._Reactive;
using Toggl.Phoebe.Analytics;
using Toggl.Phoebe.Data.Utils;
using XPlatUtils;

namespace Toggl.Phoebe._ViewModels
{
    public interface IOnClientSelectedHandler
    {
        void OnClientSelected (IClientData data);
    }

    public class ClientListVM : IDisposable
    {
        public ClientListVM (AppState appState, Guid workspaceId)
        {
            ServiceContainer.Resolve<ITracker> ().CurrentScreen = "Select Client";

            ClientDataCollection = new ObservableRangeCollection<IClientData> ();
            var clients =  appState.Clients.Values.Where (r => r.DeletedAt == null && r.WorkspaceId == workspaceId).ToList ();
            if (!clients.Any ()) {
                clients.Add (new ClientData { Name = "No client" });
            }
            Sort (clients);
            ClientDataCollection.AddRange (clients);
        }

        public void Dispose ()
        {
        }

        public ObservableRangeCollection<IClientData> ClientDataCollection { get; set;}

        private void Sort (List<IClientData> clients)
        {
            clients.Sort ((a, b) => string.Compare (
                              a.Name ?? string.Empty,
                              b.Name ?? string.Empty,
                              StringComparison.OrdinalIgnoreCase
                          ));
        }
    }
}
