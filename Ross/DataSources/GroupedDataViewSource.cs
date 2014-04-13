using System;
using System.Collections.Generic;
using System.Linq;
using MonoTouch.Foundation;
using MonoTouch.UIKit;
using Toggl.Phoebe.Data.Views;

namespace Toggl.Ross.DataSources
{
    public abstract class GroupedDataViewSource<TData, TSection, TRow> : PlainDataViewSource<TData>
    {
        private DataCache cache;

        protected GroupedDataViewSource (UITableView tableView, IDataView<TData> dataView) : base (tableView, dataView)
        {
        }

        public override void Attach ()
        {
            cache = new DataCache (this);
            base.Attach ();
        }

        protected List<TSection> GetCachedSections ()
        {
            return cache.GetSections ();
        }

        protected List<TRow> GetCachedRows (TSection section)
        {
            return cache.GetRows (section);
        }

        protected TSection GetSection (int section)
        {
            return GetCachedSections () [section];
        }

        protected TRow GetRow (NSIndexPath indexPath)
        {
            return GetCachedRows (GetSection (indexPath.Section)) [indexPath.Row];
        }

        protected override void Update ()
        {
            var oldCache = cache;
            var newCache = cache = new DataCache (this);

            TableView.BeginUpdates ();

            // Find sections and rows to delete:
            var sectionIdx = 0;
            foreach (var section in oldCache.GetSections()) {
                if (!newCache.GetSections ().Contains (section)) {
                    TableView.DeleteSections (new NSIndexSet ((uint)sectionIdx), UITableViewRowAnimation.Automatic);
                } else {
                    var oldRows = oldCache.GetRows (section);
                    var newRows = newCache.GetRows (section);
                    var rowIdx = 0;
                    foreach (var row in oldRows) {
                        if (!newRows.Contains (row)) {
                            TableView.DeleteRows (new[] { NSIndexPath.FromRowSection (rowIdx, sectionIdx) }, UITableViewRowAnimation.Automatic);
                        }

                        rowIdx += 1;
                    }
                }

                sectionIdx += 1;
            }

            // Determine moved items
            sectionIdx = 0;
            foreach (var section in newCache.GetSections()) {
                var sectionOldIdx = oldCache.GetSections ().IndexOf (section);
                // When it doesn't exist in the old, we don't increment the sectionIdx
                if (sectionOldIdx < 0)
                    continue;

                if (sectionIdx != sectionOldIdx) {
                    TableView.MoveSection (sectionOldIdx, sectionIdx);
                }

                var oldRows = oldCache.GetRows (section);
                var newRows = newCache.GetRows (section);
                var rowIdx = 0;
                foreach (var row in newRows) {
                    var rowOldIdx = oldRows.IndexOf (row);
                    // When it doesn't exist in the old, we don't increment the rowIdx
                    if (rowOldIdx < 0)
                        continue;

                    if (rowIdx != rowOldIdx) {
                        TableView.MoveRow (
                            NSIndexPath.FromRowSection (rowOldIdx, sectionOldIdx),
                            NSIndexPath.FromRowSection (rowIdx, sectionIdx));
                    }

                    rowIdx += 1;
                }

                sectionIdx += 1;
            }

            // Find sections and rows to insert
            sectionIdx = 0;
            foreach (var section in newCache.GetSections()) {
                if (!oldCache.GetSections ().Contains (section)) {
                    TableView.InsertSections (new NSIndexSet ((uint)sectionIdx), UITableViewRowAnimation.Automatic);
                } else {
                    var oldRows = oldCache.GetRows (section);
                    var newRows = newCache.GetRows (section);
                    var rowIdx = 0;
                    foreach (var row in newRows) {
                        if (!oldRows.Contains (row)) {
                            TableView.InsertRows (new[] { NSIndexPath.FromRowSection (rowIdx, sectionIdx) }, UITableViewRowAnimation.Automatic);
                        }

                        rowIdx += 1;
                    }
                }

                sectionIdx += 1;
            }

            TableView.EndUpdates ();

            UpdateFooter ();
        }

        protected abstract IEnumerable<TSection> GetSections ();

        protected abstract IEnumerable<TRow> GetRows (TSection section);

        public override int NumberOfSections (UITableView tableView)
        {
            return cache.GetSections ().Count;
        }

        public override int RowsInSection (UITableView tableview, int section)
        {
            return GetCachedRows (GetSection (section)).Count;
        }

        private class DataCache
        {
            private readonly List<TSection> sections;
            private readonly Dictionary<TSection, List<TRow>> sectionRows;

            public DataCache (GroupedDataViewSource<TData, TSection, TRow> dataSource)
            {
                sections = dataSource.GetSections ().ToList ();
                sectionRows = new Dictionary<TSection, List<TRow>> (sections.Count);
                foreach (var section in sections) {
                    sectionRows [section] = dataSource.GetRows (section).ToList ();
                }
            }

            public List<TSection> GetSections ()
            {
                return sections;
            }

            public List<TRow> GetRows (TSection section)
            {
                return sectionRows [section];
            }
        }
    }
}
