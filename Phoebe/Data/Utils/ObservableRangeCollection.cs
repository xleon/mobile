using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;

namespace Toggl.Phoebe.Data.Utils
{
    /// <summary>
    /// Represents a dynamic data collection that provides notifications when items get added, removed, or when the whole list is refreshed.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class ObservableRangeCollection<T> : ObservableCollection<T>
    {
        public void AddRange (IEnumerable<T> collection)
        {
            if (collection == null) { throw new ArgumentNullException ("collection"); }

            int startingIndex = Items.Count;
            foreach (var i in collection) { Items.Add (i); }
            OnCollectionChanged (new NotifyCollectionChangedEventArgs (NotifyCollectionChangedAction.Add, collection as List<T>, startingIndex));
        }

        public void RemoveRange (IEnumerable<T> collection)
        {
            if (collection == null) { throw new ArgumentNullException ("collection"); }

            var enumerable = collection.ToList ();
            foreach (var i in enumerable) { Items.Remove (i); }
            OnCollectionChanged (new NotifyCollectionChangedEventArgs (NotifyCollectionChangedAction.Remove, enumerable.ToList()));
        }

        public void Replace (T item)
        {
            ReplaceRange (new [] { item });
        }

        public void ReplaceRange (IEnumerable<T> collection)
        {
            if (collection == null) { throw new ArgumentNullException ("collection"); }

            Items.Clear();
            foreach (var i in collection) { Items.Add (i); }
            OnCollectionChanged (new NotifyCollectionChangedEventArgs (NotifyCollectionChangedAction.Reset));
        }

        public void RemoveFromIndex (int index)
        {
            for (int i = index; i < Items.Count; i++) {
                Items.RemoveAt (index);
            }
        }

        public ObservableRangeCollection() { }

        public ObservableRangeCollection (IEnumerable<T> collection) : base (collection) { }
    }
}

