using System;
using CoreGraphics;
using UIKit;
using Toggl.Phoebe.Data.Models;
using Toggl.Ross.Theme;

namespace Toggl.Ross.Views
{
    public abstract class SwipableTimeEntryTableViewCell : ModelTableViewCell<TimeEntryModel>
    {
        private const float SwipeWidth = 100f;

        private const float MinDuration = 0.2f;
        private const float MaxDuration = 0.6f;

        private const float FallbackTreshold = 40.0f;
        private const float VelocityTreshold = 60.0f;

        private readonly UIButton continueActionButton;

        private readonly UIView actualContentView;

        protected SwipableTimeEntryTableViewCell (IntPtr ptr) : base (ptr)
        {
            continueActionButton = new UIButton () {} .Apply (Style.TimeEntryCell.SwipeActionButton).Apply (Style.TimeEntryCell.ContinueState);

            actualContentView = new UIView ().Apply (Style.Log.CellContentView);

            continueActionButton.SetTitle ("SwipeTimeEntryContinue".Tr (), UIControlState.Normal);

            BackgroundView = new UIView ();

            SelectedBackgroundView = new UIView ().Apply (Style.CellSelectedBackground);
            ContentView.AddSubviews (
                continueActionButton,
                actualContentView
            );

            actualContentView.AddGestureRecognizer (new UIPanGestureRecognizer (OnPanningGesture) {
                ShouldRecognizeSimultaneously = (a, b) => !panLockInHorizDirection,
            });
        }

        protected abstract void OnContinue ();

        private CGPoint panStart;
        private nfloat panDeltaX;
        private bool panLockInHorizDirection;

        private void OnPanningGesture (UIPanGestureRecognizer gesture)
        {
            switch (gesture.State) {
            case UIGestureRecognizerState.Began:
                panStart = gesture.TranslationInView (actualContentView);
                panLockInHorizDirection = false;
                break;
            case UIGestureRecognizerState.Changed:
                var currentPoint = gesture.TranslationInView (actualContentView);
                panDeltaX = panStart.X - currentPoint.X;

                if (panDeltaX > 0) {
                    panDeltaX = 0;
                    return;
                }

                if (!panLockInHorizDirection) {
                    if (Math.Abs (panDeltaX) > 30) {
                        // User is swiping the cell, lock them into this direction
                        panLockInHorizDirection = true;
                    } else if (Math.Abs (panStart.Y - currentPoint.Y) > 5) {
                        // User is starting to move upwards, let them scroll
                        gesture.Enabled = false;
                    }
                }

                if (-SwipeWidth > panDeltaX) {
                    panDeltaX = -SwipeWidth;
                }

                UIView.Animate (0.1, 0, UIViewAnimationOptions.CurveEaseOut, LayoutActualContentView, null);

                break;
            case UIGestureRecognizerState.Cancelled:
            case UIGestureRecognizerState.Ended:
                if (Editing) {
                    return;
                }

                if (!gesture.Enabled) {
                    gesture.Enabled = true;
                }

                var currentX = actualContentView.Frame.X;
                var velocityX = gesture.VelocityInView (gesture.View).X;

                float maxX = 0;

                if (((int)currentX ^ (int)velocityX) > 0
                        && (velocityX > VelocityTreshold || Math.Abs (currentX) > FallbackTreshold)
                        && panLockInHorizDirection) {
                    maxX = -SwipeWidth;
                }

                var absolutePanDeltaX = Math.Abs (panDeltaX);

                var duration = Math.Max (MinDuration, Math.Min (MaxDuration, (Math.Abs (maxX) - absolutePanDeltaX) / velocityX));

                UIView.Animate (duration, delegate {
                    LayoutActualContentView (maxX);
                }, delegate {
                    if (maxX < FallbackTreshold) {
                        return;
                    }
                    OnContinue ();
                    UIView.Animate (0.3f, () => LayoutActualContentView (0));
                });

                break;
            }
        }

        private void LayoutActualContentView (float maxEdge)
        {
            var frame = ContentView.Frame;
            panDeltaX = maxEdge - frame.X;
            LayoutActualContentView();
        }

        private void LayoutActualContentView ()
        {
            var frame = ContentView.Frame;
            frame.X -= panDeltaX;
            actualContentView.Frame = frame;
        }

        public override void LayoutSubviews ()
        {
            base.LayoutSubviews ();

            var contentFrame = ContentView.Frame;

            LayoutActualContentView ();

            continueActionButton.Frame = new CGRect (
                x: 0, y: 0,
                height: contentFrame.Height,
                width: SwipeWidth
            );
        }

        protected UIView ActualContentView
        {
            get { return actualContentView; }
        }
    }
}
