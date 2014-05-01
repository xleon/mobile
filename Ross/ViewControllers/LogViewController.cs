using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using MonoTouch.CoreAnimation;
using MonoTouch.CoreFoundation;
using MonoTouch.Foundation;
using MonoTouch.UIKit;
using Toggl.Phoebe.Data;
using Toggl.Phoebe.Data.Models;
using Toggl.Phoebe.Data.Views;
using Toggl.Phoebe.Net;
using XPlatUtils;
using Toggl.Ross.DataSources;
using Toggl.Ross.Theme;
using Toggl.Ross.Views;

namespace Toggl.Ross.ViewControllers
{
    public class LogViewController : BaseTimerTableViewController
    {
        private readonly NavigationMenuController navMenuController;

        public LogViewController () : base (UITableViewStyle.Plain)
        {
            // TODO: Sync manager should be invoked in a different place?
            var syncManager = ServiceContainer.Resolve<SyncManager> ();
            syncManager.Run (SyncMode.Auto);

            navMenuController = new NavigationMenuController ();

            EdgesForExtendedLayout = UIRectEdge.None;
            new Source (TableView).Attach ();
            TableView.TableHeaderView = new TableViewHeaderView ();
        }

        public override void ViewDidLoad ()
        {
            base.ViewDidLoad ();

            navMenuController.Attach (this);
        }

        class Source : GroupedDataViewSource<object, AllTimeEntriesView.DateGroup, TimeEntryModel>
        {
            readonly static NSString EntryCellId = new NSString ("EntryCellId");
            readonly static NSString SectionHeaderId = new NSString ("SectionHeaderId");
            readonly AllTimeEntriesView dataView;

            public Source (UITableView tableView) : this (tableView, new AllTimeEntriesView ())
            {
            }

            private Source (UITableView tableView, AllTimeEntriesView dataView) : base (tableView, dataView)
            {
                this.dataView = dataView;

                tableView.RegisterClassForCellReuse (typeof(TimeEntryCell), EntryCellId);
                tableView.RegisterClassForHeaderFooterViewReuse (typeof(SectionHeaderView), SectionHeaderId);
            }

            protected override IEnumerable<AllTimeEntriesView.DateGroup> GetSections ()
            {
                return dataView.DateGroups;
            }

            protected override IEnumerable<TimeEntryModel> GetRows (AllTimeEntriesView.DateGroup section)
            {
                return section.Models;
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
                var cell = (TimeEntryCell)tableView.DequeueReusableCell (EntryCellId, indexPath);
                cell.Bind (GetRow (indexPath));
                return cell;
            }

            public override float EstimatedHeightForHeader (UITableView tableView, int section)
            {
                return 42f;
            }

            public override float GetHeightForHeader (UITableView tableView, int section)
            {
                return EstimatedHeightForHeader (tableView, section);
            }

            public override UIView GetViewForHeader (UITableView tableView, int section)
            {
                var view = (SectionHeaderView)tableView.DequeueReusableHeaderFooterView (SectionHeaderId);
                view.Bind (GetSection (section));
                return view;
            }

            public override bool CanEditRow (UITableView tableView, NSIndexPath indexPath)
            {
                return false;
            }

            public override void RowSelected (UITableView tableView, NSIndexPath indexPath)
            {
                // TODO: Navigate to edit instead
                tableView.DeselectRow (indexPath, true);
            }
        }

        class TimeEntryCell : ModelTableViewCell<TimeEntryModel>
        {
            private const float HorizPadding = 15f;
            private const float ContinueSwipeWidth = 90f;
            private const float DeleteSwipeWidth = 100f;
            private const float SnapDistance = 20f;
            private readonly UILabel continueActionLabel;
            private readonly UILabel confirmActionLabel;
            private readonly UILabel deleteActionLabel;
            private readonly UIView actualContentView;
            private readonly UIView textContentView;
            private readonly UILabel projectLabel;
            private readonly UILabel clientLabel;
            private readonly UILabel taskLabel;
            private readonly UILabel descriptionLabel;
            private readonly UIImageView taskSeparatorImageView;
            private readonly UIImageView billableTagsImageView;
            private readonly UILabel durationLabel;
            private readonly UIImageView runningImageView;
            private int rebindCounter;

            public TimeEntryCell (IntPtr ptr) : base (ptr)
            {
                continueActionLabel = new UILabel () {
                    Text = "LogCellContinue".Tr (),
                }.ApplyStyle (Style.Log.CellSwipeActionLabel);
                deleteActionLabel = new UILabel () {
                    Text = "LogCellDelete".Tr (),
                }.ApplyStyle (Style.Log.CellSwipeActionLabel);
                confirmActionLabel = new UILabel () {
                    Text = "LogCellConfirm".Tr (),
                }.ApplyStyle (Style.Log.CellSwipeActionLabel);
                actualContentView = new UIView ().ApplyStyle (Style.Log.CellContentView);
                textContentView = new UIView ();
                projectLabel = new UILabel ().ApplyStyle (Style.Log.CellProjectLabel);
                clientLabel = new UILabel ().ApplyStyle (Style.Log.CellClientLabel);
                taskLabel = new UILabel ().ApplyStyle (Style.Log.CellTaskLabel);
                descriptionLabel = new UILabel ().ApplyStyle (Style.Log.CellDescriptionLabel);
                taskSeparatorImageView = new UIImageView ().ApplyStyle (Style.Log.CellTaskDescriptionSeparator);
                billableTagsImageView = new UIImageView ();
                durationLabel = new UILabel ().ApplyStyle (Style.Log.CellDurationLabel);
                runningImageView = new UIImageView ().ApplyStyle (Style.Log.CellRunningIndicator);

                textContentView.AddSubviews (
                    projectLabel, clientLabel,
                    taskLabel, descriptionLabel,
                    taskSeparatorImageView
                );

                var maskLayer = new CAGradientLayer () {
                    AnchorPoint = PointF.Empty,
                    StartPoint = new PointF (0.0f, 0.0f),
                    EndPoint = new PointF (1.0f, 0.0f),
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

                actualContentView.AddSubviews (
                    textContentView,
                    billableTagsImageView,
                    durationLabel,
                    runningImageView
                );

                BackgroundView = new UIView ();
                SelectedBackgroundView = new UIView ().ApplyStyle (Style.CellSelectedBackground);
                ContentView.AddSubviews (
                    continueActionLabel,
                    deleteActionLabel,
                    confirmActionLabel,
                    actualContentView
                );

                actualContentView.AddGestureRecognizer (new UIPanGestureRecognizer (OnPanningGesture) {
                    ShouldRecognizeSimultaneously = (a, b) => !panLockInHorizDirection,
                });
            }

            private void OnContinue ()
            {
                if (DataSource == null)
                    return;
                DataSource.Continue ();
            }

            private void OnDelete ()
            {
                if (DataSource == null)
                    return;
                DataSource.Delete ();
            }

            enum PanLock
            {
                None,
                Left,
                Right
            }

            private PointF panStart;
            private float panDeltaX;
            private bool panLockInHorizDirection;
            private bool panDeleteConfirmed;
            private PanLock panLock;

            private void OnPanningGesture (UIPanGestureRecognizer gesture)
            {
                switch (gesture.State) {
                case UIGestureRecognizerState.Began:
                    panStart = gesture.TranslationInView (actualContentView);
                    panLockInHorizDirection = false;
                    panDeleteConfirmed = false;
                    panLock = PanLock.None;
                    break;
                case UIGestureRecognizerState.Changed:
                    var currentPoint = gesture.TranslationInView (actualContentView);
                    panDeltaX = panStart.X - currentPoint.X;

                    if (!panLockInHorizDirection) {
                        if (Math.Abs (panDeltaX) > 10) {
                            // User is swiping the cell, lock them into this direction
                            panLockInHorizDirection = true;
                        } else if (Math.Abs (panStart.Y - currentPoint.Y) > 10) {
                            // User is starting to move upwards, let them scroll
                            gesture.Enabled = false;
                        }
                    }

                    // Switch pan lock
                    var oldLock = panLock;
                    var leftWidth = ContinueSwipeWidth;
                    var rightWidth = DeleteSwipeWidth;

                    switch (panLock) {
                    case PanLock.None:
                        if (-panDeltaX >= leftWidth) {
                            panLock = PanLock.Left;
                        } else if (panDeltaX >= rightWidth) {
                            panLock = PanLock.Right;
                        }
                        // Reset delete confirmation when completely hiding the delete
                        if (panDeltaX <= 0) {
                            panDeleteConfirmed = false;
                        }
                        break;
                    case PanLock.Left:
                        if (-panDeltaX < leftWidth - SnapDistance) {
                            panLock = PanLock.None;
                        } else {
                            return;
                        }
                        break;
                    case PanLock.Right:
                        if (panDeltaX < rightWidth - SnapDistance) {
                            panLock = PanLock.None;
                        } else {
                            return;
                        }
                        break;
                    }

                    // Apply delta limits
                    switch (panLock) {
                    case PanLock.Left:
                        panDeltaX = -(leftWidth + SnapDistance);
                        break;
                    case PanLock.Right:
                        panDeltaX = rightWidth + SnapDistance;
                        break;
                    }

                    var shouldAnimate = oldLock != panLock;
                    if (shouldAnimate) {
                        UIView.Animate (0.1, 0,
                            UIViewAnimationOptions.CurveEaseOut,
                            LayoutActualContentView, null);
                    } else {
                        LayoutActualContentView ();
                    }

                    if (!panDeleteConfirmed && panLock == PanLock.Right) {
                        // Queue cross fade animation
                        UIView.Animate (0.6, 0.4,
                            UIViewAnimationOptions.CurveEaseInOut,
                            delegate {
                                confirmActionLabel.Alpha = 0;
                            },
                            delegate {
                                if (panLock != PanLock.Right)
                                    return;
                                panDeleteConfirmed = true;
                            });

                        UIView.Animate (0.4, 0.8,
                            UIViewAnimationOptions.CurveEaseInOut,
                            delegate {
                                deleteActionLabel.Alpha = 1;
                            }, null);
                    }

                    break;
                case UIGestureRecognizerState.Cancelled:
                case UIGestureRecognizerState.Ended:
                    if (!gesture.Enabled)
                        gesture.Enabled = true;
                    panLockInHorizDirection = false;
                    panDeltaX = 0;

                    var shouldContinue = panLock == PanLock.Left;
                    var shouldDelete = panLock == PanLock.Right && panDeleteConfirmed;

                    UIView.Animate (0.3, 0,
                        UIViewAnimationOptions.BeginFromCurrentState | UIViewAnimationOptions.CurveEaseInOut,
                        LayoutActualContentView,
                        delegate {
                            if (shouldContinue)
                                OnContinue ();
                            if (shouldDelete)
                                OnDelete ();
                        });
                    break;
                }
            }

            private void LayoutActualContentView ()
            {
                var frame = ContentView.Frame;
                frame.X -= panDeltaX;
                actualContentView.Frame = frame;

                if (panDeltaX < 0) {
                    BackgroundView.ApplyStyle (Style.Log.ContinueState);
                } else if (panDeltaX > 0) {
                    BackgroundView.ApplyStyle (Style.Log.DeleteState);
                } else {
                    BackgroundView.ApplyStyle (Style.Log.NoSwipeState);
                }

                switch (panLock) {
                case PanLock.None:
                    continueActionLabel.Alpha = Math.Min (1, Math.Max (0, -2 * panDeltaX / ContinueSwipeWidth - 1));
                    var delAlpha = Math.Min (1, Math.Max (0, 2 * panDeltaX / DeleteSwipeWidth - 1));
                    confirmActionLabel.Alpha = panDeleteConfirmed ? 0 : delAlpha;
                    deleteActionLabel.Alpha = panDeleteConfirmed ? delAlpha : 0;
                    break;
                case PanLock.Left:
                    continueActionLabel.Alpha = 1;
                    confirmActionLabel.Alpha = 0;
                    deleteActionLabel.Alpha = 0;
                    break;
                case PanLock.Right:
                    continueActionLabel.Alpha = 0;
                    confirmActionLabel.Alpha = panDeleteConfirmed ? 0 : 1;
                    deleteActionLabel.Alpha = panDeleteConfirmed ? 1 : 0;
                    break;
                }
            }

            public override void LayoutSubviews ()
            {
                base.LayoutSubviews ();

                var contentFrame = ContentView.Frame;

                LayoutActualContentView ();

                continueActionLabel.Frame = new RectangleF (
                    x: 0, y: 0,
                    height: contentFrame.Height,
                    width: ContinueSwipeWidth + SnapDistance
                );
                confirmActionLabel.Frame = deleteActionLabel.Frame = new RectangleF (
                    x: contentFrame.Width - DeleteSwipeWidth - SnapDistance,
                    y: 0,
                    height: contentFrame.Height,
                    width: DeleteSwipeWidth + SnapDistance
                );

                const float durationLabelWidth = 75f;
                durationLabel.Frame = new RectangleF (
                    x: contentFrame.Width - durationLabelWidth - HorizPadding,
                    y: 0,
                    width: durationLabelWidth,
                    height: contentFrame.Height
                );

                const float billableTagsHeight = 20f;
                const float billableTagsWidth = 20f;
                billableTagsImageView.Frame = new RectangleF (
                    y: (contentFrame.Height - billableTagsHeight) / 2,
                    height: billableTagsHeight,
                    x: durationLabel.Frame.X - billableTagsWidth,
                    width: billableTagsWidth
                );

                var runningHeight = runningImageView.Image.Size.Height;
                var runningWidth = runningImageView.Image.Size.Width;
                runningImageView.Frame = new RectangleF (
                    y: (contentFrame.Height - runningHeight) / 2,
                    height: runningHeight,
                    x: contentFrame.Width - (HorizPadding + runningWidth) / 2,
                    width: runningWidth
                );

                textContentView.Frame = new RectangleF (
                    x: 0, y: 0, 
                    width: billableTagsImageView.Frame.X - 2f,
                    height: contentFrame.Height
                );
                textContentView.Layer.Mask.Bounds = textContentView.Frame;

                var bounds = GetBoundingRect (projectLabel);
                projectLabel.Frame = new RectangleF (
                    x: HorizPadding,
                    y: contentFrame.Height / 2 - bounds.Height,
                    width: bounds.Width,
                    height: bounds.Height
                );

                const float clientLeftMargin = 7.5f;
                bounds = GetBoundingRect (clientLabel);
                clientLabel.Frame = new RectangleF (
                    x: projectLabel.Frame.X + projectLabel.Frame.Width + clientLeftMargin,
                    y: (float)Math.Floor (projectLabel.Frame.Y + projectLabel.Font.Ascender - clientLabel.Font.Ascender),
                    width: bounds.Width,
                    height: bounds.Height
                );

                const float secondLineTopMargin = 3f;
                var offsetX = HorizPadding + 1f;
                if (!taskLabel.Hidden) {
                    bounds = GetBoundingRect (taskLabel);
                    taskLabel.Frame = new RectangleF (
                        x: offsetX,
                        y: contentFrame.Height / 2 + secondLineTopMargin,
                        width: bounds.Width,
                        height: bounds.Height
                    );
                    offsetX += taskLabel.Frame.Width + 4f;

                    if (!taskSeparatorImageView.Hidden) {
                        const float separatorOffsetY = -2f;
                        var imageSize = taskSeparatorImageView.Image != null ? taskSeparatorImageView.Image.Size : SizeF.Empty;
                        taskSeparatorImageView.Frame = new RectangleF (
                            x: offsetX,
                            y: taskLabel.Frame.Y + taskLabel.Font.Ascender - imageSize.Height + separatorOffsetY,
                            width: imageSize.Width,
                            height: imageSize.Height
                        );

                        offsetX += taskSeparatorImageView.Frame.Width + 4f;
                    }

                    if (!descriptionLabel.Hidden) {
                        bounds = GetBoundingRect (descriptionLabel);
                        descriptionLabel.Frame = new RectangleF (
                            x: offsetX,
                            y: (float)Math.Floor (taskLabel.Frame.Y + taskLabel.Font.Ascender - descriptionLabel.Font.Ascender),
                            width: bounds.Width,
                            height: bounds.Height
                        );

                        offsetX += descriptionLabel.Frame.Width + 4f;
                    }
                } else if (!descriptionLabel.Hidden) {
                    bounds = GetBoundingRect (descriptionLabel);
                    descriptionLabel.Frame = new RectangleF (
                        x: offsetX,
                        y: contentFrame.Height / 2 + secondLineTopMargin,
                        width: bounds.Width,
                        height: bounds.Height
                    );
                }
            }

            private static RectangleF GetBoundingRect (UILabel view)
            {
                var attrs = new UIStringAttributes () {
                    Font = view.Font,
                };
                var rect = ((NSString)(view.Text ?? String.Empty)).GetBoundingRect (
                               new SizeF (Single.MaxValue, Single.MaxValue),
                               NSStringDrawingOptions.UsesLineFragmentOrigin,
                               attrs, null);
                rect.Height = (float)Math.Ceiling (rect.Height);
                return rect;
            }

            protected override void Rebind ()
            {
                if (DataSource == null)
                    return;

                rebindCounter++;

                var model = DataSource;
                var projectName = "LogCellNoProject".Tr ();
                var projectColor = Color.Gray;
                var clientName = String.Empty;

                if (model.Project != null) {
                    projectName = model.Project.Name;
                    projectColor = UIColor.Clear.FromHex (model.Project.GetHexColor ());

                    if (model.Project.Client != null) {
                        clientName = model.Project.Client.Name;
                    }
                }

                projectLabel.TextColor = projectColor;
                if (projectLabel.Text != projectName) {
                    projectLabel.Text = projectName;
                    SetNeedsLayout ();
                }
                if (clientLabel.Text != clientName) {
                    clientLabel.Text = clientName;
                    SetNeedsLayout ();
                }

                var taskName = model.Task != null ? model.Task.Name : String.Empty;
                var taskHidden = String.IsNullOrWhiteSpace (taskName);
                var description = model.Description;
                var descHidden = String.IsNullOrWhiteSpace (description);

                if (taskHidden && descHidden) {
                    description = "LogCellNoDescription".Tr ();
                    descHidden = false;
                }
                var taskDeskSepHidden = taskHidden || descHidden;

                if (taskLabel.Hidden != taskHidden || taskLabel.Text != taskName) {
                    taskLabel.Hidden = taskHidden;
                    taskLabel.Text = taskName;
                    SetNeedsLayout ();
                }
                if (descriptionLabel.Hidden != descHidden || descriptionLabel.Text != description) {
                    descriptionLabel.Hidden = descHidden;
                    descriptionLabel.Text = description;
                    SetNeedsLayout ();
                }
                if (taskSeparatorImageView.Hidden != taskDeskSepHidden) {
                    taskSeparatorImageView.Hidden = taskDeskSepHidden;
                    SetNeedsLayout ();
                }

                var hasTags = model.Tags.HasNonDefault;
                var isBillable = model.IsBillable;
                if (hasTags && isBillable) {
                    billableTagsImageView.ApplyStyle (Style.Log.BillableAndTaggedEntry);
                } else if (hasTags) {
                    billableTagsImageView.ApplyStyle (Style.Log.TaggedEntry);
                } else if (isBillable) {
                    billableTagsImageView.ApplyStyle (Style.Log.BillableEntry);
                } else {
                    billableTagsImageView.ApplyStyle (Style.Log.PlainEntry);
                }

                var duration = model.GetDuration ();
                durationLabel.Text = duration.ToString (@"h\:mm\:ss");

                runningImageView.Hidden = model.State != TimeEntryState.Running;

                if (model.State == TimeEntryState.Running) {
                    // Schedule rebind
                    var counter = rebindCounter;
                    DispatchQueue.MainQueue.DispatchAfter (
                        TimeSpan.FromMilliseconds (1000 - duration.Milliseconds),
                        delegate {
                            if (counter == rebindCounter) {
                                Rebind ();
                            }
                        });
                }

                LayoutIfNeeded ();
            }

            protected override void OnModelChanged (ModelChangedMessage msg)
            {
                if (DataSource == null)
                    return;

                if (DataSource == msg.Model) {
                    if (msg.PropertyName == TimeEntryModel.PropertyStartTime
                        || msg.PropertyName == TimeEntryModel.PropertyIsBillable
                        || msg.PropertyName == TimeEntryModel.PropertyState
                        || msg.PropertyName == TimeEntryModel.PropertyDescription
                        || msg.PropertyName == TimeEntryModel.PropertyProjectId
                        || msg.PropertyName == TimeEntryModel.PropertyTaskId)
                        Rebind ();
                } else if (DataSource.ProjectId.HasValue && DataSource.ProjectId == msg.Model.Id) {
                    if (msg.PropertyName == ProjectModel.PropertyName
                        || msg.PropertyName == ProjectModel.PropertyColor)
                        Rebind ();
                } else if (DataSource.ProjectId.HasValue && DataSource.Project != null
                           && DataSource.Project.ClientId.HasValue
                           && DataSource.Project.ClientId == msg.Model.Id) {
                    if (msg.PropertyName == ClientModel.PropertyName)
                        Rebind ();
                } else if (DataSource.TaskId.HasValue && DataSource.TaskId == msg.Model.Id) {
                    if (msg.PropertyName == TaskModel.PropertyName)
                        Rebind ();
                }
            }
        }

        class SectionHeaderView : UITableViewHeaderFooterView
        {
            private const float HorizSpacing = 15f;
            private readonly UILabel dateLabel;
            private readonly UILabel totalDurationLabel;
            private AllTimeEntriesView.DateGroup data;
            private int rebindCounter;

            public SectionHeaderView (IntPtr ptr) : base (ptr)
            {
                dateLabel = new UILabel ().ApplyStyle (Style.Log.HeaderDateLabel);
                ContentView.AddSubview (dateLabel);

                totalDurationLabel = new UILabel ().ApplyStyle (Style.Log.HeaderDurationLabel);
                ContentView.AddSubview (totalDurationLabel);

                BackgroundView = new UIView ().ApplyStyle (Style.Log.HeaderBackgroundView);
            }

            public override void LayoutSubviews ()
            {
                base.LayoutSubviews ();
                var contentFrame = ContentView.Frame;

                dateLabel.Frame = new RectangleF (
                    x: HorizSpacing,
                    y: 0,
                    width: (contentFrame.Width - 3 * HorizSpacing) / 2,
                    height: contentFrame.Height
                );

                totalDurationLabel.Frame = new RectangleF (
                    x: (contentFrame.Width - 3 * HorizSpacing) / 2 + 2 * HorizSpacing,
                    y: 0,
                    width: (contentFrame.Width - 3 * HorizSpacing) / 2,
                    height: contentFrame.Height
                );
            }

            public void Bind (AllTimeEntriesView.DateGroup data)
            {
                this.data = data;
                Rebind ();
            }

            private void Rebind ()
            {
                rebindCounter++;

                dateLabel.Text = FormatDate (data.Date);

                var duration = TimeSpan.FromSeconds (data.Models.Sum (m => m.GetDuration ().TotalSeconds));
                totalDurationLabel.Text = FormatDuration (duration);

                if (data.Models.Any (m => m.State == TimeEntryState.Running)) {
                    // Schedule rebind
                    var counter = rebindCounter;
                    DispatchQueue.MainQueue.DispatchAfter (
                        TimeSpan.FromMilliseconds (60000 - duration.Seconds * 1000 - duration.Milliseconds),
                        delegate {
                            if (counter == rebindCounter) {
                                Rebind ();
                            }
                        });
                }
            }

            private string FormatDate (DateTime date)
            {
                date = date.ToLocalTime ().Date;
                var today = DateTime.Now.Date;
                if (date.Date == today) {
                    return "LogHeaderDateToday".Tr ();
                }
                if (date.Date == today - TimeSpan.FromDays (1)) {
                    return "LogHeaderDateYesterday".Tr ();
                }
                return date.ToString ("MMMM d");
            }

            private string FormatDuration (TimeSpan duration)
            {
                if (duration.TotalHours >= 1f) {
                    return String.Format (
                        "LogHeaderDurationHoursMinutes".Tr (),
                        (int)duration.TotalHours,
                        duration.Minutes
                    );
                }
                return String.Format ("LogHeaderDurationMinutes".Tr (), duration.Minutes);
            }
        }
    }
}
