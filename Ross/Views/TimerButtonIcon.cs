using System;
using CoreAnimation;
using CoreGraphics;
using Toggl.Ross.Theme;
using UIKit;

namespace Toggl.Ross.Views
{
    class TimerButtonIcon : UIView
    {
        private readonly CAShapeLayer mask;


        public TimerButtonIcon()
        {
            this.BackgroundColor = Color.White;

            this.Frame = new CGRect(8, 8, 24, 24);

            this.mask = new CAShapeLayer();
            this.mask.Path = this.getTrianglePath();
            this.mask.AffineTransform = new CGAffineTransform(6, 0, 0, 6, 12, 12);

            this.Layer.Mask = this.mask;
        }

        private CGPath getSquarePath()
        {
            var path = new CGPath();

            path.MoveToPoint(new CGPoint(-1, -1));
            path.AddLineToPoint(new CGPoint(-1, 1));
            path.AddLineToPoint(new CGPoint(1, 1));
            path.AddLineToPoint(new CGPoint(1, -1));
            path.CloseSubpath();

            path.MoveToPoint(new CGPoint(0, 0));
            path.AddLineToPoint(new CGPoint(0, 0));
            path.AddLineToPoint(new CGPoint(0, 0));
            path.AddLineToPoint(new CGPoint(0, 0));
            path.CloseSubpath();

            return path;
        }
        private CGPath getTrianglePath()
        {
            var path = new CGPath();

            path.MoveToPoint(new CGPoint(-0.8, -1.2));
            path.AddLineToPoint(new CGPoint(-0.8, 1.2));
            path.AddLineToPoint(new CGPoint(1.2, 0));
            path.AddLineToPoint(new CGPoint(1.2, 0));
            path.CloseSubpath();

            path.MoveToPoint(new CGPoint(0, 0));
            path.AddLineToPoint(new CGPoint(0, 0));
            path.AddLineToPoint(new CGPoint(0, 0));
            path.AddLineToPoint(new CGPoint(0, 0));
            path.CloseSubpath();

            return path;
        }
        private CGPath getPlusPath()
        {
            var path = new CGPath();

            const float x = 0.2f;
            const float y = 1.2f;

            path.MoveToPoint(new CGPoint(-y, -x));
            path.AddLineToPoint(new CGPoint(-y, x));
            path.AddLineToPoint(new CGPoint(y, x));
            path.AddLineToPoint(new CGPoint(y, -x));
            path.CloseSubpath();

            path.MoveToPoint(new CGPoint(-x, -y));
            path.AddLineToPoint(new CGPoint(-x, y));
            path.AddLineToPoint(new CGPoint(x, y));
            path.AddLineToPoint(new CGPoint(x, -y));
            path.CloseSubpath();

            return path;
        }

        public void SetState(TimerBar.State state, bool animated)
        {
            CGPath path = null;
            switch (state)
            {
                case TimerBar.State.ManualMode:
                    path = this.getPlusPath();
                    break;
                case TimerBar.State.TimerInactive:
                    path = this.getTrianglePath();
                    break;
                case TimerBar.State.TimerRunning:
                    path = this.getSquarePath();
                    break;
            }

            var oldPath = this.mask.Path;

            this.mask.Path = path;

            if (!animated)
                return;

            const double animTime = 0.3;

            var anim = CABasicAnimation.FromKeyPath("path");

            anim.Duration = animTime;
            anim.SetFrom(oldPath);
            anim.SetTo(path);
            anim.RemovedOnCompletion = true;

            this.mask.AddAnimation(anim, null);
        }
    }
}

