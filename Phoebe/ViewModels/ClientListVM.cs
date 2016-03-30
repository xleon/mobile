using System;
using System.Collections.Generic;
using System.Linq;
using Toggl.Phoebe.Data.Models;
using Toggl.Phoebe.Helpers;
using Toggl.Phoebe.Reactive;
using Toggl.Phoebe.Analytics;
using XPlatUtils;

namespace Toggl.Phoebe.ViewModels
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
                clients.Add (ClientData.Create (x => x.Name = "No client"));
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
