using System;
using UIKit;
using CoreGraphics;
using Toggl.Ross.Theme;

namespace Toggl.Ross.Views
{
    public class SimpleEmptyView : UIView
    {
        private readonly UILabel titleLabel;
        private readonly UILabel messageLabel;

        public SimpleEmptyView()
        {
            Add(titleLabel = new UILabel().Apply(Style.EmptyView.TitleLabel));

            Add(messageLabel = new UILabel().Apply(Style.EmptyView.MessageLabel));
        }

        public override void LayoutSubviews()
        {
            var titleSize = titleLabel.SizeThatFits(Frame.Size);
            var messageSize = messageLabel.SizeThatFits(new CGSize(Frame.Width, Frame.Height - titleSize.Height));
            var spacing = titleSize.Height * 0.25f;

            var topOffset = (Frame.Height - titleSize.Height - spacing - messageSize.Height) / 2;

            titleLabel.Frame = new CGRect(
                (Frame.Width - titleSize.Width) / 2,
                topOffset,
                titleSize.Width,
                titleSize.Height
            );

            messageLabel.Frame = new CGRect(
                (Frame.Width - messageSize.Width) / 2,
                titleLabel.Frame.Bottom + spacing,
                messageSize.Width,
                messageSize.Height
            );
        }

        public string Title
        {
            get { return titleLabel.Text; }
            set
            {
                if (titleLabel.Text == value)
                {
                    return;
                }
                titleLabel.Text = value;
                SetNeedsLayout();
            }
        }

        public string Message
        {
            get { return messageLabel.Text; }
            set
            {
                if (messageLabel.Text == value)
                {
                    return;
                }
                messageLabel.Text = value;
                SetNeedsLayout();
            }
        }
    }
}

