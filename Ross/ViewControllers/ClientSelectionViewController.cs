using System;
using CoreGraphics;
using Foundation;
using GalaSoft.MvvmLight.Helpers;
using Toggl.Phoebe.Data.DataObjects;
using Toggl.Phoebe.Data.Models;
using Toggl.Phoebe.Data.ViewModels;
using Toggl.Ross.Theme;
using UIKit;

namespace Toggl.Ross.ViewControllers
{
    public class ClientSelectionViewController : ObservableTableViewController<ClientData>
    {
        private readonly WorkspaceModel workspace;
        private ClientListViewModel viewModel;

        public ClientSelectionViewController (WorkspaceModel workspace) : base (UITableViewStyle.Plain)
        {
            this.workspace = workspace;
            Title = "ClientTitle".Tr ();
        }

        public async override void ViewDidLoad ()
        {
            base.ViewDidLoad ();

            View.Apply (Style.Screen);
            EdgesForExtendedLayout = UIRectEdge.None;
            viewModel = await ClientListViewModel.Init (workspace.Id);

            // Set ObservableTableViewController settings
            // ObservableTableViewController is a helper class
            // from Mvvm light package.

            TableView.RowHeight = 60f;
            TableView.SeparatorStyle = UITableViewCellSeparatorStyle.None;
            CreateCellDelegate = CreateClientCell;
            BindCellDelegate = BindCell;
            DataSource = viewModel.ClientDataCollection;

            PropertyChanged += (sender, e) => {
                if (e.PropertyName == SelectedItemPropertyName && ClientSelected != null)

                    // TODO: Keep previous version calling
                    // a handler. Later it can be changed.
                {
                    ClientSelected.Invoke (SelectedItem);
                }
            };

            NavigationItem.RightBarButtonItem = new UIBarButtonItem (
                "ClientNewClient".Tr (), UIBarButtonItemStyle.Plain, OnNavigationBarAddClicked)
            .Apply (Style.NavLabelButton);
        }

        private void OnNavigationBarAddClicked (object sender, EventArgs e)
        {
            // Show create client screen
            var next = new NewClientViewController (workspace) {
                ClientCreated = ClientSelected,
            };
            NavigationController.PushViewController (next, true);
        }

        private UITableViewCell CreateClientCell (NSString cellIdentifier)
        {
            return new ClientCell (cellIdentifier);
        }

        private void BindCell (UITableViewCell cell, ClientData clientData, NSIndexPath path)
        {
            ((ClientCell)cell).Bind (clientData.Name);
        }

        public Action<ClientData> ClientSelected { get; set; }

        private class ClientCell : UITableViewCell
        {
            const float cellSpacing = 4f;
            private UILabel nameLabel;

            public ClientCell (NSString cellIdentifier) : base (UITableViewCellStyle.Default, cellIdentifier)
            {
                InitView ();
            }

            public ClientCell (IntPtr handle) : base (handle)
            {
                InitView ();
            }

            private void InitView ()
            {
                this.Apply (Style.Screen);
                ContentView.Add (nameLabel = new UILabel ().Apply (Style.ClientList.NameLabel));
                BackgroundView = new UIView ().Apply (Style.ClientList.RowBackground);
            }

            public override void LayoutSubviews ()
            {
                base.LayoutSubviews ();

                var contentFrame = new CGRect (0, cellSpacing / 2, Frame.Width, Frame.Height - cellSpacing);
                SelectedBackgroundView.Frame = BackgroundView.Frame = ContentView.Frame = contentFrame;

                contentFrame.X = 15f;
                contentFrame.Y = 0;
                contentFrame.Width -= 15f;

                nameLabel.Frame = contentFrame;
            }

            public void Bind (string labelString)
            {
                if (String.IsNullOrWhiteSpace (labelString)) {
                    nameLabel.Text = "ClientNoNameClient".Tr ();
                } else {
                    nameLabel.Text = labelString;
                }
            }
        }
    }
}
