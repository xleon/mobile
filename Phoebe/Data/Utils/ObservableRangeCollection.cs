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
        public void Reset (IEnumerable<T> collection)
        {
            if (collection == null) { throw new ArgumentNullException ("collection"); }

            Items.Clear ();
            foreach (var i in collection) { Items.Add (i); }
            OnCollectionChanged (new NotifyCollectionChangedEventArgs (
                                     NotifyCollectionChangedAction.Reset));
        }

        public void Move (int oldIndex, int newIndex, T updatedItem)
        {
            var oldItem = Items [oldIndex];
            Items.RemoveAt (oldIndex);
            Items.Insert (newIndex, updatedItem);
            OnCollectionChanged (new NotifyCollectionChangedEventArgs (
                                     NotifyCollectionChangedAction.Move, updatedItem, newIndex, oldIndex));
            OnCollectionChanged (new NotifyCollectionChangedEventArgs (
                                     NotifyCollectionChangedAction.Replace, updatedItem, oldItem, newIndex));
        }

        public void InsertRange (IEnumerable<T> collection, int startingIndex)
        {
            if (collection == null) { throw new ArgumentNullException ("collection"); }
            var enumerable = collection.ToList ();
            if (!enumerable.Any ()) {
                return;
            }
            int counter = startingIndex;
            foreach (var item in enumerable) { Items.Insert (counter, item); counter++; }
            OnCollectionChanged (new NotifyCollectionChangedEventArgs (NotifyCollectionChangedAction.Add, enumerable, startingIndex));
        }

        public void AddRange (IEnumerable<T> collection)
        {
            if (collection == null) { throw new ArgumentNullException ("collection"); }
            var enumerable = collection as IList<T> ?? collection.ToList ();
            if (!enumerable.Any ()) {
                return;
            }
            int startingIndex = Items.Count;
            foreach (var i in enumerable) { Items.Add (i); }
            OnCollectionChanged (new NotifyCollectionChangedEventArgs (NotifyCollectionChangedAction.Add, enumerable, startingIndex));
        }

        public void RemoveRange (IEnumerable<T> collection)
        {
            if (collection == null) { throw new ArgumentNullException ("collection"); }

            var enumerable = collection.ToList ();
            var startAt = Items.IndexOf (enumerable.First ());
            foreach (var i in enumerable) { Items.Remove (i); }
            OnCollectionChanged (new NotifyCollectionChangedEventArgs (NotifyCollectionChangedAction.Remove, enumerable, startAt));
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

    public static class CollectionEventBuilder
    {

        public static NotifyCollectionChangedEventArgs GetEvent (NotifyCollectionChangedAction action, int newIndex, int oldIndex)
        {
            NotifyCollectionChangedEventArgs args;
            switch (action) {
            case NotifyCollectionChangedAction.Move:
                args = new NotifyCollectionChangedEventArgs (action, new Object(), newIndex, oldIndex);
                break;
            case NotifyCollectionChangedAction.Replace:
                args = new NotifyCollectionChangedEventArgs (action, new Object(), new Object(), newIndex);
                break;
            case NotifyCollectionChangedAction.Reset:
                args = new NotifyCollectionChangedEventArgs (action);
                break;
            default:
                args = new NotifyCollectionChangedEventArgs (action, new Object(), newIndex);
                break;
            }
            return args;
        }

        public static NotifyCollectionChangedEventArgs GetRangeEvent (NotifyCollectionChangedAction action, int newIndex, int numberOfItems)
        {
            NotifyCollectionChangedEventArgs args;
            switch (action) {
            case NotifyCollectionChangedAction.Add:
                args = new NotifyCollectionChangedEventArgs (action, new List<object> (new string[numberOfItems]), newIndex);
                break;
            default:
                args = new NotifyCollectionChangedEventArgs (action, new Object(), newIndex);
                break;
            }
            return args;
        }
    }
}

