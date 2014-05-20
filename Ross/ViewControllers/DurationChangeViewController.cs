using System;
using MonoTouch.UIKit;
using Toggl.Phoebe.Data;
using Toggl.Phoebe.Data.Models;
using Toggl.Ross.Theme;
using Toggl.Ross.Views;

namespace Toggl.Ross.ViewControllers
{
    public class DurationChangeViewController : UIViewController
    {
        private readonly TimeEntryModel model;
        private DurationView durationView;
        private UIBarButtonItem barButtonItem;

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
            durationView.DurationChanged += (s, e) => Rebind ();
            durationView.SizeToFit ();

            barButtonItem = new UIBarButtonItem (
                model == null ? "DurationAdd".Tr () : "DurationSet".Tr (),
                UIBarButtonItemStyle.Plain,
                OnNavigationBarRightClicked
            ).Apply (Style.NavLabelButton);

            Rebind ();
        }

        private void Rebind ()
        {
            var isValid = durationView.EnteredDuration != Duration.Zero && durationView.EnteredDuration.IsValid;
            NavigationItem.RightBarButtonItem = isValid ? barButtonItem : null;
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
            var duration = TimeSpan.Zero;

            var entered = durationView.EnteredDuration;
            if (model == null || model.State == TimeEntryState.New) {
                duration = new TimeSpan (entered.Hours, entered.Minutes, 0);
            } else {
                duration = model.GetDuration ();
                // Keep the current seconds and milliseconds
                duration = new TimeSpan (0, entered.Hours, entered.Minutes, duration.Seconds, duration.Milliseconds);
            }

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
