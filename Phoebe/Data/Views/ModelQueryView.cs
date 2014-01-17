using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace Toggl.Phoebe.Data.Views
{
    /// <summary>
    /// Query view wraps IModelQuery and retrieves only required amount of data at once.
    /// </summary>
    public class ModelQueryView<T> : ModelsView<T>
        where T : Model, new()
    {
        private static string GetPropertyName<K> (Expression<Func<ModelQueryView<T>, K>> expr)
        {
            return expr.ToPropertyName ();
        }

        private readonly IModelQuery<T> query;
        private readonly int batchSize;
        private readonly List<T> data = new List<T> ();

        public ModelQueryView (IModelQuery<T> query, int batchSize)
        {
            this.query = query;
            this.batchSize = batchSize;
            Reload ();
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
            HasError = false;

            try {
                ChangeDataAndNotify (delegate {
                    data.Clear ();
                    data.AddRange (query.Skip (data.Count).Take (batchSize));
                });

                var count = query.Count ();
                TotalCount = count;
                HasMore = data.Count < count;
            } catch {
                HasError = true;
            }
        }

        public override void LoadMore ()
        {
            HasError = false;

            try {
                ChangeDataAndNotify (delegate {
                    data.AddRange (query.Skip (data.Count).Take (batchSize));
                });

                var count = query.Count ();
                TotalCount = count;
                HasMore = data.Count < count;
            } catch {
                HasError = true;
            }
        }

        public override IEnumerable<T> Models {
            get { return data; }
        }

        public override long Count {
            get { return data.Count; }
        }
    }
}

