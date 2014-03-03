using System;
using System.Collections.Generic;
using Toggl.Phoebe.Data.Models;
using XPlatUtils;

namespace Toggl.Phoebe.Data.Views
{
    public class WorkspaceTagsView : ModelsView<TagModel>
    {
        private readonly List<TagModel> models = new List<TagModel> ();
        #pragma warning disable 0414
        private readonly Subscription<ModelChangedMessage> subscriptionModelChanged;
        #pragma warning restore 0414
        private Guid workspaceId;
        private bool subscriptionEnabled = true;

        public WorkspaceTagsView (Guid workspaceId)
        {
            this.workspaceId = workspaceId;
            Reload ();

            var bus = ServiceContainer.Resolve<MessageBus> ();
            subscriptionModelChanged = bus.Subscribe<ModelChangedMessage> (OnModelChanged);
        }

        private void OnModelChanged (ModelChangedMessage msg)
        {
            if (!subscriptionEnabled)
                return;

            var model = msg.Model as TagModel;
            if (model == null)
                return;

            if (msg.PropertyName == TagModel.PropertyName
                || model.WorkspaceId == workspaceId) {
                ChangeDataAndNotify (delegate {
                    Sort ();
                });
            } else if (msg.PropertyName == TagModel.PropertyWorkspaceId
                       || msg.PropertyName == TagModel.PropertyIsShared) {
                if (model.WorkspaceId == workspaceId) {
                    ChangeDataAndNotify (delegate {
                        models.Add (model);
                        Sort ();
                    });
                } else {
                    var idx = models.IndexOf (model);
                    if (idx >= 0) {
                        ChangeDataAndNotify (delegate {
                            models.RemoveAt (idx);
                        });
                    }
                }
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

        private void ChangeDataAndNotify (Action change)
        {
            OnPropertyChanging (PropertyCount);
            OnPropertyChanging (PropertyModels);
            change ();
            OnPropertyChanged (PropertyModels);
            OnPropertyChanged (PropertyCount);
        }

        public override void Reload ()
        {
            subscriptionEnabled = false;
            try {
                ChangeDataAndNotify (delegate {
                    models.Clear ();
                    models.AddRange (Model.Query<TagModel> ((m) => m.WorkspaceId == workspaceId).NotDeleted ());
                    Sort ();
                });
            } finally {
                subscriptionEnabled = true;
            }
        }

        private void Sort ()
        {
            models.Sort ((a, b) => (a.Name ?? String.Empty).CompareTo ((b.Name ?? String.Empty)));
        }

        public override void LoadMore ()
        {
        }

        public override IEnumerable<TagModel> Models {
            get { return models; }
        }

        public override long Count {
            get { return models.Count; }
        }
    }
}

