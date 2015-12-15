using System;
using System.Collections.Generic;
using System.Linq;
using Cirrious.FluentLayouts.Touch;
using Foundation;
using GalaSoft.MvvmLight.Helpers;
using Toggl.Phoebe.Data.DataObjects;
using Toggl.Phoebe.Data.Models;
using Toggl.Phoebe.Data.ViewModels;
using Toggl.Ross.Theme;
using Toggl.Ross.Views;
using UIKit;

namespace Toggl.Ross.ViewControllers
{
    public class NewClientViewController : UIViewController
    {
        public Action<ClientData> ClientCreated { get; set; }
        protected CreateClientViewModel ViewModel {get; set;}
        protected TextField NameTextField { get; set; }
        private WorkspaceModel workspace;

        public NewClientViewController (WorkspaceModel workspace)
        {
            Title = "NewClientTitle".Tr ();
            this.workspace = workspace;
        }

        public override void ViewDidLoad ()
        {
            base.ViewDidLoad ();
            ViewModel = CreateClientViewModel.Init (workspace);
            this.SetBinding (() => ViewModel.ClientName, () => NameTextField.Text, BindingMode.TwoWay)
            .UpdateTargetTrigger ("EditingChanged");
        }

        public override void ViewDidAppear (bool animated)
        {
            base.ViewDidAppear (animated);
            NameTextField.BecomeFirstResponder ();
        }

        public override void LoadView ()
        {
            var view = new UIView ().Apply (Style.Screen);

            view.Add (NameTextField = new TextField {
                TranslatesAutoresizingMaskIntoConstraints = false,
                AttributedPlaceholder = new NSAttributedString (
                    "NewClientNameHint".Tr (),
                    foregroundColor: Color.Gray
                ),
                ShouldReturn = (tf) => tf.ResignFirstResponder (),
            } .Apply (Style.NewProject.NameField));

            NameTextField.EditingChanged += (sender, e) => ValidateClientName ();
            view.AddConstraints (VerticalLinearLayout (view));
            EdgesForExtendedLayout = UIRectEdge.None;
            View = view;

            NavigationItem.RightBarButtonItem = new UIBarButtonItem (
                "NewClientAdd".Tr (), UIBarButtonItemStyle.Plain, OnNavigationBarAddClicked)
            .Apply (Style.DisableNavLabelButton);
        }

        private async void OnNavigationBarAddClicked (object sender, EventArgs e)
        {
            var clientData = await ViewModel.SaveNewClient ();
            if (ClientCreated != null) {
                ClientCreated.Invoke (clientData);
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

        private void ValidateClientName ()
        {
            var valid = true;
            var name = NameTextField.Text;

            if (String.IsNullOrWhiteSpace (name)) {
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
