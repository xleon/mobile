using System;
using System.Collections.Generic;
using System.Linq;
using Cirrious.FluentLayouts.Touch;
using Foundation;
using UIKit;
using Toggl.Ross.Theme;
using Toggl.Ross.Views;
using Toggl.Phoebe.Data.ViewModels;

namespace Toggl.Ross.ViewControllers
{
    public class NewTagViewController : UIViewController
    {
        private TextField nameTextField;
        private CreateTagViewModel viewModel { get; set; }
        private IOnTagSelectedHandler handler;

        public NewTagViewController (Guid workspaceId, IOnTagSelectedHandler handler)
        {
            Title = "NewTagTitle".Tr();
            viewModel = new CreateTagViewModel (workspaceId);
            this.handler = handler;
        }

        public override void LoadView()
        {
            NavigationItem.RightBarButtonItem = new UIBarButtonItem (
                "NewTagAdd".Tr(), UIBarButtonItemStyle.Plain, OnAddTag)
            .Apply (Style.NavLabelButton).Apply (Style.DisableNavLabelButton);
            NavigationItem.RightBarButtonItem.Enabled = false;

            var view = new UIView().Apply (Style.Screen);

            view.Add (nameTextField = new TextField {
                TranslatesAutoresizingMaskIntoConstraints = false,
                AttributedPlaceholder = new NSAttributedString (
                    "NewTagNameHint".Tr(),
                    foregroundColor: Color.Gray
                ),
                ShouldReturn = (tf) => tf.ResignFirstResponder(),
            } .Apply (Style.NewProject.NameField));
            nameTextField.EditingChanged += (sender, e) => ValidateTagName ();

            view.AddConstraints (VerticalLinearLayout (view));

            EdgesForExtendedLayout = UIRectEdge.None;
            View = view;
        }

        public override void ViewDidAppear (bool animated)
        {
            base.ViewDidAppear (animated);
            nameTextField.BecomeFirstResponder();
        }

        public override void ViewWillUnload ()
        {
            viewModel.Dispose ();
            base.ViewWillUnload();
        }

        private async void OnAddTag (object sender, EventArgs e)
        {
            var newTagData = await viewModel.SaveTagModel (nameTextField.Text);
            handler.OnCreateNewTag (newTagData);
        }

        private IEnumerable<FluentLayout> VerticalLinearLayout (UIView container)
        {
            UIView prev = null;

            var subviews = container.Subviews.Where (v => !v.Hidden).ToList();
            foreach (var v in subviews) {
                if (prev == null) {
                    yield return v.AtTopOf (container, 10f);
                } else {
                    yield return v.Below (prev, 5f);
                }
                yield return v.Height().EqualTo (60f).SetPriority (UILayoutPriority.DefaultLow);
                yield return v.Height().GreaterThanOrEqualTo (60f);
                yield return v.AtLeftOf (container);
                yield return v.AtRightOf (container);

                prev = v;
            }
        }

        private void ValidateTagName ()
        {
            var valid = true;
            var name = nameTextField.Text;

            if (string.IsNullOrWhiteSpace (name)) {
                valid = false;
            }

            if (valid) {
                NavigationItem.RightBarButtonItem.Apply (Style.NavLabelButton);
            } else {
                NavigationItem.RightBarButtonItem.Apply (Style.DisableNavLabelButton);
            }

            NavigationItem.RightBarButtonItem.Enabled = valid;
        }
    }
}
