using CoreGraphics;
using Toggl.Ross.Theme;
using UIKit;

namespace Toggl.Ross.Views
{
    public class TableViewRefreshView : UIRefreshControl
    {
        const float scaleFactor = .8f;

        public TableViewRefreshView()
        {
            this.Apply(Style.TableViewHeader);
            Transform = CGAffineTransform.MakeScale(scaleFactor, scaleFactor);

        }

        public void AdaptToTableView(UITableView tableView)
        {
            // container is needed to properly offset spinner
            var container = new UIView(new CGRect(0, 20, 0, 0));

            tableView.Add(container);
            container.Add(this);
        }

    }
}
