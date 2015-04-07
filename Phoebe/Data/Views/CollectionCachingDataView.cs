using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;

namespace Toggl.Phoebe.Data.Views
{
    public class CollectionCachingDataView<T> : ICollectionDataView<T>, IDisposable
    {
        private readonly ICollectionDataView<T> source;
        private IList<T> data;
        private long? count;
        private bool? hasMore;
        private bool? isLoading;

        public CollectionCachingDataView (ICollectionDataView<T> source)
        {
            if (source == null) {
                throw new ArgumentNullException ("source");
            }

            this.source = source;
            source.Updated += OnSourceUpdated;
            source.CollectionChanged += OnCollectionUpdated;
        }

        public void Dispose ()
        {
            source.Updated -= OnSourceUpdated;
            source.CollectionChanged -= OnCollectionUpdated;
        }

        public ICollectionDataView<T> Source
        {
            get { return source; }
        }

        private void OnSourceUpdated (object sender, EventArgs e)
        {
            // Invalidate cached data
            data = null;
            count = null;
            hasMore = null;
            isLoading = null;

            // Notify our listeners
            var handler = Updated;
            if (handler != null) {
                handler (this, EventArgs.Empty);
            }
        }

        private void OnCollectionUpdated (object sender, NotifyCollectionChangedEventArgs e)
        {
            // Notify our listeners
            var handler = CollectionChanged;
            if (handler != null) {
                handler (this, e);
            }
        }

        public event NotifyCollectionChangedEventHandler CollectionChanged;

        public event EventHandler Updated;

        public void Reload ()
        {
            source.Reload ();
        }

        public void LoadMore ()
        {
            source.LoadMore ();
        }

        public IEnumerable<T> Data
        {
            get {
                if (data == null) {
                    var e = source.Data;
                    data = e as IList<T>;
                    if (data == null) {
                        data = e.ToList ();
                    }
                }
                return data;
            }
        }

        public long Count
        {
            get {
                if (count == null) {
                    count = source.Count;
                }
                return count.Value;
            }
        }

        public bool HasMore
        {
            get {
                if (hasMore == null) {
                    hasMore = source.HasMore;
                }
                return hasMore.Value;
            }
        }

        public bool IsLoading
        {
            get {
                if (isLoading == null) {
                    isLoading = source.IsLoading;
                }
                return isLoading.Value;
            }
        }
    }
}
