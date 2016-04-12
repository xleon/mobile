using System;
using UIKit;
using CoreGraphics;
using Toggl.Ross.Theme;

namespace Toggl.Ross.Views
{
    public class OBMEmptyView : UIView
    {
        private readonly UILabel titleLabel;
        private readonly UILabel messageLabel;
        private readonly UIImageView arrowImageView;

        public OBMEmptyView()
        {
            Add(arrowImageView = new UIImageView().Apply(Style.OBMEmptyView.ArrowImageView));
            Add(titleLabel = new UILabel().Apply(Style.OBMEmptyView.TitleLabel));
            Add(messageLabel = new UILabel().Apply(Style.OBMEmptyView.MessageLabel));
        }

        public override void LayoutSubviews()
        {
            var titleSize = titleLabel.SizeThatFits(Frame.Size);
            var messageSize = messageLabel.SizeThatFits(new CGSize(Frame.Width, Frame.Height - titleSize.Height));
            var spacing = titleSize.Height * 0.25f;
            var arrowHeight = arrowImageView.Image.Size.Height;
            var arrowWidth = arrowImageView.Image.Size.Width;

            titleLabel.Frame = new CGRect(
                (Frame.Width - titleSize.Width) / 2,
                arrowHeight - titleSize.Height / 2,
                titleSize.Width,
                titleSize.Height
            );

            messageLabel.Frame = new CGRect(
                (Frame.Width - messageSize.Width) / 2,
                titleLabel.Frame.Bottom + spacing,
                messageSize.Width,
                messageSize.Height
            );

            arrowImageView.Frame = new CGRect(
                y: 0f,
                height: arrowHeight ,
                x: Frame.Width - arrowWidth - 5f,
                width: arrowWidth
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
