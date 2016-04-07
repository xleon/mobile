using System;
using System.Collections.Generic;
using System.Linq;
using Cirrious.FluentLayouts.Touch;
using Foundation;
using Toggl.Phoebe.Reactive;
using Toggl.Phoebe.ViewModels;
using Toggl.Ross.Theme;
using Toggl.Ross.Views;
using UIKit;

namespace Toggl.Ross.ViewControllers
{
    public class NewClientViewController : UIViewController
    {
        private NewClientVM ViewModel {get; set;}
        private TextField NameTextField { get; set; }
        private Guid workspaceId;
        private IOnClientSelectedHandler handler;

        public NewClientViewController(Guid workspaceId, IOnClientSelectedHandler handler)
        {
            Title = "NewClientTitle".Tr();
            this.workspaceId = workspaceId;
            this.handler = handler;
        }

        public override void LoadView()
        {
            var view = new UIView().Apply(Style.Screen);

            view.Add(NameTextField = new TextField
            {
                TranslatesAutoresizingMaskIntoConstraints = false,
                AttributedPlaceholder = new NSAttributedString(
                    "NewClientNameHint".Tr(),
                    foregroundColor: Color.Gray
                ),
                ShouldReturn = (tf) => tf.ResignFirstResponder(),
            } .Apply(Style.NewProject.NameField));

            NameTextField.EditingChanged += (sender, e) => ValidateClientName();
            view.AddConstraints(VerticalLinearLayout(view));
            EdgesForExtendedLayout = UIRectEdge.None;
            View = view;

            NavigationItem.RightBarButtonItem = new UIBarButtonItem(
                "NewClientAdd".Tr(), UIBarButtonItemStyle.Plain, OnNavigationBarAddClicked)
            .Apply(Style.DisableNavLabelButton);
            NavigationItem.RightBarButtonItem.Enabled = false;
        }

        public override void ViewDidAppear(bool animated)
        {
            base.ViewDidAppear(animated);
            NameTextField.BecomeFirstResponder();
        }

        public override void ViewWillAppear(bool animated)
        {
            base.ViewWillAppear(animated);
            ViewModel = new NewClientVM(StoreManager.Singleton.AppState, workspaceId);
        }

        public override void ViewWillDisappear(bool animated)
        {
            ViewModel.Dispose();
            base.ViewWillDisappear(animated);
        }

        private async void OnNavigationBarAddClicked(object sender, EventArgs e)
        {
            var clientData = await ViewModel.SaveClientAsync(NameTextField.Text);
            handler.OnClientSelected(clientData);
        }

        private IEnumerable<FluentLayout> VerticalLinearLayout(UIView container)
        {
            UIView prev = null;

            var subviews = container.Subviews.Where(v => !v.Hidden).ToList();
            foreach (var v in subviews)
            {
                if (prev == null)
                {
                    yield return v.AtTopOf(container, 10f);
                }
                else
                {
                    yield return v.Below(prev, 5f);
                }
                yield return v.Height().EqualTo(60f).SetPriority(UILayoutPriority.DefaultLow);
                yield return v.Height().GreaterThanOrEqualTo(60f);
                yield return v.AtLeftOf(container);
                yield return v.AtRightOf(container);

                prev = v;
            }
        }

        private void ValidateClientName()
        {
            var valid = true;
            var name = NameTextField.Text;

            if (string.IsNullOrWhiteSpace(name))
            {
                valid = false;
            }

            if (valid)
            {
                NavigationItem.RightBarButtonItem.Apply(Style.NavLabelButton);
            }
            else
            {
                NavigationItem.RightBarButtonItem.Apply(Style.DisableNavLabelButton);
            }

            NavigationItem.RightBarButtonItem.Enabled = valid;
        }
    }
}
