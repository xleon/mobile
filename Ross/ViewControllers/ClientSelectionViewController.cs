using System;
using System.Drawing;
using GoogleAnalytics.iOS;
using MonoTouch.Foundation;
using MonoTouch.UIKit;
using Toggl.Phoebe.Data;
using Toggl.Phoebe.Data.DataObjects;
using Toggl.Phoebe.Data.Models;
using Toggl.Phoebe.Data.Views;
using XPlatUtils;
using Toggl.Ross.DataSources;
using Toggl.Ross.Theme;

namespace Toggl.Ross.ViewControllers
{
    public class ClientSelectionViewController : UITableViewController
    {
        private const float CellSpacing = 4f;
        private readonly WorkspaceModel workspace;

        public ClientSelectionViewController (WorkspaceModel workspace) : base (UITableViewStyle.Plain)
        {
            this.workspace = workspace;
            Title = "ClientTitle".Tr ();
        }

        public override void ViewDidLoad ()
        {
            base.ViewDidLoad ();

            View.Apply (Style.Screen);
            EdgesForExtendedLayout = UIRectEdge.None;
            new Source (this).Attach ();

            NavigationItem.RightBarButtonItem = new UIBarButtonItem (
                "ClientNewClient".Tr (), UIBarButtonItemStyle.Plain, OnNavigationBarAddClicked)
            .Apply (Style.NavLabelButton);
        }

        public override void ViewDidAppear (bool animated)
        {
            base.ViewDidAppear (animated);

            var tracker = ServiceContainer.Resolve<IGAITracker> ();
            tracker.Set (GAIConstants.ScreenName, "Client Selection View");
            tracker.Send (GAIDictionaryBuilder.CreateAppView ().Build ());
        }

        private void OnNavigationBarAddClicked (object sender, EventArgs e)
        {
            // Show create client screen
            var next = new NewClientViewController (workspace) {
                ClientCreated = (c) => ClientSelected (c),
            };
            this.NavigationController.PushViewController (next, true);
        }

        public Action<ClientModel> ClientSelected { get; set; }

        private class Source : PlainDataViewSource<ClientData>
        {
            private readonly static NSString ClientCellId = new NSString ("ClientCellId");
            private readonly ClientSelectionViewController controller;

            public Source (ClientSelectionViewController controller)
            : base (controller.TableView, GetClientView (controller.workspace))
            {
                this.controller = controller;
            }

            private static IDataView<ClientData> GetClientView (WorkspaceModel model)
            {
                var workspaceId = model.Id;
                var dataStore = ServiceContainer.Resolve<IDataStore> ();
                var q = dataStore.Table<ClientData> ()
                        .Where (r => r.DeletedAt == null && r.WorkspaceId == workspaceId);
                return new DataQueryView<ClientData> (q, 50);
            }

            public override void Attach ()
            {
                base.Attach ();

                controller.TableView.RegisterClassForCellReuse (typeof (ClientCell), ClientCellId);
                controller.TableView.SeparatorStyle = UITableViewCellSeparatorStyle.None;
            }

            public override float EstimatedHeight (UITableView tableView, NSIndexPath indexPath)
            {
                return 60f;
            }

            public override float GetHeightForRow (UITableView tableView, NSIndexPath indexPath)
            {
                return EstimatedHeight (tableView, indexPath);
            }

            public override UITableViewCell GetCell (UITableView tableView, NSIndexPath indexPath)
            {
                var cell = (ClientCell)tableView.DequeueReusableCell (ClientCellId, indexPath);
                cell.Bind ((ClientModel)GetRow (indexPath));
                return cell;
            }

            public override void RowSelected (UITableView tableView, NSIndexPath indexPath)
            {
                var client = (ClientModel)GetRow (indexPath);
                var cb = controller.ClientSelected;
                if (client != null && cb != null) {
                    cb (client);
                }
            }
        }

        private class ClientCell : UITableViewCell
        {
            private readonly UILabel nameLabel;
            private ClientModel model;

            public ClientCell (IntPtr handle) : base (handle)
            {
                this.Apply (Style.Screen);
                ContentView.Add (nameLabel = new UILabel ().Apply (Style.ClientList.NameLabel));
                BackgroundView = new UIView ().Apply (Style.ClientList.RowBackground);
            }

            public override void LayoutSubviews ()
            {
                base.LayoutSubviews ();

                var contentFrame = new RectangleF (0, CellSpacing / 2, Frame.Width, Frame.Height - CellSpacing);
                SelectedBackgroundView.Frame = BackgroundView.Frame = ContentView.Frame = contentFrame;

                contentFrame.X = 15f;
                contentFrame.Y = 0;
                contentFrame.Width -= 15f;

                nameLabel.Frame = contentFrame;
            }

            public void Bind (ClientModel model)
            {
                this.model = model;

                if (String.IsNullOrWhiteSpace (model.Name)) {
                    nameLabel.Text = "ClientNoNameClient".Tr ();
                } else {
                    nameLabel.Text = model.Name;
                }
            }
        }
    }
}
