using System;
using CoreGraphics;
using CoreAnimation;
using Foundation;
using UIKit;
using Toggl.Phoebe.Data.DataObjects;
using Toggl.Phoebe.Data.Models;
using Toggl.Phoebe.Data.Views;
using Toggl.Ross.DataSources;
using Toggl.Ross.Theme;
using Toggl.Phoebe.Data.ViewModels;

namespace Toggl.Ross.ViewControllers
{
    public class ProjectSelectionViewController : UITableViewController
    {
        public interface IProjectSelected
        {
            void OnProjectSelected (Guid projectId, Guid taskId);
        }

        private readonly static NSString ClientHeaderId = new NSString ("ClientHeaderId");
        private readonly static NSString ProjectCellId = new NSString ("ProjectCellId");
        private readonly static NSString TaskCellId = new NSString ("TaskCellId");

        private const float CellSpacing = 4f;
        private Guid workspaceId;
        private ProjectListViewModel viewModel;
        private readonly IProjectSelected handler;

        public ProjectSelectionViewController (Guid workspaceId, IProjectSelected handler) : base (UITableViewStyle.Plain)
        {
            Title = "ProjectTitle".Tr ();
            this.workspaceId = workspaceId;
            this.handler = handler;
        }

        public async override void ViewDidLoad ()
        {
            base.ViewDidLoad ();

            View.Apply (Style.Screen);
            EdgesForExtendedLayout = UIRectEdge.None;

            TableView.RegisterClassForHeaderFooterViewReuse (typeof (SectionHeaderView), ClientHeaderId);
            TableView.RegisterClassForCellReuse (typeof (ProjectCell), ProjectCellId);
            TableView.RegisterClassForCellReuse (typeof (TaskCell), TaskCellId);
            TableView.SeparatorStyle = UITableViewCellSeparatorStyle.None;

            viewModel = await ProjectListViewModel.Init (workspaceId);
            TableView.Source = new Source (this, viewModel);
        }

        public override void ViewWillUnload()
        {
            viewModel.Dispose ();
            base.ViewWillUnload();
        }

        protected void OnItemSelected (CommonData m)
        {
            Guid projectId = Guid.Empty;
            Guid taskId = Guid.Empty;

            if (m is ProjectData) {
                if (! ((ProjectsCollection.SuperProjectData)m).IsEmpty) {
                    projectId = m.Id;
                }
            } else if (m is TaskData) {
                var task = (TaskData)m;
                projectId = task.ProjectId;
                taskId = task.Id;
            }

            handler.OnProjectSelected (projectId, taskId);
            NavigationController.PopViewController (true);
        }

        class Source : ObservableCollectionViewSource<CommonData, ClientData, ProjectData>
        {
            private readonly ProjectSelectionViewController owner;
            private readonly ProjectListViewModel viewModel;

            public Source (ProjectSelectionViewController owner, ProjectListViewModel viewModel)  : base (owner.TableView, viewModel.ProjectList)
            {
                this.owner = owner;
                this.viewModel = viewModel;
            }

            public override UITableViewCell GetCell (UITableView tableView, NSIndexPath indexPath)
            {
                var index = GetPlainIndexFromRow (collection, indexPath);
                var data = collection [index];

                if (data is ProjectData) {
                    var cell = (ProjectCell)tableView.DequeueReusableCell (ProjectCellId, indexPath);
                    cell.Bind ((ProjectsCollection.SuperProjectData)data, viewModel.ProjectList.AddTasks);
                    return cell;
                } else {
                    var cell = (TaskCell)tableView.DequeueReusableCell (TaskCellId, indexPath);
                    cell.Bind ((TaskData)data);
                    return cell;
                }
            }

            public override UIView GetViewForHeader (UITableView tableView, nint section)
            {
                var index = GetPlainIndexFromSection (collection, section);
                var data = (ClientData)collection [index];

                var view = (SectionHeaderView)tableView.DequeueReusableHeaderFooterView (ClientHeaderId);
                view.Bind (data);
                return view;
            }

            public override nfloat GetHeightForHeader (UITableView tableView, nint section)
            {
                return EstimatedHeightForHeader (tableView, section);
            }

            public override nfloat EstimatedHeight (UITableView tableView, NSIndexPath indexPath)
            {
                return 60f;
            }

            public override nfloat EstimatedHeightForHeader (UITableView tableView, nint section)
            {
                return 42f;
            }

            public override bool CanEditRow (UITableView tableView, NSIndexPath indexPath)
            {
                return false;
            }

            public override void RowSelected (UITableView tableView, NSIndexPath indexPath)
            {
                var index = GetPlainIndexFromRow (collection, indexPath);
                var data = collection [index];
                owner.OnItemSelected (data);

                tableView.DeselectRow (indexPath, true);
            }
        }

        class ProjectCell : UITableViewCell
        {
            private UIView textContentView;
            private UILabel projectLabel;
            private UILabel clientLabel;
            private UIButton tasksButton;
            private ProjectsCollection.SuperProjectData projectData;
            private Action<ProjectData> onPressedTagBtn;

            public ProjectCell (IntPtr handle) : base (handle)
            {
                this.Apply (Style.Screen);
                BackgroundView = new UIView ();

                ContentView.Add (textContentView = new UIView ());
                ContentView.Add (tasksButton = new UIButton ().Apply (Style.ProjectList.TasksButtons));
                textContentView.Add (projectLabel = new UILabel ().Apply (Style.ProjectList.ProjectLabel));
                textContentView.Add (clientLabel = new UILabel ().Apply (Style.ProjectList.ClientLabel));

                var maskLayer = new CAGradientLayer () {
                    AnchorPoint = CGPoint.Empty,
                    StartPoint = new CGPoint (0.0f, 0.0f),
                    EndPoint = new CGPoint (1.0f, 0.0f),
                    Colors = new [] {
                        UIColor.FromWhiteAlpha (1, 1).CGColor,
                        UIColor.FromWhiteAlpha (1, 1).CGColor,
                        UIColor.FromWhiteAlpha (1, 0).CGColor,
                    },
                    Locations = new [] {
                        NSNumber.FromFloat (0f),
                        NSNumber.FromFloat (0.9f),
                        NSNumber.FromFloat (1f),
                    },
                };

                textContentView.Layer.Mask = maskLayer;
                tasksButton.TouchUpInside += OnTasksButtonTouchUpInside;
            }

            public override void LayoutSubviews ()
            {
                base.LayoutSubviews ();

                var contentFrame = new CGRect (0, CellSpacing / 2, Frame.Width, Frame.Height - CellSpacing);
                SelectedBackgroundView.Frame = BackgroundView.Frame = ContentView.Frame = contentFrame;

                if (!tasksButton.Hidden) {
                    var virtualWidth = contentFrame.Height;
                    var buttonWidth = tasksButton.CurrentBackgroundImage.Size.Width;
                    var extraPadding = (virtualWidth - buttonWidth) / 2f;
                    tasksButton.Frame = new CGRect (
                        contentFrame.Width - virtualWidth + extraPadding, extraPadding,
                        buttonWidth, buttonWidth);
                    contentFrame.Width -= virtualWidth;
                }

                contentFrame.X += 13f;
                contentFrame.Width -= 13f;
                textContentView.Frame = contentFrame;
                textContentView.Layer.Mask.Bounds = contentFrame;

                contentFrame = new CGRect (CGPoint.Empty, contentFrame.Size);

                if (clientLabel.Hidden) {
                    // Only display single item, so make it fill the whole text frame
                    var bounds = GetBoundingRect (projectLabel);
                    projectLabel.Frame = new CGRect (
                        x: 0,
                        y: (contentFrame.Height - bounds.Height + projectLabel.Font.Descender) / 2f,
                        width: contentFrame.Width,
                        height: bounds.Height
                    );
                } else {
                    // Carefully craft the layout
                    var bounds = GetBoundingRect (projectLabel);
                    projectLabel.Frame = new CGRect (
                        x: 0,
                        y: (contentFrame.Height - bounds.Height + projectLabel.Font.Descender) / 2f,
                        width: bounds.Width,
                        height: bounds.Height
                    );

                    const float clientLeftMargin = 7.5f;
                    bounds = GetBoundingRect (clientLabel);
                    clientLabel.Frame = new CGRect (
                        x: projectLabel.Frame.X + projectLabel.Frame.Width + clientLeftMargin,
                        y: (float)Math.Floor (projectLabel.Frame.Y + projectLabel.Font.Ascender - clientLabel.Font.Ascender),
                        width: bounds.Width,
                        height: bounds.Height
                    );
                }
            }

            public void Bind (ProjectsCollection.SuperProjectData projectData, Action<ProjectData> onPressedTagBtn, bool showClient = false)
            {
                this.projectData = projectData;
                this.onPressedTagBtn = onPressedTagBtn;

                if (projectData.IsEmpty) {
                    projectLabel.Text = "ProjectNoProject".Tr ();
                    clientLabel.Hidden = true;
                    tasksButton.Hidden = true;
                    BackgroundView.BackgroundColor = Color.Gray;
                    projectLabel.Apply (Style.ProjectList.NoProjectLabel);
                    return;
                }

                var color = UIColor.Clear.FromHex (ProjectModel.HexColors [projectData.Color % ProjectModel.HexColors.Length]);
                BackgroundView.BackgroundColor = color;

                projectLabel.Text = projectData.Name;
                clientLabel.Text = projectData.ClientName;
                clientLabel.Hidden = !showClient;
                tasksButton.Hidden = projectData.TaskNumber == 0;
                tasksButton.Selected = false;
                tasksButton.SetTitleColor (color, UIControlState.Normal);
                tasksButton.SetTitle (projectData.TaskNumber.ToString (), UIControlState.Normal);

                // Layout content.
                LayoutSubviews ();
            }

            private void OnTasksButtonTouchUpInside (object sender, EventArgs e)
            {
                if (onPressedTagBtn != null && projectData != null) {
                    onPressedTagBtn.Invoke (projectData);
                }
            }

            private static CGRect GetBoundingRect (UILabel view)
            {
                var attrs = new UIStringAttributes () {
                    Font = view.Font,
                };
                var rect = ((NSString) (view.Text ?? string.Empty)).GetBoundingRect (
                               new CGSize (float.MaxValue, float.MaxValue),
                               NSStringDrawingOptions.UsesLineFragmentOrigin,
                               attrs, null);
                rect.Height = (float)Math.Ceiling (rect.Height);
                return rect;
            }
        }

        class TaskCell : UITableViewCell
        {
            private readonly UILabel nameLabel;
            private readonly UIView separatorView;

            public TaskCell (IntPtr handle) : base (handle)
            {
                this.Apply (Style.Screen);
                ContentView.Add (nameLabel = new UILabel ().Apply (Style.ProjectList.TaskLabel));
                ContentView.Add (separatorView = new UIView ().Apply (Style.ProjectList.TaskSeparator));
                BackgroundView = new UIView ().Apply (Style.ProjectList.TaskBackground);
            }

            public override void LayoutSubviews ()
            {
                base.LayoutSubviews ();

                var contentFrame = new CGRect (0, 0, Frame.Width, Frame.Height);
                SelectedBackgroundView.Frame = BackgroundView.Frame = ContentView.Frame = contentFrame;

                // Add padding
                contentFrame.X = 15f;
                contentFrame.Y = 0;
                contentFrame.Width -= 15f;

                nameLabel.Frame = contentFrame;
                separatorView.Frame = new CGRect (
                    contentFrame.X, contentFrame.Y + contentFrame.Height - 1f,
                    contentFrame.Width, 1f);
            }

            public void Bind (TaskData data)
            {
                var taskName = data.Name;
                if (string.IsNullOrWhiteSpace (taskName)) {
                    taskName = "ProjectNoNameTask".Tr ();
                }
                nameLabel.Text = taskName;
            }
        }

        class SectionHeaderView : UITableViewHeaderFooterView
        {
            private const float HorizSpacing = 15f;
            private readonly UILabel nameLabel;

            public SectionHeaderView (IntPtr ptr) : base (ptr)
            {
                nameLabel = new UILabel ().Apply (Style.Log.HeaderDateLabel);
                ContentView.AddSubview (nameLabel);
                BackgroundView = new UIView ().Apply (Style.Log.HeaderBackgroundView);
            }

            public override void LayoutSubviews ()
            {
                base.LayoutSubviews ();
                var contentFrame = ContentView.Frame;

                nameLabel.Frame = new CGRect (
                    x: HorizSpacing,
                    y: 0,
                    width: (contentFrame.Width - 3 * HorizSpacing) / 2,
                    height: contentFrame.Height
                );
            }

            public void Bind (ClientData data)
            {
                nameLabel.Text = string.IsNullOrEmpty (data.Name) ? "ProjectNoClient".Tr () : data.Name;
            }
        }
    }
}
