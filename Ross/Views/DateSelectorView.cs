using System;
using CoreGraphics;
using Toggl.Ross.Theme;
using UIKit;

namespace Toggl.Ross.Views
{
    public class DateSelectorView : UIView
    {
        public string DateContent
        {
            set
            {
                dateLabel.Text = value;
            }
        }

        public EventHandler LeftArrowPressed;
        public EventHandler RightArrowPressed;

        private UILabel dateLabel;
        private UIButton leftArrow;
        private UIButton rightArrow;

        readonly static nfloat dateWidth = 170;

        public DateSelectorView()
        {
            BackgroundColor = Color.LightestGray;
            dateLabel = new UILabel().Apply(Style.ReportsView.DateSelectorLabel);
            Add(dateLabel);

            leftArrow = new UIButton().Apply(Style.ReportsView.DateSelectorLeftArrowButton);
            leftArrow.TouchUpInside += (sender, e) =>
            {
                if (LeftArrowPressed != null)
                {
                    LeftArrowPressed.Invoke(this, new EventArgs());
                }
            };
            Add(leftArrow);

            rightArrow = new UIButton().Apply(Style.ReportsView.DateSelectorRightArrowButton);
            rightArrow.TouchUpInside += (sender, e) =>
            {
                if (RightArrowPressed != null)
                {
                    RightArrowPressed.Invoke(this, new EventArgs());
                }
            };
            Add(rightArrow);
        }

        public override void LayoutSubviews()
        {
            base.LayoutSubviews();

            leftArrow.Frame = new CGRect(0, 0, (Bounds.Width - dateWidth) / 2, Bounds.Height);
            dateLabel.Frame = new CGRect(leftArrow.Bounds.Width, 0, dateWidth, Bounds.Height);
            rightArrow.Frame = new CGRect(Bounds.Width - leftArrow.Bounds.Width, 0, (Bounds.Width - dateLabel.Bounds.Width) / 2, Bounds.Height);
            rightArrow.ImageEdgeInsets = new UIEdgeInsets(0, 0, 0, rightArrow.Bounds.Width - 30);
            leftArrow.ImageEdgeInsets = new UIEdgeInsets(0, leftArrow.Bounds.Width - 30, 0, 0);
        }

        public override void Draw(CGRect rect)
        {
            using(CGContext g = UIGraphics.GetCurrentContext())
            {
                Color.TimeBarBoderColor.SetColor();
                g.FillRect(new CGRect(0.0f, 0.0f, rect.Width, 1.0f / ContentScaleFactor));
            }
        }
    }
}

