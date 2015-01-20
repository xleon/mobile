using System;
using System.Collections.Generic;
using CoreGraphics;
using System.Linq;
using CoreFoundation;
using Foundation;
using UIKit;
using Toggl.Phoebe.Data;
using Toggl.Phoebe.Data.DataObjects;
using Toggl.Phoebe.Data.Views;

namespace Toggl.Ross.DataSources
{
    public abstract class GroupedDataViewSource<TData, TSection, TRow> : UITableViewSource
    {
        private readonly UITableView tableView;
        private readonly IDataView<TData> dataView;
        private UIActivityIndicatorView defaultFooterView;
        private DataCache cache;

        protected GroupedDataViewSource (UITableView tableView, IDataView<TData> dataView)
        {
            this.tableView = tableView;
            this.dataView = dataView;
            dataView.Updated += OnDataViewUpdated;
        }

        protected override void Dispose (bool disposing)
        {
            if (disposing) {
                dataView.Updated -= OnDataViewUpdated;
                if (tableView.Source == this) {
                    tableView.Source = null;
                }
            }
            base.Dispose (disposing);
        }

        public UIView EmptyView { get; set; }

        public virtual void Attach ()
        {
            cache = new DataCache (this);
            tableView.Source = this;
            UpdateFooter ();
            TryLoadMore ();
        }

        public bool IsEmpty
        {
            get { return !HasData && !dataView.IsLoading && !dataView.HasMore; }
        }

        protected virtual bool HasData
        {
            get {
                var sections = cache.GetSections ();
                if (sections.Count == 1) {
                    return cache.GetRows (sections [0]).Count > 0;
                }
                return sections.Count > 0;
            }
        }

        public override void Scrolled (UIScrollView scrollView)
        {
            TryLoadMore ();
        }

        private void TryLoadMore ()
        {
            var currentOffset = tableView.ContentOffset.Y;
            var maximumOffset = tableView.ContentSize.Height - tableView.Frame.Height;

            if (maximumOffset - currentOffset <= 200.0) {
                // Automatically load more
                if (!dataView.IsLoading && dataView.HasMore) {
                    dataView.LoadMore ();
                }
            }
        }

        private void OnDataViewUpdated (object sender, EventArgs e)
        {
            if (Handle == IntPtr.Zero) {
                return;
            }
            ScheduleUpdate ();
            TryLoadMore ();
        }

        private bool updateScheduled;

        protected void ScheduleUpdate ()
        {
            if (updateScheduled) {
                return;
            }
            updateScheduled = true;

            // For whatever reason we need to dispatch the message again in order for the data to update
            // during scroll animation.
            DispatchQueue.MainQueue.DispatchAsync (delegate {
                updateScheduled = false;
                if (tableView.Source == this) {
                    Update ();
                }
            });
        }

        protected virtual bool SectionsMatch (TSection a, TSection b)
        {
            var data = a as CommonData;
            if (data != null) {
                return data.Matches (b);
            }
            return Object.ReferenceEquals (a, b);
        }

        protected virtual bool RowsMatch (TRow a, TRow b)
        {
            var data = a as CommonData;
            if (data != null) {
                return data.Matches (b);
            }
            return Object.ReferenceEquals (a, b);
        }

        protected virtual void Update ()
        {
            tableView.BeginUpdates ();

            var oldCache = cache;
            var newCache = cache = new DataCache (this);

            var oldSections = oldCache.GetSections ();
            var newSections = newCache.GetSections ();
            var sectionChanges = ChangesResolver.Resolve (oldSections, newSections, SectionsMatch);

            foreach (var change in sectionChanges) {
                if (change.Action == ChangesResolver.ResolvedAction.Delete) {
                    tableView.DeleteSections (NSIndexSet.FromIndex (change.OldIndex), UITableViewRowAnimation.Automatic);
                } else if (change.Action == ChangesResolver.ResolvedAction.Insert) {
                    tableView.InsertSections (NSIndexSet.FromIndex (change.NewIndex), UITableViewRowAnimation.Automatic);
                } else if (change.Action == ChangesResolver.ResolvedAction.Keep) {
                    // Detect changes in section rows
                    var oldRows = oldCache.GetRows (oldSections [change.OldIndex]);
                    var newRows = newCache.GetRows (newSections [change.NewIndex]);
                    var rowsMaxCount = Math.Max (oldRows.Count, newRows.Count);

                    for (var rowIdx = 0; rowIdx < rowsMaxCount; rowIdx++) {
                        var hasOldRow = rowIdx < oldRows.Count;
                        var hasNewRow = rowIdx < newRows.Count;
                        var oldRow = hasOldRow ? oldRows [rowIdx] : default (TRow);
                        var newRow = hasNewRow ? newRows [rowIdx] : default (TRow);

                        if (RowsMatch (oldRow, newRow)) {
                            continue;
                        }

                        if (hasOldRow) {
                            // Determine if we should delete this row
                            if (!newRows.Any (r => RowsMatch (oldRow, r))) {
                                tableView.DeleteRows (new[] { NSIndexPath.FromRowSection (rowIdx, change.OldIndex) }, UITableViewRowAnimation.Automatic);
                            }
                        }

                        if (hasNewRow) {
                            // Determine if we should insert or move this row
                            var oldRowIdx = oldRows.FindIndex (r => RowsMatch (newRow, r));
                            if (oldRowIdx >= 0) {
                                tableView.MoveRow (
                                    NSIndexPath.FromRowSection (oldRowIdx, change.OldIndex),
                                    NSIndexPath.FromRowSection (rowIdx, change.NewIndex));
                            } else {
                                // No old row found, insert this as new row
                                tableView.InsertRows (new[] { NSIndexPath.FromRowSection (rowIdx, change.NewIndex) }, UITableViewRowAnimation.Automatic);
                            }
                        }
                    }
                }
            }

            tableView.EndUpdates ();

            UpdateFooter ();
        }

        protected virtual void UpdateFooter ()
        {
            if (dataView.HasMore || dataView.IsLoading) {
                if (defaultFooterView == null) {
                    defaultFooterView = new UIActivityIndicatorView (UIActivityIndicatorViewStyle.Gray);
                    defaultFooterView.Frame = new CGRect (0, 0, 50, 50);
                }
                tableView.TableFooterView = defaultFooterView;
                defaultFooterView.StartAnimating ();
            } else if (IsEmpty) {
                tableView.TableFooterView = EmptyView;
            } else {
                tableView.TableFooterView = null;
            }
        }

        public override nint NumberOfSections (UITableView tableView)
        {
            return cache.GetSections ().Count;
        }

        public override nint RowsInSection (UITableView tableview, nint section)
        {
            return GetCachedRows (GetSection (section)).Count;
        }

        protected List<TSection> GetCachedSections ()
        {
            return cache.GetSections ();
        }

        protected List<TRow> GetCachedRows (TSection section)
        {
            return cache.GetRows (section);
        }

        protected TSection GetSection (nint section)
        {
            return GetCachedSections () [ (int)section];
        }

        protected TRow GetRow (NSIndexPath indexPath)
        {
            return GetCachedRows (GetSection (indexPath.Section)) [indexPath.Row];
        }

        public UITableView TableView
        {
            get { return tableView; }
        }

        public IDataView<TData> DataView
        {
            get { return dataView; }
        }

        protected abstract IEnumerable<TSection> GetSections ();

        protected abstract IEnumerable<TRow> GetRows (TSection section);

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
