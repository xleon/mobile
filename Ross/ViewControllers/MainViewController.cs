using System;
using Foundation;
using UIKit;
using Toggl.Phoebe;
using Toggl.Phoebe.Net;
using CoreGraphics;
using Toggl.Ross.Theme;
using Toggl.Phoebe.Reactive;
using System.Reactive.Linq;
using Toggl.Phoebe.Data;

namespace Toggl.Ross.ViewControllers
{
    public class MainViewController : UINavigationController
    {
        private UIAlertView upgradeAlert;
        private UIView fadeView;

        private UITapGestureRecognizer tapGesture;
        private UIPanGestureRecognizer panGesture;
        private CGPoint draggingPoint;

        private const float menuSlideAnimationDuration = .3f;
        private const int menuOffset = 60;
        private const int velocityTreshold = 100;
        private LeftViewController menu;

        private nfloat Width { get { return View.Frame.Width; } }
        private nfloat CurrentX { get { return View.Frame.X; } }
        private nfloat MaxDraggingX { get { return Width - menuOffset; } }
        private nfloat MinDraggingX { get { return 0; } }
        private bool MenuOpen {  get { return 0 != CurrentX; }}
        private IDisposable stateObserver;

        // TODO: Because the gesture of some events
        // is the same to the gesture of open/close
        // main Menu, this flag could let external objects to
        // deactivate it. This behaviour will change soon.
        public bool MenuEnabled { get; set; }

        public override void ViewWillAppear(bool animated)
        {
            base.ViewWillAppear(animated);
            // ATTENTION Suscription to state (settings) changes inside
            // the view. This will be replaced for "router"
            // modified in the reducers.
            stateObserver = StoreManager.Singleton
                            .Observe(x => x.State.Settings.UserId)
                            .StartWith(StoreManager.Singleton.AppState.Settings.UserId)
                            .DistinctUntilChanged()
                            .Subscribe(userGuid => ResetRootViewController(userGuid));
        }

        public override void ViewDidAppear(bool animated)
        {
            base.ViewDidAppear(animated);

            Application.MarkLaunched();
            menu = new LeftViewController();
            View.Window.InsertSubview(menu.View, 0);
        }

        public override void ViewDidLoad()
        {
            base.ViewDidLoad();

            View.Apply(Style.Screen);
            NavigationBar.Apply(Style.NavigationBar);
            Delegate = new NavDelegate();

            panGesture = new UIPanGestureRecognizer(OnPanGesture)
            {
                // TODO: TableView scroll gestures are not
                // compatible with the open / close pan gesture.
                ShouldRecognizeSimultaneously = (a, b) => !(b.View is UITableView),
                CancelsTouchesInView = true,
            };
            View.AddGestureRecognizer(panGesture);

            fadeView = new UIView();
            fadeView.BackgroundColor = UIColor.FromRGBA(29f / 255f, 29f / 255f, 28f / 255f, 0.5f);
            fadeView.Frame = new CGRect(0, 0, View.Frame.Width, View.Frame.Height);
            fadeView.Hidden = true;

            tapGesture = new UITapGestureRecognizer(CloseMenu)
            {
                ShouldReceiveTouch = (a, b) => true,
                ShouldRecognizeSimultaneously = (a, b) => true,
                CancelsTouchesInView = true,
            };
            fadeView.AddGestureRecognizer(tapGesture);
            View.Add(fadeView);
        }

        public override void ViewWillDisappear(bool animated)
        {
            stateObserver.Dispose();
            base.ViewWillDisappear(animated);
        }

        private void OnPanGesture(UIPanGestureRecognizer recognizer)
        {
            if (!MenuEnabled)
            {
                return;
            }

            var translation = recognizer.TranslationInView(recognizer.View);
            var movement = translation.X - draggingPoint.X;

            switch (recognizer.State)
            {
                case UIGestureRecognizerState.Began:
                    draggingPoint = translation;
                    break;
                case UIGestureRecognizerState.Changed:
                    var newX = CurrentX;
                    newX += movement;
                    if (newX > MinDraggingX && newX < MaxDraggingX)
                    {
                        MoveToLocation(newX);
                    }
                    draggingPoint = translation;
                    break;
                case UIGestureRecognizerState.Ended:
                    if (Math.Abs(translation.X) >= velocityTreshold)
                    {
                        if (translation.X < 0)
                        {
                            CloseMenu();
                        }
                        else
                        {
                            OpenMenu();
                        }
                    }
                    else
                    {
                        if (Math.Abs(CurrentX) < (Width - menuOffset) / 2)
                        {
                            CloseMenu();
                        }
                        else
                        {
                            OpenMenu();
                        }
                    }
                    break;
            }
        }

        public void CloseMenu()
        {
            fadeView.Hidden = true;
            UIView.Animate(menuSlideAnimationDuration, 0, UIViewAnimationOptions.CurveEaseOut, () => MoveToLocation(0), null);
        }

        public void OpenMenu()
        {
            UIView.Animate(menuSlideAnimationDuration, 0, UIViewAnimationOptions.CurveEaseOut, () => MoveToLocation(Width - menuOffset), () =>
            {
                fadeView.Hidden = false;
            });
        }

        public void ToggleMenu()
        {
            if (MenuOpen)
            {
                CloseMenu();
            }
            else
            {
                OpenMenu();
            }
        }

        public void MoveToLocation(nfloat x)
        {
            var rect = View.Frame;
            rect.Y = 0;
            rect.X = x;
            View.Frame = rect;
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
        }

        private void OnTogglHttpResponse(TogglHttpResponseMessage msg)
        {
            // TODO Rx Activate update mechanism.
            if (msg.StatusCode == System.Net.HttpStatusCode.Gone)
            {
                if (upgradeAlert == null)
                {
                    upgradeAlert = new UIAlertView(
                        "MainUpdateNeededTitle".Tr(),
                        "MainUpdateNeededMessage".Tr(),
                        null, "MainUpdateNeededOk".Tr());
                    upgradeAlert.Clicked += (s, e) => UIApplication.SharedApplication.OpenUrl(new NSUrl(Build.AppStoreUrl));
                }
                upgradeAlert.Show();
            }
        }

        private void ResetRootViewController(Guid userGuid)
        {
            UIViewController vc = null;
            bool isUserLogged = userGuid != Guid.Empty;
            bool emptyStack = ViewControllers.Length < 1;

            if (isUserLogged && (emptyStack || ViewControllers [0] is WelcomeViewController))
            {
                // TODO Rx a Fullsync is triggered.
                // Evaluate if it is the best place or not.
                RxChain.Send(new ServerRequest.GetChanges());
                vc = new LogViewController();
                MenuEnabled = true;
            }
            else if (emptyStack || !(ViewControllers [0] is WelcomeViewController))
            {
                vc = new WelcomeViewController();
                MenuEnabled = false;
            }
            if (vc != null)
            {
                SetViewControllers(new [] { vc }, ViewControllers.Length > 0);
            }
        }

        private class NavDelegate : UINavigationControllerDelegate
        {
            public UIPercentDrivenInteractiveTransition InteractiveTransition { get; set; }

            public override IUIViewControllerAnimatedTransitioning GetAnimationControllerForOperation(UINavigationController navigationController, UINavigationControllerOperation operation, UIViewController fromViewController, UIViewController toViewController)
            {
                if (toViewController is DurationChangeViewController)
                {
                    var durationController = (DurationChangeViewController)toViewController;
                    durationController.PreviousControllerType = fromViewController.GetType();
                    return new DurationChangeViewController.PushAnimator();
                }
                if (fromViewController is DurationChangeViewController)
                {
                    var durationController = (DurationChangeViewController)fromViewController;
                    if (durationController.PreviousControllerType == toViewController.GetType())
                    {
                        return new DurationChangeViewController.PopAnimator();
                    }
                    durationController.PreviousControllerType = null;
                }
                return null;
            }

            public override IUIViewControllerInteractiveTransitioning GetInteractionControllerForAnimationController(UINavigationController navigationController, IUIViewControllerAnimatedTransitioning animationController)
            {
                return InteractiveTransition;
            }
        }
    }
}