using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace Toggl.Phoebe.Data
{
    public partial class Model
    {
        private class ForeignRelationData
        {
            public string IdProperty { get; set; }

            public Guid? Id { get; set; }

            public string InstanceProperty { get; set; }

            public Type InstanceType { get; set; }

            public Model Instance { get; set; }
        }

        private readonly List<ForeignRelationData> fkRelations = new List<ForeignRelationData> ();

        protected int ForeignRelation<T> (string idProperty, string instanceProperty)
        {
            fkRelations.Add (new ForeignRelationData () {
                IdProperty = idProperty,
                InstanceProperty = instanceProperty,
                InstanceType = typeof(T),
            });
            return fkRelations.Count;
        }

        protected Guid? GetForeignId (int relationId)
        {
            var fk = fkRelations [relationId - 1];
            return fk.Id;
        }

        protected void SetForeignId (int relationId, Guid? value)
        {
            var fk = fkRelations [relationId - 1];
            if (fk.Id == value)
                return;

            ChangePropertyAndNotify (fk.IdProperty, delegate {
                fk.Id = value;
            });

            // Try to resolve id to model
            Model inst = null;
            if (fk.Id != null) {
                inst = Model.Manager.Cached (fk.InstanceType).FirstOrDefault ((m) => m.Id == fk.Id.Value);
            }
            if (inst != fk.Instance) {
                ChangePropertyAndNotify (fk.InstanceProperty, delegate {
                    fk.Instance = inst;
                });
            }
        }

        protected T GetForeignModel<T> (int relationId)
            where T : Model
        {
            return (T)GetForeignModel (relationId);
        }

        private Model GetForeignModel (int relationId)
        {
            var fk = fkRelations [relationId - 1];
            if (fk.Instance != null)
                return fk.Instance;

            if (fk.Id != null) {
                // Lazy loading, try to load the value from shared models, or database.
                var inst = Model.Manager.Get (fk.InstanceType, fk.Id.Value);

                ChangePropertyAndNotify (fk.InstanceProperty, delegate {
                    fk.Instance = inst;
                });
            }

            return fk.Instance;
        }

        protected void SetForeignModel<T> (int relationId, T value)
            where T : Model
        {
            var fk = fkRelations [relationId - 1];
            if (value != null)
                value = Model.Update (value);
            if (fk.Instance == value)
                return;

            ChangePropertyAndNotify (fk.InstanceProperty, delegate {
                fk.Instance = value;
            });

            // Update current id:
            var id = fk.Instance != null ? fk.Instance.Id : (Guid?)null;
            if (fk.Id != id) {
                ChangePropertyAndNotify (fk.IdProperty, delegate {
                    fk.Id = id;
                });
            }
        }

        public Dictionary<string, Model> GetAllForeignModels ()
        {
            var dict = new Dictionary<string, Model> ();
            for (var i = 0; i < fkRelations.Count; i++) {
                var relationId = i + 1;
                var fk = fkRelations [relationId - 1];
                dict [fk.InstanceProperty] = GetForeignModel (relationId);
            }
            return dict;
        }
    }
}