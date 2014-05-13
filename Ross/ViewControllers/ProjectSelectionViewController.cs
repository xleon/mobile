using System;
using System.Collections.Generic;
using System.Linq;
using MonoTouch.Foundation;
using MonoTouch.UIKit;
using Toggl.Phoebe;
using Toggl.Phoebe.Data;
using Toggl.Phoebe.Data.Models;
using Toggl.Phoebe.Data.Views;
using XPlatUtils;
using Toggl.Ross.DataSources;
using Toggl.Ross.Views;

namespace Toggl.Ross.ViewControllers
{
    public class ProjectSelectionViewController : UITableViewController
    {
        public ProjectSelectionViewController (TimeEntryModel model) : base (UITableViewStyle.Grouped)
        {
            Title = "ProjectTitle".Tr ();

            EdgesForExtendedLayout = UIRectEdge.None;
            new Source (this).Attach ();
            TableView.TableHeaderView = new TableViewHeaderView ();
        }

        class Source : GroupedDataViewSource<object, ProjectAndTaskView.Workspace, object>
        {
            private readonly static NSString WorkspaceHeaderId = new NSString ("SectionHeaderId");
            private readonly static NSString ProjectCellId = new NSString ("ProjectCellId");
            private readonly static NSString TaskCellId = new NSString ("TaskCellId");
            private readonly ProjectSelectionViewController controller;
            private readonly ProjectAndTaskView dataView;
            private readonly HashSet<Guid> expandedProjects = new HashSet<Guid> ();

            public Source (ProjectSelectionViewController controller)
                : this (controller, new ProjectAndTaskView ())
            {
            }

            private Source (ProjectSelectionViewController controller, ProjectAndTaskView dataView)
                : base (controller.TableView, dataView)
            {
                this.controller = controller;
                this.dataView = dataView;

                controller.TableView.RegisterClassForHeaderFooterViewReuse (typeof(WorkspaceHeaderView), WorkspaceHeaderId);
                controller.TableView.RegisterClassForCellReuse (typeof(ProjectCell), ProjectCellId);
                controller.TableView.RegisterClassForCellReuse (typeof(TaskCell), TaskCellId);
            }

            private void SetTasksExpanded (Guid projectId, bool expand)
            {
                if (expand && expandedProjects.Add (projectId)) {
                    Update ();
                } else if (!expand && expandedProjects.Remove (projectId)) {
                    Update ();
                }
            }

            public override float EstimatedHeight (UITableView tableView, NSIndexPath indexPath)
            {
                return 60f;
            }

            public override float GetHeightForRow (UITableView tableView, NSIndexPath indexPath)
            {
                return EstimatedHeight (tableView, indexPath);
            }

            public override float EstimatedHeightForHeader (UITableView tableView, int section)
            {
                return 42f;
            }

            public override float GetHeightForHeader (UITableView tableView, int section)
            {
                return EstimatedHeightForHeader (tableView, section);
            }

            public override UITableViewCell GetCell (UITableView tableView, NSIndexPath indexPath)
            {
                var row = GetRow (indexPath);

                var project = row as ProjectAndTaskView.Project;
                if (project != null) {
                    var cell = (ProjectCell)tableView.DequeueReusableCell (ProjectCellId, indexPath);
                    cell.Bind (project);
                    return cell;
                }

                var task = row as TaskModel;
                if (task != null) {
                    var cell = (TaskCell)tableView.DequeueReusableCell (TaskCellId, indexPath);
                    cell.Bind (task);
                    return cell;
                }

                throw new InvalidOperationException (String.Format ("Unknown row type {0}", row.GetType ()));
            }

            public override UIView GetViewForHeader (UITableView tableView, int section)
            {
                var view = (WorkspaceHeaderView)tableView.DequeueReusableHeaderFooterView (WorkspaceHeaderId);
                view.Bind (GetSection (section).Model);
                return view;
            }

            public override bool CanEditRow (UITableView tableView, NSIndexPath indexPath)
            {
                return false;
            }

            public override void RowSelected (UITableView tableView, NSIndexPath indexPath)
            {
                var row = GetRow (indexPath);

                ProjectModel projectModel = null;
                var taskModel = row as TaskModel;
                if (taskModel != null) {
                    projectModel = taskModel.Project;
                } else {
                    var project = row as ProjectAndTaskView.Project;
                    if (project != null) {
                        // TODO: Set time entry project
                        if (project.IsNewProject) {
                            // TODO: Open project creation view controller
                            return;
                        } else if (!project.IsNoProject) {
                            projectModel = project.Model;
                        }
                    }
                }

                // TODO: Update model and return

                tableView.DeselectRow (indexPath, true);
            }

            protected override IEnumerable<ProjectAndTaskView.Workspace> GetSections ()
            {
                return dataView.Workspaces;
            }

            protected override IEnumerable<object> GetRows (ProjectAndTaskView.Workspace section)
            {
                foreach (var proj in section.Projects) {
                    yield return proj;

                    var isExpanded = proj.Model != null && proj.Model.Id != null && expandedProjects.Contains (proj.Model.Id.Value);
                    if (isExpanded) {
                        foreach (var task in proj.Tasks) {
                            yield return task;
                        }
                    }
                }
            }
        }

        private class ProjectCell : BindableTableViewCell<ProjectAndTaskView.Project>
        {
            private Subscription<ModelChangedMessage> subscriptionModelChanged;

            public ProjectCell (IntPtr handle) : base (handle)
            {
            }

            protected override void Dispose (bool disposing)
            {
                if (disposing) {
                    if (subscriptionModelChanged != null) {
                        var bus = ServiceContainer.Resolve<MessageBus> ();
                        bus.Unsubscribe (subscriptionModelChanged);
                        subscriptionModelChanged = null;
                    }
                }

                base.Dispose (disposing);
            }

            public override void WillMoveToSuperview (UIView newsuper)
            {
                base.WillMoveToSuperview (newsuper);

                if (newsuper != null) {
                    if (subscriptionModelChanged == null) {
                        var bus = ServiceContainer.Resolve<MessageBus> ();
                        subscriptionModelChanged = bus.Subscribe<ModelChangedMessage> ((msg) => {
                            if (Handle == IntPtr.Zero)
                                return;

                            OnModelChanged (msg);
                        });
                    }
                } else {
                    if (subscriptionModelChanged != null) {
                        var bus = ServiceContainer.Resolve<MessageBus> ();
                        bus.Unsubscribe (subscriptionModelChanged);
                        subscriptionModelChanged = null;
                    }
                }
            }

            protected void OnModelChanged (ModelChangedMessage msg)
            {
            }

            protected override void Rebind ()
            {
                throw new NotImplementedException ();
            }
        }

        private class TaskCell : ModelTableViewCell<TaskModel>
        {
            public TaskCell (IntPtr handle) : base (handle)
            {
            }

            protected override void Rebind ()
            {
                throw new NotImplementedException ();
            }

            protected override void OnModelChanged (ModelChangedMessage msg)
            {
                throw new NotImplementedException ();
            }
        }

        private class WorkspaceHeaderView : UITableViewHeaderFooterView
        {
            public WorkspaceHeaderView (IntPtr handle) : base (handle)
            {
            }

            public void Bind (WorkspaceModel model)
            {
            }
        }
    }
}
