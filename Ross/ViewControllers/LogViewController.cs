using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using MonoTouch.CoreAnimation;
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
    public class LogViewController : UITableViewController
    {
        public LogViewController () : base (UITableViewStyle.Plain)
        {
            // TODO: Sync manager should be invoked in a different place?
            var syncManager = ServiceContainer.Resolve<SyncManager> ();
            syncManager.Run (SyncMode.Auto);

            EdgesForExtendedLayout = UIRectEdge.None;
            new Source (TableView).Attach ();
            TableView.TableHeaderView = new TableViewHeaderView ();
        }

        public override void ViewDidLoad ()
        {
            base.ViewDidLoad ();

            Title = "Log";
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
                return 60f;
            }

            public override UITableViewCell GetCell (UITableView tableView, NSIndexPath indexPath)
            {
                var cell = (TimeEntryCell)tableView.DequeueReusableCell (EntryCellId, indexPath);
                cell.Rebind (GetRow (indexPath));
                return cell;
            }

            public override float EstimatedHeightForHeader (UITableView tableView, int section)
            {
                return 42f;
            }

            public override float GetHeightForHeader (UITableView tableView, int section)
            {
                return 42f;
            }

            public override UIView GetViewForHeader (UITableView tableView, int section)
            {
                var view = (SectionHeaderView)tableView.DequeueReusableHeaderFooterView (SectionHeaderId);
                view.Rebind (GetSection (section));
                return view;
            }
        }

        class TimeEntryCell : UITableViewCell
        {
            private const float HorizPadding = 15f;
            private readonly UIView textContentView;
            private readonly UILabel projectLabel;
            private readonly UILabel clientLabel;
            private readonly UILabel taskLabel;
            private readonly UILabel descriptionLabel;
            private readonly UIImageView taskSeparatorImageView;
            private readonly UIImageView billableTagsImageView;
            private readonly UILabel durationLabel;
            private readonly UIImageView runningImageView;

            public TimeEntryCell (IntPtr ptr) : base (ptr)
            {
                textContentView = new UIView ();
                projectLabel = new UILabel ().ApplyStyle (Style.Log.CellProjectLabel);
                clientLabel = new UILabel ().ApplyStyle (Style.Log.CellClientLabel);
                taskLabel = new UILabel ().ApplyStyle (Style.Log.CellTaskLabel);
                descriptionLabel = new UILabel ().ApplyStyle (Style.Log.CellDescriptionLabel);
                taskSeparatorImageView = new UIImageView ().ApplyStyle (Style.Log.CellTaskDescriptionSeparator);
                billableTagsImageView = new UIImageView ();
                durationLabel = new UILabel ().ApplyStyle (Style.Log.CellDurationLabel);
                runningImageView = new UIImageView ();

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

                ContentView.AddSubviews (
                    textContentView,
                    billableTagsImageView,
                    durationLabel,
                    runningImageView
                );
            }

            public override void LayoutSubviews ()
            {
                base.LayoutSubviews ();

                var contentFrame = ContentView.Frame;

                const float durationLabelWidth = 80f;
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

                const float runningHeight = 5f;
                const float runningWidth = 5f;
                runningImageView.Frame = new RectangleF (
                    y: (contentFrame.Height - runningHeight) / 2,
                    height: runningHeight,
                    x: contentFrame.Width - (HorizPadding - runningWidth) / 2,
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
                    y: (float)Math.Ceiling (projectLabel.Frame.Y + projectLabel.Font.Ascender - clientLabel.Font.Ascender),
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
                    offsetX += taskLabel.Frame.Width + 2f;

                    if (!taskSeparatorImageView.Hidden) {
                        var imageSize = taskSeparatorImageView.Image != null ? taskSeparatorImageView.Image.Size : SizeF.Empty;
                        taskSeparatorImageView.Frame = new RectangleF (
                            x: offsetX,
                            y: taskLabel.Frame.Y + taskLabel.Font.Ascender - imageSize.Height,
                            width: imageSize.Width,
                            height: imageSize.Height
                        );

                        offsetX += taskSeparatorImageView.Frame.Width + 2f;
                    }

                    if (!descriptionLabel.Hidden) {
                        bounds = GetBoundingRect (descriptionLabel);
                        descriptionLabel.Frame = new RectangleF (
                            x: offsetX,
                            y: (float)Math.Floor (taskLabel.Frame.Y + taskLabel.Font.Ascender - descriptionLabel.Font.Ascender),
                            width: bounds.Width,
                            height: bounds.Height
                        );

                        offsetX += descriptionLabel.Frame.Width + 2f;
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
                return ((NSString)(view.Text ?? String.Empty)).GetBoundingRect (
                    new SizeF (Single.MaxValue, Single.MaxValue),
                    NSStringDrawingOptions.UsesLineFragmentOrigin,
                    attrs, null);
            }

            public void Rebind (TimeEntryModel model)
            {
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
                    // TODO: Schedule rebind
                }

                LayoutIfNeeded ();
            }
        }

        class SectionHeaderView : UITableViewHeaderFooterView
        {
            private const float HorizSpacing = 15f;
            private readonly UILabel dateLabel;
            private readonly UILabel totalDurationLabel;

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

            public void Rebind (AllTimeEntriesView.DateGroup data)
            {
                dateLabel.Text = FormatDate (data.Date);

                var duration = TimeSpan.FromSeconds (data.Models.Sum (m => m.GetDuration ().TotalSeconds));
                totalDurationLabel.Text = FormatDuration (duration);
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
