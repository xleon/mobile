using CoreGraphics;
using Toggl.Ross.Theme;
using UIKit;

namespace Toggl.Ross.Views
{
    public class TableViewRefreshView : UIRefreshControl
    {
        const int sideSize = 5;
        const float scaleFactor = .8f;

        public TableViewRefreshView()
        {
            this.Apply(Style.TableViewHeader);
            Transform = CGAffineTransform.MakeScale(scaleFactor, scaleFactor);
        }

        public void AdaptToTableView(UITableView tableView)
        {
            tableView.AddSubview(this);

            // visual hack to hide a white
            // space between control and table header.
            var tableFrame = tableView.Frame;
            tableFrame.Y = -tableFrame.Size.Height;
            var view = new UIView(tableFrame);
            view.BackgroundColor = BackgroundColor;
            tableView.InsertSubviewBelow(view, this);
        }

    }
}
