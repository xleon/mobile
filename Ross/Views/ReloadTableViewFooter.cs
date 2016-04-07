using System;
using CoreGraphics;
using Toggl.Ross.Theme;
using UIKit;

namespace Toggl.Ross.Views
{
    public class ReloadTableViewFooter : UIView
    {
        private UILabel errorLabel;
        private UIButton syncButton;
        public Action SyncButtonPressedHandler { get; set; }

        public ReloadTableViewFooter()
        {
            errorLabel = new UILabel();
            errorLabel.Text = "FooterErrorLoadingLabel".Tr();
            errorLabel.Apply(Style.Log.ReloadTableViewFooterLabel);

            syncButton = UIButton.FromType(UIButtonType.System);
            syncButton.Apply(Style.Log.ReloadTableViewFooterButton);
            syncButton.SetTitle("FooterTryAgainLabel".Tr(), UIControlState.Normal);

            syncButton.TouchUpInside += (sender, e) =>
            {
                if (SyncButtonPressedHandler != null)
                {
                    SyncButtonPressedHandler.Invoke();
                }
            };

            Add(errorLabel);
            Add(syncButton);
        }

        public override void LayoutSubviews()
        {
            base.LayoutSubviews();
            errorLabel.Frame = new CGRect(5f, 5f, Frame.Width, Frame.Height / 2);
            syncButton.Frame = new CGRect(5f, Frame.Height / 2 - 7f, Frame.Width, Frame.Height / 2);
        }
    }
}

