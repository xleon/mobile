using System;
using System.Collections.Generic;
using System.Linq;
using Toggl.Phoebe.Data.DataObjects;
using XPlatUtils;

namespace Toggl.Phoebe.Data.Views
{
    public class WorkspaceClientsView : IDataView<ClientData>, IDisposable
    {
        private readonly List<ClientData> dataObjects = new List<ClientData> ();
        private Subscription<DataChangeMessage> subscriptionDataChange;
        private Guid workspaceId;

        public WorkspaceClientsView (Guid workspaceId)
        {
            this.workspaceId = workspaceId;
            var bus = ServiceContainer.Resolve<MessageBus> ();
            subscriptionDataChange = bus.Subscribe<DataChangeMessage> (OnDataChange);
            Reload ();
        }

        public void Dispose ()
        {
            if (subscriptionDataChange != null) {
                var bus = ServiceContainer.Resolve<MessageBus> ();
                bus.Unsubscribe (subscriptionDataChange);
                subscriptionDataChange = null;
            }

            GC.SuppressFinalize (this);
        }

        public event EventHandler Updated;

        private void OnDataChange (DataChangeMessage msg)
        {
            var clientData = msg.Data as ClientData;
            if (clientData == null) {
                return;
            }

            var isExcluded = msg.Action == DataAction.Delete
                             || clientData.DeletedAt.HasValue
                             || clientData.WorkspaceId != workspaceId;
            var existingData = dataObjects.FirstOrDefault (r => r.Matches (clientData));

            if (isExcluded) {
                if (existingData != null) {
                    dataObjects.Remove (existingData);
                    OnUpdated ();
                }
            } else {
                clientData = new ClientData (clientData);

                if (existingData == null) {
                    dataObjects.Add (clientData);
                } else {
                    dataObjects.UpdateData (clientData);
                }

                Sort ();
                OnUpdated ();
            }
        }

        private void OnUpdated ()
        {
            var handler = Updated;
            if (handler != null) {
                handler (this, EventArgs.Empty);
            }
        }

        public Guid WorkspaceId
        {
            get { return workspaceId; }
            set {
                if (workspaceId == value) {
                    return;
                }
                workspaceId = value;
                Reload ();
            }
        }

        public async void Reload ()
        {
            if (IsLoading && workspaceId == Guid.Empty) {
                return;
            }

            var bus = ServiceContainer.Resolve<MessageBus> ();
            var shouldSubscribe = subscriptionDataChange != null;
            var store = ServiceContainer.Resolve<IDataStore> ();

            if (subscriptionDataChange != null) {
                bus.Unsubscribe (subscriptionDataChange);
                subscriptionDataChange = null;
                shouldSubscribe = true;
            }

            try {
                dataObjects.Clear ();
                IsLoading = true;


                var clients = await store.Table<ClientData> ()
                              .QueryAsync (r => r.DeletedAt == null && r.WorkspaceId == workspaceId);

                dataObjects.AddRange (clients);

                Sort ();
            } finally {
                if (shouldSubscribe) {
                    subscriptionDataChange = bus.Subscribe<DataChangeMessage> (OnDataChange);
                }
                IsLoading = false;
                OnUpdated ();
            }
        }

        private void Sort ()
        {
            dataObjects.Sort ((a, b) => String.Compare (
                                  a.Name ?? String.Empty,
                                  b.Name ?? String.Empty,
                                  StringComparison.OrdinalIgnoreCase
                              ));
        }

        public void LoadMore () {}

        public IEnumerable<ClientData> Data
        {
            get { return dataObjects; }
        }

        public long Count
        {
            get {
                return dataObjects.Count;
            }
        }

        public bool HasMore
        {
            get { return false; }
        }

        public bool IsLoading { get; private set; }
    }
}