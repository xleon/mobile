using System;
using MonoTouch.UIKit;
using Toggl.Phoebe.Data.Models;
using Toggl.Ross.Views;
using Toggl.Ross.Theme;
using System.Collections.Generic;
using Cirrious.FluentLayouts.Touch;
using System.Linq;
using XPlatUtils;
using GoogleAnalytics.iOS;
using MonoTouch.Foundation;

namespace Toggl.Ross.ViewControllers
{
    public class NewClientViewController : UIViewController
    {
        private readonly ClientModel model;
        private TextField nameTextField;
        private UIButton clientButton;
        private bool shouldRebindOnAppear;
        private bool isSaving;

        public NewClientViewController (WorkspaceModel workspace)
        {
            this.model = new ClientModel () {
                Workspace = workspace,
                Name = "",

            };
            Title = "NewClientTitle".Tr ();
        }

        public Action<ClientModel> ClientCreated { get; set; }

        private void BindNameField (TextField v)
        {
            if (v.Text != model.Name) {
                v.Text = model.Name;
            }
        }

        private void Rebind ()
        {
            nameTextField.Apply (BindNameField);
        }

        public override void LoadView ()
        {
            var view = new UIView ().Apply (Style.Screen);

            view.Add (nameTextField = new TextField () {
                TranslatesAutoresizingMaskIntoConstraints = false,
                AttributedPlaceholder = new NSAttributedString (
                    "NewClientNameHint".Tr (),
                    foregroundColor: Color.Gray
                ),
                ShouldReturn = (tf) => tf.ResignFirstResponder (),
            }.Apply (Style.NewProject.NameField).Apply (BindNameField));
            nameTextField.EditingChanged += OnNameFieldEditingChanged;

            view.AddConstraints (VerticalLinearLayout (view));

            EdgesForExtendedLayout = UIRectEdge.None;
            View = view;

            NavigationItem.RightBarButtonItem = new UIBarButtonItem (
                "NewClientAdd".Tr (), UIBarButtonItemStyle.Plain, OnNavigationBarAddClicked)
                .Apply (Style.NavLabelButton);
        }

        private void OnNameFieldEditingChanged (object sender, EventArgs e)
        {
            model.Name = nameTextField.Text;
        }

        private async void OnNavigationBarAddClicked (object sender, EventArgs e)
        {
            if (String.IsNullOrWhiteSpace (model.Name)) {
                // TODO: Show error dialog?
                return;
            }

            if (isSaving)
                return;

            isSaving = true;
            try {
                // Create new project:
                await model.SaveAsync ();

                // Invoke callback hook
                var cb = ClientCreated;
                if (cb != null) {
                    cb (model);
                } else {
                    NavigationController.PopViewControllerAnimated (true);
                }
            } finally {
                isSaving = false;
            }
        }

        private IEnumerable<FluentLayout> VerticalLinearLayout (UIView container)
        {
            UIView prev = null;

            var subviews = container.Subviews.Where (v => !v.Hidden).ToList ();
            foreach (var v in subviews) {
                if (prev == null) {
                    yield return v.AtTopOf (container, 10f);
                } else {
                    yield return v.Below (prev, 5f);
                }
                yield return v.Height ().EqualTo (60f).SetPriority (UILayoutPriority.DefaultLow);
                yield return v.Height ().GreaterThanOrEqualTo (60f);
                yield return v.AtLeftOf (container);
                yield return v.AtRightOf (container);

                prev = v;
            }
        }

        public override void ViewWillAppear (bool animated)
        {
            base.ViewWillAppear (animated);

            if (shouldRebindOnAppear) {
                Rebind ();
            } else {
                shouldRebindOnAppear = true;
            }
        }

        public override void ViewDidAppear (bool animated)
        {
            base.ViewDidAppear (animated);
            nameTextField.BecomeFirstResponder ();

            var tracker = ServiceContainer.Resolve<IGAITracker> ();
            tracker.Set (GAIConstants.ScreenName, "New Client View");
            tracker.Send (GAIDictionaryBuilder.CreateAppView ().Build ());
        }

    }
}

