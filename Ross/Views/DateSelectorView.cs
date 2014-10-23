using System;
using System.Drawing;
using MonoTouch.UIKit;
using Toggl.Ross.Theme;

namespace Toggl.Ross.Views
{
    public class DateSelectorView : UIView
    {
        const float minHeight = 40;
        const float minWidth = 300;

        public string DateContent
        {
            set {
                dateLabel.Text = value;
            }
        }

        public EventHandler LeftArrowPressed;
        public EventHandler RightArrowPressed;

        private UILabel dateLabel;
        private UIButton leftArrow;
        private UIButton rightArrow;
        private float padding = 30;
        private float arrowWidth = 40;

        public DateSelectorView ( RectangleF frame)
        {
            if (frame.Height < minHeight) {
                frame.Height = minHeight;
            }

            if (frame.Width < minWidth) {
                frame.Width = minWidth;
            }

            Frame = frame;
            BackgroundColor = Color.LightestGray; // real color DateSelectorGray = UIColor.FromRGB (0xF8, 0xF8, 0xF8);
            dateLabel = new UILabel ().Apply (Style.ReportsView.DateSelectorLabel);
            Add (dateLabel);

            leftArrow = new UIButton ().Apply (Style.ReportsView.DateSelectorLeftArrowButton);
            leftArrow.TouchUpInside += (sender, e) => {
                if ( LeftArrowPressed != null) {
                    LeftArrowPressed.Invoke ( this, new EventArgs());
                }
            };
            Add (leftArrow);

            rightArrow = new UIButton ().Apply (Style.ReportsView.DateSelectorRightArrowButton);
            rightArrow.TouchUpInside += (sender, e) => {
                if ( RightArrowPressed != null) {
                    RightArrowPressed.Invoke ( this, new EventArgs());
                }
            };
            Add (rightArrow);

            setFormattedPeriod ();
        }

        public override void LayoutSubviews ()
        {
            base.LayoutSubviews ();

            dateLabel.Frame = new RectangleF ( padding, 0, Frame.Width - padding * 2, Frame.Height);
            leftArrow.Frame = new RectangleF ( padding, 0, arrowWidth, Frame.Height);
            rightArrow.Frame = new RectangleF (Frame.Width - arrowWidth - padding, 0, arrowWidth, Frame.Height);

        }

        private void setFormattedPeriod()
        {

        }
    }
}

