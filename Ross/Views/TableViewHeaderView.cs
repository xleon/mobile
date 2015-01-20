using System;
using CoreGraphics;
using UIKit;
using Toggl.Ross.Theme;

namespace Toggl.Ross.Views
{
    public class TableViewHeaderView : UIView
    {
        private UIView backgroundView;

        public TableViewHeaderView ()
        {
            backgroundView = new UIView ();
            AddSubview (backgroundView);

            this.Apply (Style.TableViewHeader);
        }

        public override void LayoutSubviews ()
        {
            base.LayoutSubviews ();

            backgroundView.Frame = new CGRect (0, -480, Frame.Width, 480);
        }

        public override UIColor BackgroundColor
        {
            get { return backgroundView.BackgroundColor; }
            set { backgroundView.BackgroundColor = value; }
        }
    }
}
