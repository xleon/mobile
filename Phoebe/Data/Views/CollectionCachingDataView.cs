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
        private int? count;

        public CollectionCachingDataView (ICollectionDataView<T> source)
        {
            if (source == null) {
                throw new ArgumentNullException ("source");
            }

            this.source = source;
            this.source.Updated += OnSourceUpdated;
            this.source.CollectionChanged += OnCollectionUpdated;
            this.source.OnIsLoadingChanged += OnLoading;
            this.source.OnHasMoreChanged += OnHasMore;
        }

        public void Dispose ()
        {
            source.Updated -= OnSourceUpdated;
            source.CollectionChanged -= OnCollectionUpdated;
            source.OnIsLoadingChanged -= OnLoading;
            source.OnHasMoreChanged -= OnHasMore;
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

            // Notify our listeners
            var handler = Updated;
            if (handler != null) {
                handler (this, EventArgs.Empty);
            }
        }

        private void OnCollectionUpdated (object sender, NotifyCollectionChangedEventArgs e)
        {
            var handler = CollectionChanged;
            if (handler != null) {
                handler (this, e);
            }
        }

        private void OnLoading (object sender, EventArgs e)
        {
            var handler = OnIsLoadingChanged;
            if (handler != null) {
                handler (this, e);
            }
        }

        private void OnHasMore (object sender, EventArgs e)
        {
            var handler = OnHasMoreChanged;
            if (handler != null) {
                handler (this, e);
            }
        }

        public event NotifyCollectionChangedEventHandler CollectionChanged;

        public event EventHandler Updated;

        public event EventHandler OnIsLoadingChanged;

        public event EventHandler OnHasMoreChanged;

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

        public int Count
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
                return source.HasMore;
            }
        }

        public bool IsLoading
        {
            get {
                return source.IsLoading;
            }
        }
    }
}