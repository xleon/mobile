using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace Toggl.Phoebe.Data.Views
{
    public abstract class ModelsView<T> : ObservableObject, IModelsView<T>
        where T : Model, new()
    {
        private static string GetPropertyName<K> (Expression<Func<ModelsView<T>, K>> expr)
        {
            return expr.ToPropertyName ();
        }

        public abstract void Reload ();

        public abstract void LoadMore ();

        public static readonly string PropertyModels = GetPropertyName ((m) => m.Models);

        public abstract IEnumerable<T> Models {
            get;
        }

        public static readonly string PropertyCount = GetPropertyName ((m) => m.Count);

        public abstract long Count {
            get;
        }

        private long? totalCount;
        public static readonly string PropertyTotalCount = GetPropertyName ((m) => m.TotalCount);

        public long? TotalCount {
            get { return totalCount; }
            set {
                if (totalCount == value)
                    return;

                ChangePropertyAndNotify (PropertyTotalCount, delegate {
                    totalCount = value;
                });
            }
        }

        private bool hasMore;
        public static readonly string PropertyHasMore = GetPropertyName ((m) => m.HasMore);

        public bool HasMore {
            get { return hasMore; }
            protected set {
                if (hasMore == value)
                    return;

                ChangePropertyAndNotify (PropertyHasMore, delegate {
                    hasMore = value;
                });
            }
        }

        private bool loading;
        public static readonly string PropertyIsLoading = GetPropertyName ((m) => m.IsLoading);

        public bool IsLoading {
            get { return loading; }
            protected set {
                if (loading == value)
                    return;

                ChangePropertyAndNotify (PropertyIsLoading, delegate {
                    loading = value;
                });
            }
        }

        private bool hasError;
        public static readonly string PropertyHasError = GetPropertyName ((m) => m.HasError);

        public bool HasError {
            get { return hasError; }
            protected set {
                if (hasError == value)
                    return;

                ChangePropertyAndNotify (PropertyHasError, delegate {
                    hasError = value;
                });
            }
        }
    }
}
