using System;
using System.Collections.Generic;
using System.Linq;
using Toggl.Phoebe.Data.DataObjects;
using XPlatUtils;

namespace Toggl.Phoebe.Data.Views
{
    public sealed class WorkspaceTagsView : IDataView<TagData>, IDisposable
    {
        private readonly List<TagData> dataObjects = new List<TagData> ();
        private Subscription<DataChangeMessage> subscriptionDataChange;
        private Guid workspaceId;

        public WorkspaceTagsView (Guid workspaceId)
        {
            this.workspaceId = workspaceId;
            Reload ();

            var bus = ServiceContainer.Resolve<MessageBus> ();
            subscriptionDataChange = bus.Subscribe<DataChangeMessage> (OnDataChange);
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

        private void OnDataChange (DataChangeMessage msg)
        {
            var tagData = msg.Data as TagData;
            if (tagData == null) {
                return;
            }

            var isExcluded = msg.Action == DataAction.Delete
                             || tagData.DeletedAt.HasValue
                             || tagData.WorkspaceId != workspaceId;
            var existingData = dataObjects.FirstOrDefault (r => r.Matches (tagData));

            if (isExcluded) {
                if (existingData != null) {
                    dataObjects.Remove (existingData);
                    OnUpdated ();
                }
            } else {
                tagData = new TagData (tagData);

                if (existingData == null) {
                    dataObjects.Add (tagData);
                } else {
                    dataObjects.UpdateData (tagData);
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

        private void Sort ()
        {
            dataObjects.Sort ((a, b) => String.Compare (
                                  a.Name ?? String.Empty,
                                  b.Name ?? String.Empty,
                                  StringComparison.Ordinal
                              ));
        }

        public event EventHandler Updated;

        public async void Reload ()
        {
            if (IsLoading || WorkspaceId == Guid.Empty) {
                return;
            }

            var store = ServiceContainer.Resolve<IDataStore> ();
            var bus = ServiceContainer.Resolve<MessageBus> ();
            bool shouldSubscribe = false;

            if (subscriptionDataChange != null) {
                shouldSubscribe = true;
                bus.Unsubscribe (subscriptionDataChange);
                subscriptionDataChange = null;
            }

            try {
                dataObjects.Clear ();
                IsLoading = true;
                OnUpdated ();

                var tags = await store.Table<TagData> ()
                           .QueryAsync (r => r.DeletedAt == null
                                        && r.WorkspaceId == workspaceId);
                dataObjects.AddRange (tags);
                Sort ();
            } finally {
                if (shouldSubscribe) {
                    subscriptionDataChange = bus.Subscribe<DataChangeMessage> (OnDataChange);
                }
                IsLoading = false;
                OnUpdated ();
            }
        }

        public void LoadMore ()
        {
        }

        public IEnumerable<TagData> Data
        {
            get { return dataObjects; }
        }

        public long Count
        {
            get { return dataObjects.Count; }
        }

        public bool HasMore
        {
            get { return false; }
        }

        public bool IsLoading { get; private set; }
    }
}

