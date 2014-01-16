using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace Toggl.Phoebe.Data.Views
{
    /// <summary>
    /// Query view wraps IModelQuery and retrieves only required amount of data at once.
    /// </summary>
    public class ModelQueryView<T> : ObservableObject, IModelsView<T>
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

        public void Reload ()
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

        public void LoadMore ()
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

        public static readonly string PropertyModels = GetPropertyName ((m) => m.Models);

        public IEnumerable<T> Models {
            get { return data; }
        }

        public static readonly string PropertyCount = GetPropertyName ((m) => m.Count);

        public long Count {
            get { return data.Count; }
        }

        private bool hasMore;
        public static readonly string PropertyHasMore = GetPropertyName ((m) => m.HasMore);

        public bool HasMore {
            get { return hasMore; }
            private set {
                if (hasMore == value)
                    return;

                ChangePropertyAndNotify (PropertyHasMore, delegate {
                    hasMore = value;
                });
            }
        }

        private long? totalCount;
        public static readonly string PropertyTotalCount = GetPropertyName ((m) => m.TotalCount);

        public long? TotalCount {
            get { return totalCount; }
            private set {
                if (totalCount == value)
                    return;

                ChangePropertyAndNotify (PropertyTotalCount, delegate {
                    totalCount = value;
                });
            }
        }

        public bool IsLoading {
            get { return false; }
        }

        private bool hasError;
        public static readonly string PropertyHasError = GetPropertyName ((m) => m.HasError);

        public bool HasError {
            get { return hasError; }
            private set {
                if (hasError == value)
                    return;

                ChangePropertyAndNotify (PropertyHasError, delegate {
                    hasError = value;
                });
            }
        }
    }
}

