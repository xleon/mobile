using System;
using CoreGraphics;
using Foundation;
using GalaSoft.MvvmLight.Helpers;
using Toggl.Phoebe.Data.Models;
using Toggl.Phoebe.Reactive;
using Toggl.Phoebe.ViewModels;
using Toggl.Ross.Theme;
using UIKit;

namespace Toggl.Ross.ViewControllers
{
    public class ClientSelectionViewController : ObservableTableViewController<IClientData>
    {
        private ClientListVM viewModel;
        private readonly IOnClientSelectedHandler handler;
        private readonly Guid workspaceId;

        public ClientSelectionViewController (Guid workspaceId, IOnClientSelectedHandler handler) : base (UITableViewStyle.Plain)
        {
            this.handler = handler;
            this.workspaceId = workspaceId;
            Title = "ClientTitle".Tr ();
        }

        public override void ViewDidLoad ()
        {
            base.ViewDidLoad ();

            View.Apply (Style.Screen);
            EdgesForExtendedLayout = UIRectEdge.None;
            viewModel = new ClientListVM (StoreManager.Singleton.AppState, workspaceId);

            // Set ObservableTableViewController settings
            // ObservableTableViewController is a helper class
            // from Mvvm light package.

            TableView.RowHeight = 60f;
            TableView.SeparatorStyle = UITableViewCellSeparatorStyle.None;
            CreateCellDelegate = CreateClientCell;
            BindCellDelegate = BindCell;
            DataSource = viewModel.ClientDataCollection;

            // TODO: Keep previous version calling
            // a handler. Later it can be changed.
            PropertyChanged += (sender, e) => {
                if (e.PropertyName == SelectedItemPropertyName) {
                    handler.OnClientSelected (SelectedItem);
                }
            };

            NavigationItem.RightBarButtonItem = new UIBarButtonItem (UIBarButtonSystemItem.Add, OnAddBtnPressed);
        }

        private void OnAddBtnPressed (object sender, EventArgs e)
        {
            // Show create client screen
            var next = new NewClientViewController (workspaceId, handler);
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
                if (string.IsNullOrWhiteSpace (labelString)) {
                    nameLabel.Text = "ClientNoNameClient".Tr ();
                } else {
                    nameLabel.Text = labelString;
                }
            }
        }
    }
}
