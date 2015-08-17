using System;
using CoreGraphics;
using UIKit;
using Toggl.Phoebe.Data.Models;
using Toggl.Ross.Theme;

namespace Toggl.Ross.Views
{
    public abstract class SwipableTimeEntryTableViewCell : ModelTableViewCell<TimeEntryModel>
    {
        private const float ContinueSwipeWidth = 90f;
        private const float DeleteSwipeWidth = 100f;
        private const float SnapDistance = 20f;

        private readonly UILabel continueActionLabel;
        private readonly UILabel deleteActionLabel;
        private readonly UIView actualContentView;

        protected SwipableTimeEntryTableViewCell (IntPtr ptr) : base (ptr)
        {
            continueActionLabel = new UILabel () {
                Text = "SwipeTimeEntryContinue".Tr (),
            } .Apply (Style.TimeEntryCell.SwipeActionLabel);
            deleteActionLabel = new UILabel () {
                Text = "SwipeTimeEntryDelete".Tr (),
            } .Apply (Style.TimeEntryCell.SwipeActionLabel);
            actualContentView = new UIView ().Apply (Style.Log.CellContentView);

            BackgroundView = new UIView ();
            SelectedBackgroundView = new UIView ().Apply (Style.CellSelectedBackground);
            ContentView.AddSubviews (
                continueActionLabel,
                deleteActionLabel,
                actualContentView
            );

            actualContentView.AddGestureRecognizer (new UIPanGestureRecognizer (OnPanningGesture) {
                ShouldRecognizeSimultaneously = (a, b) => !panLockInHorizDirection,
            });
        }

        protected abstract void OnContinue ();

        protected abstract void OnDelete ();

        enum PanLock {
            None,
            Left,
            Right
        }

        private CGPoint panStart;
        private nfloat panDeltaX;
        private bool panLockInHorizDirection;
        private PanLock panLock;

        private void OnPanningGesture (UIPanGestureRecognizer gesture)
        {
            var leftWidth = ContinueSwipeWidth;
            var rightWidth = DeleteSwipeWidth;

            switch (gesture.State) {
            case UIGestureRecognizerState.Began:
                panStart = gesture.TranslationInView (actualContentView);
                panLockInHorizDirection = false;
                panLock = PanLock.None;
                break;
            case UIGestureRecognizerState.Changed:
                var currentPoint = gesture.TranslationInView (actualContentView);
                panDeltaX = panStart.X - currentPoint.X;

                if (!panLockInHorizDirection) {
                    if (Math.Abs (panDeltaX) > 10) {
                        // User is swiping the cell, lock them into this direction
                        panLockInHorizDirection = true;
                    } else if (Math.Abs (panStart.Y - currentPoint.Y) > 10) {
                        // User is starting to move upwards, let them scroll
                        gesture.Enabled = false;
                    }
                }

                // Switch pan lock
                var oldLock = panLock;

                switch (panLock) {
                case PanLock.None:
                    if (-panDeltaX >= leftWidth) {
                        panLock = PanLock.Left;
                    } else if (panDeltaX >= rightWidth) {
                        panLock = PanLock.Right;
                    }
                    break;
                case PanLock.Left:
                    if (-panDeltaX < leftWidth - SnapDistance) {
                        panLock = PanLock.None;
                    } else {
                        return;
                    }
                    break;
                case PanLock.Right:
                    if (panDeltaX < rightWidth - SnapDistance) {
                        panLock = PanLock.None;
                    } else {
                        return;
                    }
                    break;
                }

                // Apply delta limits
                switch (panLock) {
                case PanLock.Left:
                    panDeltaX = - (leftWidth + SnapDistance);
                    break;
                case PanLock.Right:
                    panDeltaX = rightWidth + SnapDistance;
                    break;
                }

                var shouldAnimate = oldLock != panLock;
                if (shouldAnimate) {
                    UIView.Animate (0.1, 0,
                                    UIViewAnimationOptions.CurveEaseOut,
                                    LayoutActualContentView, null);
                } else {
                    LayoutActualContentView ();
                }

                break;
            case UIGestureRecognizerState.Cancelled:
            case UIGestureRecognizerState.Ended:
                if (!gesture.Enabled) {
                    gesture.Enabled = true;
                }

                var velocityX = gesture.VelocityInView (gesture.View).X;

                var absolutePanDeltaX = Math.Abs (panDeltaX);
                var maxX = velocityX < 0 ? leftWidth : -rightWidth;

                var duration = (Math.Abs (maxX) - absolutePanDeltaX) / velocityX;

                UIView.Animate (duration, delegate {
                    var frame = ContentView.Frame;
                    panDeltaX = maxX - frame.X;
                    LayoutActualContentView();

                }, delegate {
                    LayoutActualContentView();
                });

                Console.WriteLine ("velocity {0}, duration {1}", velocityX, duration);
                break;
            }
        }

        private void LayoutActualContentView ()
        {
            var frame = ContentView.Frame;
            frame.X -= panDeltaX;
            actualContentView.Frame = frame;

            if (panDeltaX < 0) {
                BackgroundView.Apply (Style.TimeEntryCell.ContinueState);
            } else if (panDeltaX > 0) {
                BackgroundView.Apply (Style.TimeEntryCell.DeleteState);
            } else {
                BackgroundView.Apply (Style.TimeEntryCell.NoSwipeState);
            }

            switch (panLock) {
            case PanLock.None:
                continueActionLabel.Alpha = (nfloat)Math.Min (1, Math.Max (0, -2 * panDeltaX / ContinueSwipeWidth - 1));
                deleteActionLabel.Alpha = (nfloat)Math.Min (1, Math.Max (0, 2 * Math.Abs (panDeltaX) / DeleteSwipeWidth - 1));
                break;
            case PanLock.Left:
                continueActionLabel.Alpha = 1;
                deleteActionLabel.Alpha = 0;
                break;
            case PanLock.Right:
                continueActionLabel.Alpha = 0;
                deleteActionLabel.Alpha = 1;
                break;
            }
        }

        public override void LayoutSubviews ()
        {
            base.LayoutSubviews ();

            var contentFrame = ContentView.Frame;

            LayoutActualContentView ();

            continueActionLabel.Frame = new CGRect (
                x: 0, y: 0,
                height: contentFrame.Height,
                width: ContinueSwipeWidth + SnapDistance
            );

            deleteActionLabel.Frame = new CGRect (
                x: contentFrame.Width - DeleteSwipeWidth,
                y: 0,
                height: contentFrame.Height,
                width: DeleteSwipeWidth
            );
        }

        protected UIView ActualContentView
        {
            get { return actualContentView; }
        }
    }
}
