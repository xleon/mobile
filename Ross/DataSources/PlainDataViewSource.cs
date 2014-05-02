using System;
using System.Drawing;
using MonoTouch.CoreFoundation;
using MonoTouch.UIKit;
using Toggl.Phoebe.Data.Views;

namespace Toggl.Ross.DataSources
{
    public abstract class PlainDataViewSource<T> : UITableViewSource
    {
        private readonly UITableView tableView;
        private readonly IDataView<T> dataView;
        private UIActivityIndicatorView defaultFooterView;

        protected PlainDataViewSource (UITableView tableView, IDataView<T> dataView)
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

        public virtual void Attach ()
        {
            tableView.Source = this;
            UpdateFooter ();
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
            tableView.ReloadData ();
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
            } else {
                tableView.TableFooterView = null;
            }
        }

        public override int NumberOfSections (UITableView tableView)
        {
            return 1;
        }

        public override int RowsInSection (UITableView tableview, int section)
        {
            return (int)dataView.Count;
        }

        public UITableView TableView {
            get { return tableView; }
        }
    }
}

