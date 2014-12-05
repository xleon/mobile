using System;
using System.Drawing;
using MonoTouch.UIKit;
using Toggl.Phoebe.Data;
using Toggl.Ross.Theme;

namespace Toggl.Ross.ViewControllers
{
    public class ReportsMenuController
    {
        private ReportsViewController controller;
        private UIView containerView;
        private UIView menuView;
        private UIButton dayButton;
        private UIButton weekButton;
        private UIButton yearButton;
        private UIButton[] menuButtons;
        private UIView[] separators;
        private bool menuShown;
        private bool isAnimating;
        private TogglWindow window;
        private ZoomSelectorView switcherView;

        public void Attach (ReportsViewController controller)
        {
            this.controller = controller;

            window = AppDelegate.TogglWindow;

            window.OnHitTest += OnTogglWindowHit;

            switcherView = new ZoomSelectorView () {
                IsMenuDisplayed = false,
                Level = controller.ZoomLevel
            };

            switcherView.SelectorButton.TouchUpInside += OnNavigationButtonTouched;
            controller.NavigationItem.TitleView = switcherView;
        }

        public void Detach ()
        {
            if (window != null) {
                window.OnHitTest -= OnTogglWindowHit;
                switcherView.SelectorButton.TouchUpInside -= OnNavigationButtonTouched;
            }
            window = null;
        }

        private void OnTogglWindowHit (UIView view)
        {
            if (menuShown) {
                bool hitInsideMenu = IsSubviewOfMenu (view);
                if (!hitInsideMenu) {
                    ToggleMenu ();
                }
            }
        }

        private bool IsSubviewOfMenu (UIView other)
        {
            if (menuView == null) {
                return false;
            }

            var enumerator = menuView.Subviews.GetEnumerator ();

            while (enumerator.MoveNext ()) {
                if (enumerator.Current == other) {
                    return true;
                }
            }

            return false;
        }

        private void EnsureViews ()
        {
            if (containerView != null) {
                return;
            }

            var navController = controller.NavigationController;
            if (navController == null) {
                return;
            }

            containerView = new UIView () {
                ClipsToBounds = true,
            };

            menuView = new UIView ().Apply (Style.NavMenu.Background);

            menuButtons = new[] {
                (dayButton = new UIButton ()),
                (weekButton = new UIButton ()),
                (yearButton = new UIButton ()),
            };
            dayButton.SetTitle ("ReportsMenuWeek".Tr (), UIControlState.Normal);
            weekButton.SetTitle ("ReportsMenuMonth".Tr (), UIControlState.Normal);
            yearButton.SetTitle ("ReportsMenuYear".Tr (), UIControlState.Normal);

            foreach (var menuButton in menuButtons) {
                var isActive = (menuButton == dayButton && controller.ZoomLevel == ZoomLevel.Week)
                               || (menuButton == weekButton && controller.ZoomLevel == ZoomLevel.Month)
                               || (menuButton == yearButton && controller.ZoomLevel == ZoomLevel.Year);

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
            if (sender == dayButton) {
                controller.ZoomLevel = ZoomLevel.Week;
            } else if (sender == weekButton) {
                controller.ZoomLevel = ZoomLevel.Month;
            } else if (sender == yearButton) {
                controller.ZoomLevel = ZoomLevel.Year;
            }
            switcherView.Level = controller.ZoomLevel;

            ToggleMenu ();
        }

        private void OnNavigationButtonTouched (object sender, EventArgs e)
        {
            ToggleMenu ();
        }

        private void ToggleMenu ()
        {
            if (isAnimating) {
                return;
            }

            EnsureViews ();

            if (containerView == null) {
                return;
            }

            isAnimating = true;

            foreach (var menuButton in menuButtons) {
                var isActive = (menuButton == dayButton && controller.ZoomLevel == ZoomLevel.Week)
                               || (menuButton == weekButton && controller.ZoomLevel == ZoomLevel.Month)
                               || (menuButton == yearButton && controller.ZoomLevel == ZoomLevel.Year);

                if (isActive) {
                    menuButton.Apply (Style.NavMenu.HighlightedItem);
                } else {
                    menuButton.Apply (Style.NavMenu.NormalItem);
                }
            }

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
            switcherView.IsMenuDisplayed = menuShown;
        }


        internal class ZoomSelectorView : UIView
        {
            private ZoomLevel _level;
            public ZoomLevel Level
            {
                get {
                    return _level;
                } set {
                    if (_level == value) {
                        return;
                    }
                    _level = value;
                    ChangeCurrentState ();
                }
            }

            private bool _isMenuDisplayed;
            public bool IsMenuDisplayed
            {
                get {
                    return _isMenuDisplayed;
                } set {
                    if (_isMenuDisplayed == value) {
                        return;
                    }
                    _isMenuDisplayed = value;
                    ChangeArrowState ();
                }
            }

            public UIButton SelectorButton;

            private readonly UIImage arrowDownImage;
            private readonly UIImage arrowUpImage;
            private readonly UIImageView arrowView;

            public ZoomSelectorView ()
            {
                Frame = new RectangleF ( 0,0, 100, 44);

                SelectorButton = new UIButton();
                SelectorButton = new UIButton ().Apply (Style.ReportsView.SelectorButton);
                SelectorButton.SetTitle ("ReportsTitleWeekly".Tr (), UIControlState.Normal); // dummy text stuff
                SelectorButton.SizeToFit();
                SelectorButton.Frame = new RectangleF ( Frame.X, (Frame.Height - SelectorButton.Frame.Height)/2 - 1, Frame.Width, SelectorButton.Frame.Height);
                Add ( SelectorButton);

                arrowUpImage = UIImage.FromFile ( "btn-arrow-up.png");
                arrowDownImage = UIImage.FromFile ( "btn-arrow-down.png");

                arrowView = new UIImageView ( arrowDownImage);
                arrowView.SizeToFit();
                arrowView.Frame = new RectangleF ( (Frame.Width - arrowView.Frame.Width)/2, SelectorButton.Frame.Height, arrowView.Frame.Width, arrowView.Frame.Height);
                Add ( arrowView);
            }

            private void ChangeCurrentState()
            {
                switch (_level) {
                case ZoomLevel.Week:
                    SelectorButton.SetTitle ("ReportsTitleWeekly".Tr (), UIControlState.Normal);
                    break;
                case ZoomLevel.Month:
                    SelectorButton.SetTitle ("ReportsTitleMonthly".Tr (), UIControlState.Normal);
                    break;
                case ZoomLevel.Year:
                    SelectorButton.SetTitle ("ReportsTitleYearly".Tr (), UIControlState.Normal);
                    break;
                }
            }

            private void ChangeArrowState()
            {
                arrowView.Image = _isMenuDisplayed ? arrowUpImage : arrowDownImage;
            }
        }
    }
}
