using System;
using System.Collections.Generic;
using System.Linq;
using Cirrious.FluentLayouts.Touch;
using GoogleAnalytics.iOS;
using MonoTouch.Foundation;
using MonoTouch.UIKit;
using Toggl.Phoebe.Data;
using Toggl.Phoebe.Data.Models;
using XPlatUtils;
using Toggl.Ross.Theme;
using Toggl.Ross.Views;

namespace Toggl.Ross.ViewControllers
{
    public class NewProjectViewController : UIViewController
    {
        private readonly ProjectModel model;
        private TextField nameTextField;
        private UIButton clientButton;
        private bool shouldRebindOnAppear;

        public NewProjectViewController (WorkspaceModel workspace, int color)
        {
            this.model = Model.Update (new ProjectModel () {
                Workspace = workspace,
                Color = color,
                IsActive = true,
            });
            Title = "NewProjectTitle".Tr ();
        }

        public Action<ProjectModel> ProjectCreated { get; set; }

        private void BindNameField (TextField v)
        {
            if (v.Text != model.Name) {
                v.Text = model.Name;
            }
        }

        private void BindClientButton (UIButton v)
        {
            if (model.Client == null) {
                v.Apply (Style.NewProject.NoClient);
                v.SetTitle ("NewProjectClientHint".Tr (), UIControlState.Normal);
            } else {
                var text = model.Client.Name;
                if (String.IsNullOrEmpty (text)) {
                    text = "NewProjectNoNameClient".Tr ();
                }

                v.Apply (Style.NewProject.WithClient);
                v.SetTitle (text, UIControlState.Normal);
            }
        }

        private void Rebind ()
        {
            nameTextField.Apply (BindNameField);
            clientButton.Apply (BindClientButton);
        }

        public override void LoadView ()
        {
            var view = new UIView ().Apply (Style.Screen);

            view.Add (nameTextField = new TextField () {
                TranslatesAutoresizingMaskIntoConstraints = false,
                AttributedPlaceholder = new NSAttributedString (
                    "NewProjectNameHint".Tr (),
                    foregroundColor: Color.Gray
                ),
                ShouldReturn = (tf) => tf.ResignFirstResponder (),
            }.Apply (Style.NewProject.NameField).Apply (BindNameField));
            nameTextField.EditingChanged += OnNameFieldEditingChanged;

            view.Add (clientButton = new UIButton () {
                TranslatesAutoresizingMaskIntoConstraints = false,
            }.Apply (Style.NewProject.ClientButton).Apply (BindClientButton));
            clientButton.TouchUpInside += OnClientButtonTouchUpInside;

            view.AddConstraints (VerticalLinearLayout (view));

            EdgesForExtendedLayout = UIRectEdge.None;
            View = view;

            NavigationItem.RightBarButtonItem = new UIBarButtonItem (
                "NewProjectAdd".Tr (), UIBarButtonItemStyle.Plain, OnNavigationBarAddClicked)
                .Apply (Style.NavLabelButton);
        }

        private void OnNameFieldEditingChanged (object sender, EventArgs e)
        {
            model.Name = nameTextField.Text;
        }

        private void OnClientButtonTouchUpInside (object sender, EventArgs e)
        {
            var controller = new ClientSelectionViewController (model.Workspace) {
                ClientSelected = (client) => {
                    model.Client = client;
                    NavigationController.PopToViewController (this, true);
                }
            };
            NavigationController.PushViewController (controller, true);
        }

        private void OnNavigationBarAddClicked (object sender, EventArgs e)
        {
            if (String.IsNullOrWhiteSpace (model.Name)) {
                // TODO: Show error dialog?
                return;
            }

            // Create new project:
            model.IsPersisted = true;

            // Invoke callback hook
            var cb = ProjectCreated;
            if (cb != null) {
                cb (model);
            } else {
                NavigationController.PopViewControllerAnimated (true);
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
            tracker.Set (GAIConstants.ScreenName, "New Project View");
            tracker.Send (GAIDictionaryBuilder.CreateAppView ().Build ());
        }
    }
}
