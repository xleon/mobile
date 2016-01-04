using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using Foundation;
using Toggl.Phoebe;
using UIKit;

namespace Toggl.Ross.DataSources
{
    public abstract class ObservableCollectionViewSource<TData, TSection, TRow> : UITableViewSource
    {
        protected readonly UITableView tableView;
        protected readonly ObservableCollection<TData> collection;
        protected UITableView TableView  { get { return tableView; }}

        protected ObservableCollectionViewSource (UITableView tableView, ObservableCollection<TData> collection)
        {
            this.tableView = tableView;
            this.collection = collection;
            collection.CollectionChanged += OnCollectionChange;
        }

        protected override void Dispose (bool disposing)
        {
            if (disposing) 
                collection.CollectionChanged -= OnCollectionChange;
            base.Dispose (disposing);
        }

        public override nint RowsInSection (UITableView tableview, nint section)
        {
            return GetCurrentRowsBySection (collection, section);
        }

        public override nint NumberOfSections (UITableView tableView)
        {
            return collection.OfType<TSection> ().Count ();
        }

        private void OnCollectionChange (object sender, NotifyCollectionChangedEventArgs e)
        {
            var collectionData = (ObservableCollection<TData>) sender;

            if (e.Action == NotifyCollectionChangedAction.Reset) {
                TableView.ReloadData();
            }

            Console.WriteLine (e.Action + " " + e.NewStartingIndex);

            if (e.Action == NotifyCollectionChangedAction.Add) {
                if (e.NewItems [0] is TSection) {
                    var indexSet = GetSectionFromPlainIndex (collectionData, e.NewStartingIndex);
                    TableView.InsertSections (indexSet, UITableViewRowAnimation.Automatic);
                } else {
                    var indexPath = GetRowFromPlainIndex (collectionData, e.NewStartingIndex);
                    TableView.InsertRows (new [] {indexPath}, UITableViewRowAnimation.Automatic);
                }
            }

            if (e.Action == NotifyCollectionChangedAction.Remove) {
                if (e.OldItems [0] is TSection) {
                    var indexSet = GetSectionFromPlainIndex (collectionData, e.OldStartingIndex);
                    TableView.DeleteSections (indexSet, UITableViewRowAnimation.Automatic);
                } else {
                    var indexPath = GetRowFromPlainIndex (collectionData, e.OldStartingIndex);
                    TableView.DeleteRows (new [] {indexPath}, UITableViewRowAnimation.Automatic);
                }
            }

            if (e.Action == NotifyCollectionChangedAction.Replace) {
                if (e.NewItems [0] is TSection) {
                    var indexSet = GetSectionFromPlainIndex (collectionData, e.NewStartingIndex);
                    TableView.ReloadSections (indexSet, UITableViewRowAnimation.Automatic);
                } else {
                    var indexPath = GetRowFromPlainIndex (collectionData, e.NewStartingIndex);
                    TableView.ReloadRows (new [] {indexPath}, UITableViewRowAnimation.Automatic);
                }
            }

            if (e.Action == NotifyCollectionChangedAction.Move) {
                if (! (e.NewItems [0] is TSection)) {
                    var fromIndexPath = GetRowFromPlainIndex (collectionData, e.OldStartingIndex);
                    var toIndexPath = GetRowFromPlainIndex (collectionData, e.NewStartingIndex);
                    TableView.MoveRow (fromIndexPath, toIndexPath);
                }
            }
        }

        public UIView EmptyView { get; set; }

        #region IEnumerable Utils
        protected NSIndexSet GetSectionFromPlainIndex (IEnumerable<TData> collection, int headerIndex)
        {
            var index = collection.Take (headerIndex).OfType <TSection> ().Count ();
            return NSIndexSet.FromIndex (index);
        }

        protected NSIndexPath GetRowFromPlainIndex (IEnumerable<TData> collection, int holderIndex)
        {
            var enumerable = collection.ToArray ();
            var row = enumerable.Take (holderIndex).Reverse ().IndexOf (p => p is TSection);
            var section = enumerable.Take (holderIndex).OfType <TSection> ().Count () - 1; // less one this time.
            return NSIndexPath.FromRowSection (row, section);
        }

        protected int GetPlainIndexFromSection (IEnumerable<TData> collection, nint sectionIndex)
        {
            return collection.IndexOf (p => p == collection.OfType <TSection> ().ElementAt ((int)sectionIndex));
        }

        protected int GetPlainIndexFromRow (IEnumerable<TData> collection, NSIndexPath rowIndexPath)
        {
            return GetPlainIndexFromSection (collection, rowIndexPath.Section) + rowIndexPath.Row + 1;
        }

        public static int GetCurrentRowsBySection (IEnumerable<TData> collection, nint sectionIndex)
        {
            var enumerable = collection.ToArray ();
            var startIndex = GetPlainIndexFromSection (enumerable, sectionIndex);
            return enumerable.Skip (startIndex + 1).TakeWhile (p => p is TRow).Count ();
        }
        #endregion
    }

}

