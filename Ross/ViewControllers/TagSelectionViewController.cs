using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using GoogleAnalytics.iOS;
using MonoTouch.Foundation;
using MonoTouch.UIKit;
using Toggl.Phoebe.Data.Models;
using Toggl.Phoebe.Data.Views;
using XPlatUtils;
using Toggl.Ross.DataSources;
using Toggl.Ross.Theme;

namespace Toggl.Ross.ViewControllers
{
    public class TagSelectionViewController : UITableViewController
    {
        private const float CellSpacing = 4f;
        private readonly TimeEntryModel model;
        private Source source;

        public TagSelectionViewController (TimeEntryModel model) : base (UITableViewStyle.Plain)
        {
            this.model = model;

            Title = "TagTitle".Tr ();
        }

        public override void ViewDidLoad ()
        {
            base.ViewDidLoad ();

            View.Apply (Style.Screen);
            EdgesForExtendedLayout = UIRectEdge.None;
            source = new Source (this);
            source.Attach ();

            NavigationItem.RightBarButtonItem = new UIBarButtonItem (
                "TagSet".Tr (), UIBarButtonItemStyle.Plain, OnNavigationBarSetClicked)
                .Apply (Style.NavLabelButton);
        }

        public override void ViewDidAppear (bool animated)
        {
            base.ViewDidAppear (animated);

            var tracker = ServiceContainer.Resolve<IGAITracker> ();
            tracker.Set (GAIConstants.ScreenName, "Tag Selection View");
            tracker.Send (GAIDictionaryBuilder.CreateAppView ().Build ());
        }

        private void OnNavigationBarSetClicked (object s, EventArgs e)
        {
            // Find tags to remove and which to add, don't touch ones which weren't changed.
            var toRemove = new List<TimeEntryTagModel> ();
            var toAdd = new List<TagModel> (source.SelectedTags);

            foreach (var m in model.Tags) {
                var tag = m.To;
                if (tag == null) {
                    continue;
                } else if (!toAdd.Remove (tag)) {
                    toRemove.Add (m);
                }
            }

            foreach (var m in toRemove) {
                model.Tags.Remove (m);
            }
            foreach (var tag in toAdd) {
                model.Tags.Add (tag);
            }

            NavigationController.PopViewControllerAnimated (true);
        }

        private class Source : PlainDataViewSource<TagModel>
        {
            private readonly static NSString TagCellId = new NSString ("EntryCellId");
            private readonly TagSelectionViewController controller;
            private readonly HashSet<TagModel> selectedTags;

            public Source (TagSelectionViewController controller)
                : base (controller.TableView, new WorkspaceTagsView (controller.model.WorkspaceId.Value))
            {
                this.controller = controller;
                this.selectedTags = new HashSet<TagModel> (controller.model.Tags.Select (m => m.To).Where (m => m != null));
            }

            public override void Attach ()
            {
                base.Attach ();

                controller.TableView.RegisterClassForCellReuse (typeof(TagCell), TagCellId);
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
                var cell = (TagCell)tableView.DequeueReusableCell (TagCellId, indexPath);
                cell.Bind (GetRow (indexPath));
                cell.Checked = selectedTags.Contains (cell.Model);
                return cell;
            }

            public override void RowSelected (UITableView tableView, NSIndexPath indexPath)
            {
                var cell = tableView.CellAt (indexPath) as TagCell;
                if (cell != null) {
                    if (selectedTags.Remove (cell.Model)) {
                        cell.Checked = false;
                    } else if (selectedTags.Add (cell.Model)) {
                        cell.Checked = true;
                    }
                }

                tableView.DeselectRow (indexPath, false);
            }

            public IEnumerable<TagModel> SelectedTags {
                get { return selectedTags; }
            }
        }

        private class TagCell : UITableViewCell
        {
            private readonly UILabel nameLabel;
            private TagModel model;

            public TagCell (IntPtr handle) : base (handle)
            {
                this.Apply (Style.Screen);
                ContentView.Add (nameLabel = new UILabel ().Apply (Style.TagList.NameLabel));
                BackgroundView = new UIView ().Apply (Style.TagList.RowBackground);
            }

            public override void LayoutSubviews ()
            {
                base.LayoutSubviews ();

                var contentFrame = new RectangleF (0, CellSpacing / 2, Frame.Width, Frame.Height - CellSpacing);
                SelectedBackgroundView.Frame = BackgroundView.Frame = ContentView.Frame = contentFrame;

                contentFrame.X = 15f;
                contentFrame.Y = 0;
                contentFrame.Width -= 15f;

                if (Checked) {
                    // Adjust for the checkbox accessory
                    contentFrame.Width -= 40f;
                }

                nameLabel.Frame = contentFrame;
            }

            public void Bind (TagModel model)
            {
                this.model = model;

                if (String.IsNullOrWhiteSpace (model.Name)) {
                    nameLabel.Text = "TagNoNameTag".Tr ();
                } else {
                    nameLabel.Text = model.Name;
                }
            }

            public bool Checked {
                get { return Accessory == UITableViewCellAccessory.Checkmark; }
                set {
                    Accessory = value ? UITableViewCellAccessory.Checkmark : UITableViewCellAccessory.None;
                    SetNeedsLayout ();
                }
            }

            public TagModel Model {
                get { return model; }
            }
        }
    }
}

