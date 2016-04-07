using System;
using CoreAnimation;
using CoreGraphics;
using Foundation;
using Toggl.Ross.Theme;
using UIKit;

namespace Toggl.Ross.Views
{
    public class StatusView : UIView
    {
        private const string RotateAnimationKey = "rotateAnim";

        private UIButton retryButton;
        private UIButton cancelButton;
        private UILabel statusLabel;
        private bool isSyncing;
        private string statusSyncingText;
        private string statusFailText;

        public StatusView()
        {
            Add(retryButton = new UIButton().Apply(Style.SyncStatus.RetryButton));
            Add(cancelButton = new UIButton().Apply(Style.SyncStatus.CancelButton));
            Add(statusLabel = new UILabel().Apply(Style.SyncStatus.StatusLabel));

            retryButton.TouchUpInside += (s, e) =>
            {
                if (Retry != null)
                {
                    Retry();
                }
            };
            cancelButton.TouchUpInside += (s, e) =>
            {
                if (Cancel != null)
                {
                    Cancel();
                }
            };

            this.Apply(Style.SyncStatus.BarBackground);

            statusSyncingText = "SyncStatusSyncing".Tr();
            statusFailText = "SyncStatusFail".Tr();

            ResetState();
        }

        public Action Retry { get; set; }

        public Action Cancel { get; set; }

        public string StatusFailText
        {
            get
            {
                return statusFailText;
            }
            set
            {
                statusFailText = value;
                ResetState();
            }
        }

        public string StatusSyncingText
        {
            get
            {
                return statusSyncingText;
            }
            set
            {
                statusSyncingText = value;
                ResetState();
            }
        }

        public override void LayoutSubviews()
        {
            base.LayoutSubviews();

            var size = Frame.Size;

            var buttonDimension = size.Height;
            retryButton.Frame = new CGRect(
                0, (size.Height - buttonDimension) / 2,
                buttonDimension, buttonDimension);
            cancelButton.Frame = new CGRect(
                size.Width - retryButton.Frame.Width,
                (size.Height - retryButton.Frame.Height) / 2,
                buttonDimension, buttonDimension);

            var statusX = retryButton.Frame.X + retryButton.Frame.Width;
            statusLabel.Frame = new CGRect(
                statusX, 0, cancelButton.Frame.X - statusX, size.Height);
        }

        private void ResetState()
        {
            if (isSyncing)
            {
                retryButton.UserInteractionEnabled = cancelButton.UserInteractionEnabled = false;
                statusLabel.Text = statusSyncingText;
                cancelButton.Hidden = true;

                StartRetryRotation();
            }
            else
            {
                retryButton.UserInteractionEnabled = cancelButton.UserInteractionEnabled = true;
                statusLabel.Text = statusFailText;
                cancelButton.Hidden = false;
            }
        }

        private void StartRetryRotation()
        {
            var layer = retryButton.ImageView.Layer;

            var anim = layer.AnimationForKey(RotateAnimationKey) as CABasicAnimation;
            if (anim == null)
            {
                anim = CABasicAnimation.FromKeyPath("transform.rotation.z");
                anim.From = NSNumber.FromFloat(0f);
                anim.To = NSNumber.FromFloat(2f * (float)Math.PI);
                anim.Duration = 2f;

                layer.AddAnimation(anim, RotateAnimationKey);
            }
        }

        public bool IsSyncing
        {
            get { return isSyncing; }
            set
            {
                if (isSyncing == value)
                {
                    return;
                }
                isSyncing = value;
                ResetState();
            }
        }
    }
}

