using System;
using MonoTouch.UIKit;
using Toggl.Phoebe.Data.Models;
using Toggl.Ross.Theme;
using Toggl.Ross.Views;

namespace Toggl.Ross.ViewControllers
{
    public class DurationChangeViewController : UIViewController
    {
        private readonly TimeEntryModel model;
        private DurationView durationView;

        public DurationChangeViewController (TimeEntryModel model)
        {
            this.model = model;
        }

        public override void LoadView ()
        {
            View = new UIImageView () {
                BackgroundColor = Color.Black,
            };
        }

        public override void ViewDidLoad ()
        {
            base.ViewDidLoad ();

            // Configure navigation item
            NavigationItem.TitleView = durationView = new DurationView () {
                Hint = model != null ? model.GetDuration () : TimeSpan.Zero,
            }.Apply (Style.DurationEdit.DurationView);
            durationView.SizeToFit ();

            NavigationItem.RightBarButtonItem = new UIBarButtonItem (
                model == null ? "DurationAdd".Tr () : "DurationSet".Tr (),
                UIBarButtonItemStyle.Plain,
                OnNavigationBarRightClicked
            ).Apply (Style.NavLabelButton);
        }

        public override void ViewDidAppear (bool animated)
        {
            base.ViewDidAppear (animated);
            durationView.BecomeFirstResponder ();
        }

        public override void ViewWillDisappear (bool animated)
        {
            base.ViewWillDisappear (animated);
            durationView.ResignFirstResponder ();
        }

        private void OnNavigationBarRightClicked (object sender, EventArgs args)
        {
            // TODO: Check that the user has changed the duration

            var duration = TimeSpan.Zero; // TODO

            if (model == null) {
                var m = TimeEntryModel.CreateFinished (duration);
                var controller = new EditTimeEntryViewController (m);

                // Replace self with edit controller on the stack
                var vcs = NavigationController.ViewControllers;
                vcs [vcs.Length - 1] = controller;
                NavigationController.SetViewControllers (vcs, true);
            } else {
                model.SetDuration (duration);
                NavigationController.PopViewControllerAnimated (true);
            }
        }
    }
}
