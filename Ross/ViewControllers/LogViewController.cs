using System;
using System.Collections.Generic;
using System.Drawing;
using MonoTouch.Foundation;
using MonoTouch.UIKit;
using Toggl.Phoebe.Data.Models;
using Toggl.Phoebe.Data.Views;
using Toggl.Ross.DataSources;
using Toggl.Ross.Theme;
using Toggl.Ross.Views;

namespace Toggl.Ross.ViewControllers
{
    public class LogViewController : UITableViewController
    {
        public LogViewController () : base (UITableViewStyle.Plain)
        {
            EdgesForExtendedLayout = UIRectEdge.None;
            new Source (TableView).Attach ();
            TableView.TableHeaderView = new TableViewHeaderView ();
        }

        public override void ViewDidLoad ()
        {
            base.ViewDidLoad ();

            Title = "Log";
        }

        class Source : GroupedDataViewSource<object, AllTimeEntriesView.DateGroup, TimeEntryModel>
        {
            readonly static NSString EntryCellId = new NSString ("EntryCellId");
            readonly static NSString SectionHeaderId = new NSString ("SectionHeaderId");
            readonly AllTimeEntriesView dataView;

            public Source (UITableView tableView) : this (tableView, new AllTimeEntriesView ())
            {
            }

            private Source (UITableView tableView, AllTimeEntriesView dataView) : base (tableView, dataView)
            {
                this.dataView = dataView;

                tableView.RegisterClassForCellReuse (typeof(TimeEntryCell), EntryCellId);
                tableView.RegisterClassForHeaderFooterViewReuse (typeof(SectionHeaderView), SectionHeaderId);
            }

            protected override IEnumerable<AllTimeEntriesView.DateGroup> GetSections ()
            {
                return dataView.DateGroups;
            }

            protected override IEnumerable<TimeEntryModel> GetRows (AllTimeEntriesView.DateGroup section)
            {
                return section.Models;
            }

            public override UITableViewCell GetCell (UITableView tableView, NSIndexPath indexPath)
            {
                var cell = (TimeEntryCell)tableView.DequeueReusableCell (EntryCellId, indexPath);
                cell.Rebind (GetRow (indexPath));
                return cell;
            }

            public override float GetHeightForHeader (UITableView tableView, int section)
            {
                return 42;
            }

            public override UIView GetViewForHeader (UITableView tableView, int section)
            {
                var view = (SectionHeaderView)tableView.DequeueReusableHeaderFooterView (SectionHeaderId);
                view.Rebind (GetSection (section));
                return view;
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

        class SectionHeaderView : UITableViewHeaderFooterView
        {
            private readonly UILabel textLabel;

            public SectionHeaderView (IntPtr ptr) : base (ptr)
            {
                textLabel = new UILabel ();
                ContentView.AddSubview (textLabel);
                BackgroundView = new UIView () {
                    BackgroundColor = Color.LightGray,
                };
            }

            public override void LayoutSubviews ()
            {
                base.LayoutSubviews ();
                textLabel.Frame = new RectangleF (PointF.Empty, ContentView.Frame.Size);
            }

            public void Rebind (AllTimeEntriesView.DateGroup data)
            {
                textLabel.Text = data.Date.ToShortDateString ();
            }
        }
    }
}
