using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;

namespace Toggl.Phoebe.Data.Views
{
    public class CollectionCachingDataView<T> : ICollectionDataView<T>, IDisposable
    {
        private readonly ICollectionDataView<T> source;
        private List<T> data;

        public CollectionCachingDataView (ICollectionDataView<T> source)
        {
            if (source == null) {
                throw new ArgumentNullException ("source");
            }

            data = new List<T> (source.Data);

            this.source = source;
            this.source.CollectionChanged += OnCollectionUpdated;
            this.source.OnIsLoadingChanged += OnLoading;
            this.source.OnHasMoreChanged += OnHasMore;
        }

        public void Dispose ()
        {
            source.CollectionChanged -= OnCollectionUpdated;
            source.OnIsLoadingChanged -= OnLoading;
            source.OnHasMoreChanged -= OnHasMore;

            data.Clear ();
            data = null;
        }

        public ICollectionDataView<T> Source
        {
            get { return source; }
        }

        private void OnCollectionUpdated (object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Reset) {
                data = new List<T> (source.Data);
            }

            if (e.Action == NotifyCollectionChangedAction.Add) {
                if (e.NewItems.Count == 1) {
                    data.Insert (e.NewStartingIndex, source.Data.ElementAtOrDefault (e.NewStartingIndex));
                } else {
                    if (e.NewStartingIndex == 0) {
                        data.Clear ();
                    }

                    for (int i = e.NewStartingIndex; i < e.NewStartingIndex + e.NewItems.Count; i++) {
                        var item = source.Data.ElementAtOrDefault (i);
                        if (i == data.Count) {
                            data.Insert (i, item);
                        } else if (i > data.Count) {
                            data.Add (item);
                        } else {
                            data [i] = item;
                        }
                    }
                }
            }

            if (e.Action == NotifyCollectionChangedAction.Remove) {
                data.RemoveAt (e.OldStartingIndex);
            }

            if (e.Action == NotifyCollectionChangedAction.Replace) {
                data [e.NewStartingIndex] = source.Data.ElementAtOrDefault (e.NewStartingIndex);
            }

            if (e.Action == NotifyCollectionChangedAction.Move) {
                var savedItem = data [e.OldStartingIndex];
                data.RemoveAt (e.OldStartingIndex);
                data.Insert (e.NewStartingIndex, savedItem);
            }

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
                return data;
            }
        }

        public int Count
        {
            get {
                return data.Count;
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