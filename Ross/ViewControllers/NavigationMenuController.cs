using System;
using System.Drawing;
using MonoTouch.UIKit;
using Toggl.Phoebe.Net;
using XPlatUtils;
using Toggl.Ross.Data;
using Toggl.Ross.Theme;

namespace Toggl.Ross.ViewControllers
{
    public class NavigationMenuController
    {
        private UIViewController controller;
        private UIView containerView;
        private UIView menuView;
        private UIButton logButton;
        private UIButton recentButton;
        private UIButton settingsButton;
        private UIButton signOutButton;
        private UIButton[] menuButtons;
        private UIView[] separators;
        private bool menuShown;
        private bool isAnimating;
        private TogglWindow window;

        public void Attach (UIViewController controller)
        {
            this.controller = controller;

            window = AppDelegate.TogglWindow;

            window.OnHitTest += OnTogglWindowHit;

            controller.NavigationItem.LeftBarButtonItem = new UIBarButtonItem (
                Image.IconNav.ImageWithRenderingMode (UIImageRenderingMode.AlwaysOriginal),
                UIBarButtonItemStyle.Plain, OnNavigationButtonTouched);
        }

        public void Detach ()
        {
            if (window != null)
                window.OnHitTest -= OnTogglWindowHit;
            window = null;
        }

        private void OnTogglWindowHit (UIView view)
        {
            if (menuShown) {
                bool hitInsideMenu = IsSubviewOfMenu (view);
                if (!hitInsideMenu)
                    ToggleMenu ();
            }
        }

        private bool IsSubviewOfMenu (UIView other)
        {
            if (menuView == null)
                return false;

            var enumerator = menuView.Subviews.GetEnumerator ();

            while (enumerator.MoveNext ()) {
                if (enumerator.Current == other)
                    return true;
            }

            return false;
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

            menuView = new UIView ().Apply (Style.NavMenu.Background);

            menuButtons = new[] {
                // (recentButton = new UIButton ()),
                (logButton = new UIButton ()),
                (settingsButton = new UIButton ()),
                (signOutButton = new UIButton ()),
            };
            // recentButton.SetTitle ("NavMenuRecent".Tr (), UIControlState.Normal);
            logButton.SetTitle ("NavMenuLog".Tr (), UIControlState.Normal);
            settingsButton.SetTitle ("NavMenuSettings".Tr (), UIControlState.Normal);
            signOutButton.SetTitle ("NavMenuSignOut".Tr (), UIControlState.Normal);

            foreach (var menuButton in menuButtons) {
                var isActive = (menuButton == recentButton && controller is RecentViewController)
                               || (menuButton == logButton && controller is LogViewController);

                if (isActive) {
                    menuButton.Apply (Style.NavMenu.HighlightedItem);
                } else {
                    menuButton.Apply (Style.NavMenu.NormalItem);
                }
                menuButton.TouchUpInside += OnMenuButtonTouchUpInside;
            }

            separators = new UIView[menuButtons.Length - 1];
            for (var i = 0; i < separators.Length; i++) {
                separators [i] = new UIView ().Apply (Style.NavMenu.Separator);
            }

            menuView.AddSubviews (separators);
            menuView.AddSubviews (menuButtons);
            containerView.AddSubview (menuView);

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
                ServiceContainer.Resolve<SettingsStore> ().PreferredStartView = "recent";
                var navController = controller.NavigationController;
                navController.SetViewControllers (new[] { new RecentViewController () }, true);
            } else if (sender == logButton && !(controller is LogViewController)) {
                ServiceContainer.Resolve<SettingsStore> ().PreferredStartView = "log";
                var navController = controller.NavigationController;
                navController.SetViewControllers (new[] { new LogViewController () }, true);
            } else if (sender == settingsButton) {
                var navController = controller.NavigationController;
                navController.PushViewController (new SettingsViewController (), true);
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
            if (isAnimating)
                return;

            EnsureViews ();

            if (containerView == null)
                return;

            isAnimating = true;

            if (menuShown) {
                UIView.Animate (
                    0.4, 0,
                    UIViewAnimationOptions.BeginFromCurrentState | UIViewAnimationOptions.CurveEaseIn,
                    delegate {
                        menuView.Frame = new RectangleF (
                            x: 0,
                            y: -containerView.Frame.Height,
                            width: containerView.Frame.Width,
                            height: containerView.Frame.Height
                        );
                    },
                    delegate {
                        if (!menuShown) {
                            // Remove from subview
                            containerView.RemoveFromSuperview ();
                        }
                        isAnimating = false;
                    });
            } else {
                UIView.Animate (
                    0.4, 0,
                    UIViewAnimationOptions.BeginFromCurrentState | UIViewAnimationOptions.CurveEaseOut,
                    delegate {
                        menuView.Frame = new RectangleF (
                            x: 0,
                            y: 0,
                            width: containerView.Frame.Width,
                            height: containerView.Frame.Height
                        );
                    }, delegate {
                        isAnimating = false;
                    });

                // Make sure that the containerView has been added the the view hiearchy
                if (containerView.Superview == null) {
                    var navController = controller.NavigationController;
                    if (navController != null) {
                        navController.View.AddSubview (containerView);
                    }
                }
            }

            menuShown = !menuShown;
            containerView.UserInteractionEnabled = menuShown;
        }
    }
}
