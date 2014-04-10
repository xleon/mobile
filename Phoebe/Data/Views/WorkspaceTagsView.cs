using System;
using System.Collections.Generic;
using Toggl.Phoebe.Data.Models;
using XPlatUtils;

namespace Toggl.Phoebe.Data.Views
{
    public sealed class WorkspaceTagsView : IDataView<TagModel>, IDisposable
    {
        private readonly List<TagModel> models = new List<TagModel> ();
        private Subscription<ModelChangedMessage> subscriptionModelChanged;
        private Guid workspaceId;

        public WorkspaceTagsView (Guid workspaceId)
        {
            this.workspaceId = workspaceId;
            Reload ();

            var bus = ServiceContainer.Resolve<MessageBus> ();
            subscriptionModelChanged = bus.Subscribe<ModelChangedMessage> (OnModelChanged);
        }

        public void Dispose ()
        {
            if (subscriptionModelChanged != null) {
                var bus = ServiceContainer.Resolve<MessageBus> ();
                bus.Unsubscribe (subscriptionModelChanged);
                subscriptionModelChanged = null;
            }

            GC.SuppressFinalize (this);
        }

        private void OnModelChanged (ModelChangedMessage msg)
        {
            var model = msg.Model as TagModel;
            if (model == null)
                return;

            if (msg.PropertyName == TagModel.PropertyName
                || model.WorkspaceId == workspaceId) {
                Sort ();
                OnUpdated ();
            } else if (msg.PropertyName == TagModel.PropertyWorkspaceId
                       || msg.PropertyName == TagModel.PropertyIsShared) {
                if (model.WorkspaceId == workspaceId) {
                    models.Add (model);
                    Sort ();
                    OnUpdated ();
                } else {
                    var idx = models.IndexOf (model);
                    if (idx >= 0) {
                        models.RemoveAt (idx);
                        OnUpdated ();
                    }
                }
            }
        }

        private void OnUpdated ()
        {
            var handler = Updated;
            if (handler != null) {
                handler (this, EventArgs.Empty);
            }
        }

        public Guid WorkspaceId {
            get { return workspaceId; }
            set {
                if (workspaceId == value)
                    return;
                workspaceId = value;
                Reload ();
            }
        }

        private void Sort ()
        {
            models.Sort ((a, b) => (a.Name ?? String.Empty).CompareTo ((b.Name ?? String.Empty)));
        }

        public event EventHandler Updated;

        public void Reload ()
        {
            var bus = ServiceContainer.Resolve<MessageBus> ();
            bool shouldSubscribe = false;

            if (subscriptionModelChanged != null) {
                shouldSubscribe = true;
                bus.Unsubscribe (subscriptionModelChanged);
                subscriptionModelChanged = null;
            }

            try {
                models.Clear ();
                models.AddRange (Model.Query<TagModel> ((m) => m.WorkspaceId == workspaceId).NotDeleted ());
                Sort ();
                OnUpdated ();
            } finally {
                if (shouldSubscribe) {
                    subscriptionModelChanged = bus.Subscribe<ModelChangedMessage> (OnModelChanged);
                }
            }
        }

        public void LoadMore ()
        {
        }

        public IEnumerable<TagModel> Data {
            get { return models; }
        }

        public long Count {
            get { return models.Count; }
        }

        public bool HasMore {
            get { return false; }
        }

        public bool IsLoading {
            get { return false; }
        }
    }
}

