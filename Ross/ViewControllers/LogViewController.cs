using System;
using System.ComponentModel;
using System.Linq;
using MonoTouch.Foundation;
using MonoTouch.UIKit;
using Toggl.Phoebe.Data.Models;
using Toggl.Phoebe.Data.Views;

namespace Toggl.Ross.ViewControllers
{
    public class LogViewController : UITableViewController
    {
        public LogViewController () : base (UITableViewStyle.Grouped)
        {
            TableView.Source = new Source (TableView);
        }

        class Source : UITableViewSource
        {
            static NSString LoadingCellId = new NSString ("LoadingCellId");
            static NSString EntryCellId = new NSString ("EntryCellId");
            static NSString HeaderCellId = new NSString ("HeaderCellId");
            readonly UITableView tableView;
            readonly AllTimeEntriesView dataView;

            public Source (UITableView tableView)
            {
                this.tableView = tableView;
                this.dataView = new AllTimeEntriesView ();

                tableView.RegisterClassForCellReuse (typeof(IndicatorCell), LoadingCellId);
                tableView.RegisterClassForCellReuse (typeof(TimeEntryCell), EntryCellId);
                tableView.RegisterClassForCellReuse (typeof(HeaderCell), HeaderCellId);

                dataView.Updated += OnDataViewUpdated;
            }

            private void OnDataViewUpdated (object sender, EventArgs e)
            {
                if (Handle == IntPtr.Zero)
                    return;
                NotifyDataSetChanged ();
            }

            private void NotifyDataSetChanged ()
            {
                if (tableView.Source == this) {
                    tableView.ReloadData ();
                }
            }

            public override int NumberOfSections (UITableView tableView)
            {
                return 1;
            }

            private NSString GetCellId (NSIndexPath indexPath)
            {
                if (dataView.IsLoading && indexPath.Row == dataView.Count)
                    return LoadingCellId;
                var item = dataView.Data.ElementAt (indexPath.Row);
                if (item is AllTimeEntriesView.DateGroup)
                    return HeaderCellId;
                return EntryCellId;
            }

            public override UITableViewCell GetCell (UITableView tableView, NSIndexPath indexPath)
            {
                var cellId = GetCellId (indexPath);

                if (!dataView.IsLoading && dataView.HasMore && indexPath.Row + 4 > dataView.Count) {
                    dataView.LoadMore ();
                }

                if (cellId == LoadingCellId) {
                    var cell = (IndicatorCell)tableView.DequeueReusableCell (cellId, indexPath);
                    return cell;
                } else if (cellId == HeaderCellId) {
                    var cell = (HeaderCell)tableView.DequeueReusableCell (cellId, indexPath);
                    cell.Rebind ((AllTimeEntriesView.DateGroup)dataView.Data.ElementAt (indexPath.Row));
                    return cell;
                } else {
                    var cell = (TimeEntryCell)tableView.DequeueReusableCell (cellId, indexPath);
                    cell.Rebind ((TimeEntryModel)dataView.Data.ElementAt (indexPath.Row));
                    return cell;
                }
            }

            public override int RowsInSection (UITableView tableview, int section)
            {
                var rows = (int)dataView.Count;
                if (dataView.IsLoading) {
                    rows += 1;
                }
                return rows;
            }
        }

        class IndicatorCell : UITableViewCell
        {
            UIActivityIndicatorView indicatorView;

            public IndicatorCell (IntPtr ptr) : base (ptr)
            {
                Initialize ();
            }

            private void Initialize ()
            {
                indicatorView = new UIActivityIndicatorView (UIActivityIndicatorViewStyle.Gray);
                ContentView.Add (indicatorView);
            }

            public override void LayoutSubviews ()
            {
                base.LayoutSubviews ();

                indicatorView.Frame = new System.Drawing.RectangleF (
                    (ContentView.Frame.Width - indicatorView.Frame.Width) / 2,
                    (ContentView.Frame.Height - indicatorView.Frame.Height) / 2,
                    indicatorView.Frame.Width,
                    indicatorView.Frame.Height
                );
                indicatorView.StartAnimating ();
            }
        }

        class TimeEntryCell : UITableViewCell
        {
            public TimeEntryCell (IntPtr ptr) : base (ptr)
            {
            }

            public void Rebind (TimeEntryModel model)
            {
                var project = "(no project)";
                if (model.Project != null) {
                    if (model.Project.Client != null) {
                        project = String.Concat (model.Project.Name, " ", model.Project.Client.Name);
                    } else {
                        project = model.Project.Name;
                    }
                }
                TextLabel.Text = String.Concat (project, " ", model.Description);
            }
        }

        class HeaderCell : UITableViewCell
        {
            public HeaderCell (IntPtr ptr) : base (ptr)
            {
            }

            public void Rebind (AllTimeEntriesView.DateGroup data)
            {
                TextLabel.Text = data.Date.ToShortDateString ();
            }
        }
    }
}
