using System;
using System.Drawing;
using MonoTouch.CoreGraphics;
using MonoTouch.Foundation;
using MonoTouch.UIKit;
using Toggl.Ross.Theme;

namespace Toggl.Ross.ViewControllers
{
    public class NavigationMenuController
    {
        private UIViewController controller;

        public NavigationMenuController ()
        {
        }

        public void Attach (UIViewController controller)
        {
            this.controller = controller;

            controller.NavigationItem.LeftBarButtonItem = new UIBarButtonItem (
                Image.IconNav.ImageWithRenderingMode (UIImageRenderingMode.AlwaysOriginal),
                UIBarButtonItemStyle.Plain, OnNavigationButtonTouched);
        }

        private UIView containerView;
        private UIView menuView;
        private UIDynamicAnimator menuAnimator;
        private bool menuShown;

        private void EnsureViews ()
        {
            if (containerView != null)
                return;

            var navController = controller.NavigationController;
            if (navController == null)
                return;

            containerView = new UIView () {
                ClipsToBounds = true,
            };

            menuView = new UIView () {
                BackgroundColor = UIColor.Black.ColorWithAlpha (0.90f),
            };

            containerView.AddSubview (menuView);

            menuAnimator = new UIDynamicAnimator (containerView);

            containerView.Frame = new RectangleF (
                x: 0,
                y: navController.NavigationBar.Frame.Bottom,
                width: navController.View.Frame.Width,
                height: 250f
            );

            menuView.Frame = new RectangleF (
                x: 0,
                y: -containerView.Frame.Height,
                width: containerView.Frame.Width,
                height: containerView.Frame.Height
            );

            navController.View.AddSubview (containerView);

            return;
        }

        private void OnNavigationButtonTouched (object sender, EventArgs e)
        {
            EnsureViews ();

            if (containerView == null)
                return;

            menuAnimator.RemoveAllBehaviors ();

            if (menuShown) {
                var collision = new UICollisionBehavior (menuView);
                collision.AddBoundary (
                    (NSString)"top",
                    new PointF (0, -menuView.Frame.Height - 1f),
                    new PointF (containerView.Frame.Width, -menuView.Frame.Height - 1f));
                menuAnimator.AddBehavior (collision);

                menuAnimator.AddBehavior (new UIGravityBehavior (menuView) {
                    GravityDirection = new CGVector (0, -1),
                    Magnitude = 4,
                });
            } else {
                var collision = new UICollisionBehavior (menuView);
                collision.AddBoundary (
                    (NSString)"bottom",
                    new PointF (0, containerView.Frame.Height),
                    new PointF (containerView.Frame.Width, containerView.Frame.Height));
                menuAnimator.AddBehavior (collision);

                menuAnimator.AddBehavior (new UIGravityBehavior (menuView) {
                    Magnitude = 4,
                });
            }

            menuShown = !menuShown;
            containerView.UserInteractionEnabled = menuShown;
        }
    }
}
