using System;
using System.Collections.Generic;
using System.Linq;

namespace Toggl.Phoebe.Data.Views
{
    public class CachingDataView<T> : IDataView<T>, IDisposable
    {
        private readonly IDataView<T> source;
        private IList<T> data;
        private long? count;
        private bool? hasMore;
        private bool? isLoading;

        public CachingDataView (IDataView<T> source)
        {
            if (source == null) {
                throw new ArgumentNullException ("source");
            }

            this.source = source;
            source.Updated += OnSourceUpdated;
        }

        public void Dispose ()
        {
            source.Updated -= OnSourceUpdated;
        }

        public IDataView<T> Source
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
