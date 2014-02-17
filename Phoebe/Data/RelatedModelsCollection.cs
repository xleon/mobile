using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using XPlatUtils;

namespace Toggl.Phoebe.Data
{
    public class RelatedModelsCollection<TRelated, TInter, TFrom, TTo> : IEnumerable<TInter>
        where TRelated : Model, new()
        where TFrom : Model, new()
        where TTo : Model, new()
        where TInter: IntermediateModel<TFrom, TTo>, new()
    {
        private readonly bool reverse;
        private readonly Model model;
        private List<TInter> relations;
        private bool preloaded;
        #pragma warning disable 0414
        private Subscription<ModelChangedMessage> subscriptionModelChanged;
        #pragma warning restore 0414

        public RelatedModelsCollection (Model model) : this (model, typeof(TRelated) == typeof(TFrom))
        {
        }

        public RelatedModelsCollection (Model model, bool reverse)
        {
            this.reverse = reverse;
            this.model = model;

            model.PropertyChanged += OnModelPropertyChanged;
        }

        private void OnModelPropertyChanged (object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == Model.PropertyIsPersisted) {
                if (model.IsShared) {
                    EnsureRelatedPreloaded ();
                    foreach (var inter in relations) {
                        inter.IsPersisted = inter.From.IsPersisted && inter.To.IsPersisted;
                    }
                }
            }
        }

        private Expression<Func<TInter, bool>> InterLookupQuery {
            get {
                if (reverse) {
                    return (inter) => inter.ToId == model.Id;
                } else {
                    return (inter) => inter.FromId == model.Id;
                }
            }
        }

        private Func<TInter, bool> InterLookupFunc {
            get {
                if (reverse) {
                    return (inter) => inter.ToId == model.Id;
                } else {
                    return (inter) => inter.FromId == model.Id;
                }
            }
        }

        private void OnModelChanged (ModelChangedMessage msg)
        {
            var inter = msg.Model as TInter;
            if (inter == null)
                return;

            if (msg.PropertyName == Model.PropertyIsShared
                || msg.PropertyName == IntermediateModel<Model, Model>.PropertyFromId
                || msg.PropertyName == IntermediateModel<Model, Model>.PropertyFromId) {
                if (IsOurInter (inter) && !relations.Contains (inter)) {
                    AddRelation (inter);
                }
            }
        }

        private bool IsOurInter (TInter inter)
        {
            if (!inter.IsShared || inter.DeletedAt != null)
                return false;
            if (model.Id != (reverse ? inter.ToId : inter.FromId))
                return false;
            return true;
        }

        private void EnsureLoaded ()
        {
            if (relations != null)
                return;

            relations = new List<TInter> ();
            var cachedInters = Model.Manager.Cached<TInter> ().Where (InterLookupFunc);
            var dbInters = Model.Query<TInter> (InterLookupQuery);
            var inters = cachedInters.Union (dbInters).Where (IsOurInter);
            foreach (var inter in inters) {
                AddRelation (inter);
            }

            var bus = ServiceContainer.Resolve<MessageBus> ();
            subscriptionModelChanged = bus.Subscribe<ModelChangedMessage> (OnModelChanged);
        }

        private void EnsureRelatedPreloaded ()
        {
            if (preloaded)
                return;

            EnsureLoaded ();
            preloaded = true;

            var ids = relations
                .Select ((inter) => reverse ? inter.FromId : inter.ToId)
                .Where ((v) => v.HasValue)
                .ToList ();
            Model.Query<TRelated> ((m) => ids.Contains (m.Id));
        }

        private void AddRelation (TInter inter)
        {
            inter.PropertyChanged += OnInterPropertyChanged;
            relations.Add (inter);
        }

        private void RemoveRelation (TInter inter)
        {
            inter.PropertyChanged -= OnInterPropertyChanged;
            relations.Remove (inter);
        }

        private void OnInterPropertyChanged (object sender, PropertyChangedEventArgs e)
        {
            var inter = (TInter)sender;
            if (e.PropertyName == Model.PropertyDeletedAt
                || e.PropertyName == IntermediateModel<Model, Model>.PropertyFromId
                || e.PropertyName == IntermediateModel<Model, Model>.PropertyFromId) {
                if (!IsOurInter (inter)) {
                    RemoveRelation (inter);
                }
            }
        }

        public TInter Add (TRelated relation)
        {
            if (!model.IsShared)
                throw new InvalidOperationException ("Cannot add many-to-many relations to non-shared model.");
            // The fact that we have to enforce persistance is sad, but currently no better way to keep things simple
            // and fast execution.
            if (!relation.IsShared)
                throw new ArgumentException ("Cannot add non-shared related model.", "relation");

            // Check for duplicates
            EnsureLoaded ();
            var inter = relations.FirstOrDefault ((m) => (reverse ? m.FromId : m.ToId) == relation.Id);

            // Create new relation
            if (inter == null) {
                inter = Model.Update (new TInter () {
                    From = (TFrom)(reverse ? relation : model),
                    To = (TTo)(reverse ? model : relation),
                    IsPersisted = model.IsPersisted && relation.IsPersisted,
                });
            }

            return inter;
        }

        public void Remove (TRelated relation)
        {
            if (!model.IsShared)
                throw new InvalidOperationException ("Cannot remove many-to-many relations from a non-shared model.");
            if (!relation.IsShared)
                return;

            EnsureLoaded ();

            var inters = relations.Where ((m) => (reverse ? m.FromId : m.ToId) == relation.Id).ToList ();
            foreach (var inter in inters) {
                RemoveRelation (inter);
                inter.Delete ();
            }
        }

        public void Clear ()
        {
            if (!model.IsShared)
                throw new InvalidOperationException ("Cannot clear many-to-many relations from a non-shared model.");

            EnsureLoaded ();

            // Delete all inters
            foreach (var inter in relations.ToList ()) {
                RemoveRelation (inter);
                inter.Delete ();
            }
        }

        public int Count {
            get {
                if (relations != null) {
                    return relations.Count;
                }
                return Model.Query<TInter> (InterLookupQuery).Count ();
            }
        }

        public IEnumerator<TInter> GetEnumerator ()
        {
            EnsureLoaded ();
            return relations.GetEnumerator ();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator ()
        {
            return GetEnumerator ();
        }
    }
}
