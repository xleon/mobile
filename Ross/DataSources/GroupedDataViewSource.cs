using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using MonoTouch.CoreFoundation;
using MonoTouch.Foundation;
using MonoTouch.UIKit;
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

        public bool IsEmpty {
            get { return !HasData && !dataView.IsLoading && !dataView.HasMore; }
        }

        protected virtual bool HasData {
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
            if (Handle == IntPtr.Zero)
                return;
            ScheduleUpdate ();
            TryLoadMore ();
        }

        private bool updateScheduled;

        protected void ScheduleUpdate ()
        {
            if (updateScheduled)
                return;
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

        protected virtual void Update ()
        {
            var oldCache = cache;
            var newCache = cache = new DataCache (this);

            tableView.BeginUpdates ();

            // Find sections and rows to delete:
            var sectionIdx = 0;
            foreach (var section in oldCache.GetSections()) {
                if (!newCache.GetSections ().Contains (section)) {
                    tableView.DeleteSections (new NSIndexSet ((uint)sectionIdx), UITableViewRowAnimation.Automatic);
                } else {
                    var oldRows = oldCache.GetRows (section);
                    var newRows = newCache.GetRows (section);
                    var rowIdx = 0;
                    foreach (var row in oldRows) {
                        if (!newRows.Contains (row)) {
                            tableView.DeleteRows (new[] { NSIndexPath.FromRowSection (rowIdx, sectionIdx) }, UITableViewRowAnimation.Automatic);
                        }

                        rowIdx += 1;
                    }
                }

                sectionIdx += 1;
            }

            // Determine new items and moved items
            sectionIdx = 0;
            foreach (var section in newCache.GetSections()) {
                var sectionOldIdx = oldCache.GetSections ().IndexOf (section);
                if (sectionOldIdx < 0) {
                    // New section needs to be inserted
                    tableView.InsertSections (new NSIndexSet ((uint)sectionIdx), UITableViewRowAnimation.Automatic);
                } else {
                    if (sectionIdx != sectionOldIdx) {
                        // Old section has changed idx, mark as moved
                        tableView.MoveSection (sectionOldIdx, sectionIdx);
                    }

                    var oldRows = oldCache.GetRows (section);
                    var newRows = newCache.GetRows (section);
                    var rowIdx = 0;
                    foreach (var row in newRows) {
                        var rowOldIdx = oldRows.IndexOf (row);
                        if (rowOldIdx < 0) {
                            // New row needs to be inserted
                            tableView.InsertRows (new[] { NSIndexPath.FromRowSection (rowIdx, sectionIdx) }, UITableViewRowAnimation.Automatic);
                        } else if (rowIdx != rowOldIdx) {
                            // Old row has changed idx, mark as moved
                            tableView.MoveRow (
                                NSIndexPath.FromRowSection (rowOldIdx, sectionOldIdx),
                                NSIndexPath.FromRowSection (rowIdx, sectionIdx));
                        }

                        rowIdx += 1;
                    }
                }

                sectionIdx += 1;
            }

            tableView.EndUpdates ();

            UpdateFooter ();
        }

        protected virtual void UpdateFooter ()
        {
            if (dataView.HasMore || dataView.IsLoading) {
                if (defaultFooterView == null) {
                    defaultFooterView = new UIActivityIndicatorView (UIActivityIndicatorViewStyle.Gray);
                    defaultFooterView.Frame = new RectangleF (0, 0, 50, 50);
                }
                tableView.TableFooterView = defaultFooterView;
                defaultFooterView.StartAnimating ();
            } else if (IsEmpty) {
                tableView.TableFooterView = EmptyView;
            } else {
                tableView.TableFooterView = null;
            }
        }

        public override int NumberOfSections (UITableView tableView)
        {
            return cache.GetSections ().Count;
        }

        public override int RowsInSection (UITableView tableview, int section)
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

        protected TSection GetSection (int section)
        {
            return GetCachedSections () [section];
        }

        protected TRow GetRow (NSIndexPath indexPath)
        {
            return GetCachedRows (GetSection (indexPath.Section)) [indexPath.Row];
        }

        public UITableView TableView {
            get { return tableView; }
        }

        public IDataView<TData> DataView {
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
