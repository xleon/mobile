using System;
using System.Collections.Generic;
using Cirrious.FluentLayouts.Touch;
using Toggl.Phoebe.Net;
using Toggl.Ross.Data;
using Toggl.Ross.Theme;
using UIKit;
using XPlatUtils;

namespace Toggl.Ross.ViewControllers
{
    public sealed class LeftViewController : UIViewController
    {
        private TogglWindow window;
        private UIButton logButton;
        private UIButton reportsButton;
        private UIButton settingsButton;
        private UIButton feedbackButton;
        private UIButton signOutButton;
        private UIButton[] menuButtons;

        public override void LoadView ()
        {
            base.LoadView ();
            window = AppDelegate.TogglWindow;
            View.BackgroundColor = UIColor.White;

            menuButtons = new[] {
                (logButton = new UIButton ()),
                (reportsButton = new UIButton ()),
                (settingsButton = new UIButton ()),
                (feedbackButton = new UIButton ()),
                (signOutButton = new UIButton ()),
            };
            logButton.SetTitle ("NavMenuLog".Tr (), UIControlState.Normal);
            reportsButton.SetTitle ("NavMenuReports".Tr (), UIControlState.Normal);
            settingsButton.SetTitle ("NavMenuSettings".Tr (), UIControlState.Normal);
            feedbackButton.SetTitle ("NavMenuFeedback".Tr (), UIControlState.Normal);
            signOutButton.SetTitle ("NavMenuSignOut".Tr (), UIControlState.Normal);

            logButton.HorizontalAlignment = reportsButton.HorizontalAlignment = settingsButton.HorizontalAlignment =
                                                feedbackButton.HorizontalAlignment = signOutButton.HorizontalAlignment = UIControlContentHorizontalAlignment.Left;

            foreach (var button in menuButtons) {
                button.Apply (Style.Left.Button);
                button.TouchUpInside += OnMenuButtonTouchUpInside;
                View.AddSubview (button);
            }

            View.SubviewsDoNotTranslateAutoresizingMaskIntoConstraints();

            View.AddConstraints (MakeConstraints (View));
        }

        private static IEnumerable<FluentLayout> MakeConstraints (UIView container)
        {
            UIView prev = null;

            foreach (var view in container.Subviews) {

                var startTopMargin = 50.0f;
                var topMargin = 0f;
                var horizMargin = 0f;

                if (view is UIButton) {
                    topMargin = 7f;
                    horizMargin = 15f;
                }

                if (prev == null) {
                    yield return view.AtTopOf (container, topMargin + startTopMargin);
                } else {
                    yield return view.Below (prev, topMargin);
                }

                yield return view.AtLeftOf (container, horizMargin);
                yield return view.AtRightOf (container, horizMargin + 20);

                prev = view;
            }
        }

        private void OnMenuButtonTouchUpInside (object sender, EventArgs e)
        {
            var main = window.RootViewController as MainViewController;
            if (sender == logButton) {
                ServiceContainer.Resolve<SettingsStore> ().PreferredStartView = "log";
                main.SetViewControllers (new[] {
                    new LogViewController ()
                }, true);
            } else if (sender == reportsButton) {
                main.PushViewController (new ReportsViewController (), true);
            } else if (sender == settingsButton) {
                main.PushViewController (new SettingsViewController (), true);
            } else if (sender == feedbackButton) {
                main.PushViewController (new FeedbackViewController (), true);
            } else {
                ServiceContainer.Resolve<AuthManager> ().Forget ();
            }
            main.CloseMenu();
        }
    }
}
