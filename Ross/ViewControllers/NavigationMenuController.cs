using System;
using System.Drawing;
using MonoTouch.CoreGraphics;
using MonoTouch.Foundation;
using MonoTouch.UIKit;
using Toggl.Phoebe.Net;
using XPlatUtils;
using Toggl.Ross.Theme;

namespace Toggl.Ross.ViewControllers
{
    public class NavigationMenuController
    {
        private UIViewController controller;
        private UIView containerView;
        private UIView menuView;
        private UIDynamicAnimator menuAnimator;
        private UIButton logButton;
        private UIButton recentButton;
        private UIButton settingsButton;
        private UIButton signOutButton;
        private UIButton[] menuButtons;
        private UIView[] separators;
        private bool menuShown;

        public void Attach (UIViewController controller)
        {
            this.controller = controller;

            controller.NavigationItem.LeftBarButtonItem = new UIBarButtonItem (
                Image.IconNav.ImageWithRenderingMode (UIImageRenderingMode.AlwaysOriginal),
                UIBarButtonItemStyle.Plain, OnNavigationButtonTouched);
        }

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

            menuView = new UIView ().ApplyStyle (Style.NavMenu.Background);

            menuButtons = new[] {
                (recentButton = new UIButton ()),
                (logButton = new UIButton ()),
                (settingsButton = new UIButton ()),
                (signOutButton = new UIButton ()),
            };
            recentButton.SetTitle ("NavMenuRecent".Tr (), UIControlState.Normal);
            logButton.SetTitle ("NavMenuLog".Tr (), UIControlState.Normal);
            settingsButton.SetTitle ("NavMenuSettings".Tr (), UIControlState.Normal);
            signOutButton.SetTitle ("NavMenuSignOut".Tr (), UIControlState.Normal);

            foreach (var menuButton in menuButtons) {
                var isActive = (menuButton == recentButton && controller is RecentViewController)
                               || (menuButton == logButton && controller is LogViewController);

                if (isActive) {
                    menuButton.ApplyStyle (Style.NavMenu.HighlightedItem);
                } else {
                    menuButton.ApplyStyle (Style.NavMenu.NormalItem);
                }
                menuButton.TouchUpInside += OnMenuButtonTouchUpInside;
            }

            separators = new UIView[menuButtons.Length - 1];
            for (var i = 0; i < separators.Length; i++) {
                separators [i] = new UIView ().ApplyStyle (Style.NavMenu.Separator);
            }

            menuView.AddSubviews (separators);
            menuView.AddSubviews (menuButtons);
            containerView.AddSubview (menuView);

            menuAnimator = new UIDynamicAnimator (containerView);
            menuAnimator.Delegate = new AnimatorDelegate (this);

            // Layout items:
            var offsetY = 15f;
            var sepIdx = 0;
            foreach (var menuButton in menuButtons) {
                menuButton.SizeToFit ();

                var frame = menuButton.Frame;
                frame.Width = navController.View.Frame.Width;
                frame.Y = offsetY;
                menuButton.Frame = frame;

                offsetY += frame.Height;

                // Position separator
                if (sepIdx < separators.Length) {
                    var separator = separators [sepIdx];
                    var rightMargin = menuButton.ContentEdgeInsets.Right;
                    separator.Frame = new RectangleF (
                        x: rightMargin,
                        y: offsetY,
                        width: navController.View.Frame.Width - rightMargin,
                        height: 1f
                    );

                    sepIdx += 1;
                    offsetY += separator.Frame.Height;
                }
            }
            offsetY += 15f;

            containerView.Frame = new RectangleF (
                x: 0,
                y: navController.NavigationBar.Frame.Bottom,
                width: navController.View.Frame.Width,
                height: offsetY
            );

            menuView.Frame = new RectangleF (
                x: 0,
                y: -containerView.Frame.Height,
                width: containerView.Frame.Width,
                height: containerView.Frame.Height
            );

            return;
        }

        private void OnMenuButtonTouchUpInside (object sender, EventArgs e)
        {
            if (sender == recentButton && !(controller is RecentViewController)) {
                var navController = controller.NavigationController;
                navController.SetViewControllers (new[] { new RecentViewController () }, true);
                // TODO: Store user selection?
            } else if (sender == logButton && !(controller is LogViewController)) {
                var navController = controller.NavigationController;
                navController.SetViewControllers (new[] { new LogViewController () }, true);
                // TODO: Store user selection?
            } else if (sender == settingsButton) {
            } else if (sender == signOutButton) {
                ServiceContainer.Resolve<AuthManager> ().Forget ();
            }

            ToggleMenu ();
        }

        private void OnNavigationButtonTouched (object sender, EventArgs e)
        {
            ToggleMenu ();
        }

        private void ToggleMenu ()
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

        private class AnimatorDelegate : UIDynamicAnimatorDelegate
        {
            private readonly NavigationMenuController menuController;

            public AnimatorDelegate (NavigationMenuController menuController)
            {
                this.menuController = menuController;
            }

            public override void DidPause (UIDynamicAnimator animator)
            {
                // Remove from subview
                if (!menuController.menuShown) {
                    menuController.containerView.RemoveFromSuperview ();
                }
            }

            public override void WillResume (UIDynamicAnimator animator)
            {
                // Make sure that the containerView has been added the the view hiearchy
                if (menuController.containerView.Superview == null) {
                    var navController = menuController.controller.NavigationController;
                    if (navController != null) {
                        navController.View.AddSubview (menuController.containerView);
                    }
                }
            }
        }
    }
}
