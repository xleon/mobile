using System;
using CoreGraphics;
using UIKit;
using Toggl.Ross.Theme;

namespace Toggl.Ross.Views
{
    public class TableViewRefreshView : UIRefreshControl
    {
        public TableViewRefreshView ()
        {
            this.Apply (Style.TableViewHeader);
        }

        public void AdaptToTableView (UITableView tableView)
        {
            var frame = tableView.Frame;
            frame.Y = -frame.Size.Height;
            var view = new UIView (frame);
            view.BackgroundColor = BackgroundColor;
            tableView.InsertSubviewBelow (view, this);
        }
    }
}
