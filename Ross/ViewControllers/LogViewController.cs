using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CoreAnimation;
using CoreGraphics;
using Foundation;
using GalaSoft.MvvmLight.Helpers;
using Toggl.Phoebe;
using Toggl.Phoebe.Data;
using Toggl.Phoebe.Data.Models;
using Toggl.Phoebe.Data.Utils;
using Toggl.Phoebe.Data.ViewModels;
using Toggl.Ross.DataSources;
using Toggl.Ross.Theme;
using Toggl.Ross.Views;
using UIKit;


namespace Toggl.Ross.ViewControllers
{
    public class LogViewController : UITableViewController
    {
        private const string DefaultDurationText = " 00:00:00 ";
        readonly static NSString EntryCellId = new NSString ("EntryCellId");
        readonly static NSString SectionHeaderId = new NSString ("SectionHeaderId");

        private NavigationMenuController navMenuController;
        private UIView emptyView;
        private UIView obmEmptyView;
        private UIButton durationButton;
        private UIButton actionButton;
        private UIBarButtonItem navigationButton;
        private UIActivityIndicatorView defaultFooterView;

        private Binding<bool, bool> syncBinding, hasMoreBinding, hasErrorBinding;
        private Binding<ObservableCollection<IHolder>, ObservableCollection<IHolder>> collectionBinding;

        protected LogTimeEntriesViewModel ViewModel {get; set;}

        public LogViewController () : base (UITableViewStyle.Plain)
        {
            navMenuController = new NavigationMenuController ();
        }

        public async override void ViewDidLoad ()
        {
            base.ViewDidLoad ();

            EdgesForExtendedLayout = UIRectEdge.None;
            TableView.RegisterClassForCellReuse (typeof (TimeEntryCell), EntryCellId);
            TableView.RegisterClassForHeaderFooterViewReuse (typeof (SectionHeaderView), SectionHeaderId);

            emptyView = new SimpleEmptyView {
                Title = "LogEmptyTitle".Tr (),
                Message = "LogEmptyMessage".Tr (),
            };

            obmEmptyView = new OBMEmptyView () {
                Title = "LogOBMEmptyTitle".Tr (),
                Message = "LogOBMEmptyMessage".Tr (),
            };

            // Create view model
            ViewModel = LogTimeEntriesViewModel.Init ();

            var headerView = new TableViewRefreshView ();
            RefreshControl = headerView;
            headerView.AdaptToTableView (TableView);
            headerView.ValueChanged += (sender, e) => ViewModel.TriggerFullSync ();

            // Bindings
            syncBinding = this.SetBinding (() => ViewModel.IsAppSyncing).WhenSourceChanges (() => {
                if (!ViewModel.IsAppSyncing) {
                    headerView.EndRefreshing ();
                }
            });
            hasMoreBinding = this.SetBinding (() => ViewModel.HasMoreItems).WhenSourceChanges (SetFooterState);
            hasErrorBinding = this.SetBinding (() => ViewModel.HasLoadErrors).WhenSourceChanges (SetFooterState);
            collectionBinding = this.SetBinding (() => ViewModel.Collection).WhenSourceChanges (() => {
                TableView.Source = new TimeEntriesSource (TableView, ViewModel.Collection, OnScrollEnds);
            });

            // Setup top toolbar
            SetupToolbar ();
            navMenuController.Attach (this);

            // TODO: Review this line.
            // Get data to fill the list. For the moment,
            // until a screenloader is added to the screen
            // is better to load the items after create
            // the viewModel and show the loader from RecyclerView
            await ViewModel.LoadMore ();
        }

        protected override void Dispose (bool disposing)
        {
            if (disposing) {
                if (navMenuController != null) {
                    navMenuController.Detach ();
                    navMenuController = null;
                }
            }

            base.Dispose (disposing);
        }

        public override void ViewDidLayoutSubviews ()
        {
            base.ViewDidLayoutSubviews ();
            emptyView.Frame = new CGRect (25f, (View.Frame.Size.Height - 200f) / 2, View.Frame.Size.Width - 50f, 200f);
            obmEmptyView.Frame = new CGRect (25f, 15f, View.Frame.Size.Width - 50f, 200f);
        }

        private void SetupToolbar ()
        {
            // Lazyily create views
            if (durationButton == null) {
                durationButton = new UIButton ().Apply (Style.NavTimer.DurationButton);
                durationButton.SetTitle (DefaultDurationText, UIControlState.Normal); // Dummy content to use for sizing of the label
                durationButton.SizeToFit ();
                durationButton.TouchUpInside += OnDurationButtonTouchUpInside;
            }

            if (navigationButton == null) {
                actionButton = new UIButton ().Apply (Style.NavTimer.StartButton);
                actionButton.SizeToFit ();
                actionButton.TouchUpInside += OnActionButtonTouchUpInside;
                navigationButton = new UIBarButtonItem (actionButton);
            }

            // Attach views
            var navigationItem = NavigationItem;
            navigationItem.TitleView = durationButton;
            navigationItem.RightBarButtonItem = navigationButton;
        }

        private void OnDurationButtonTouchUpInside (object sender, EventArgs e)
        {
        }

        private async void OnActionButtonTouchUpInside (object sender, EventArgs e)
        {
            await ViewModel.StartStopTimeEntry ();
        }

        private async void OnScrollEnds ()
        {
            await ViewModel.LoadMore ();
        }

        private void SetFooterState ()
        {
            if (ViewModel.HasMoreItems && !ViewModel.HasLoadErrors) {
                if (defaultFooterView == null) {
                    defaultFooterView = new UIActivityIndicatorView (UIActivityIndicatorViewStyle.Gray);
                    defaultFooterView.Frame = new CGRect (0, 0, 50, 50);
                }
                TableView.TableFooterView = defaultFooterView;
                defaultFooterView.StartAnimating ();
            } else if (ViewModel.HasMoreItems && ViewModel.HasLoadErrors) {
                //loadState = RecyclerLoadState.Retry;
            } else if (!ViewModel.HasMoreItems && !ViewModel.HasLoadErrors) {
                if (OBMExperimentManager.IncludedInExperiment (OBMExperimentManager.HomeEmptyState) {
                TableView.TableFooterView = obmEmptyView;
            } else {
                TableView.TableFooterView = emptyView;
            }
        }
    }

    #region TableViewSource
    class TimeEntriesSource : ObservableCollectionViewSource<IHolder, DateHolder, ITimeEntryHolder>
    {
        private Action onScrollEndAction;
        private bool isLoading;

        public TimeEntriesSource (UITableView tableView, ObservableCollection<IHolder> data, Action onScrollEndAction) : base (tableView, data)
            {
                this.onScrollEndAction = onScrollEndAction;
            }

            public override UITableViewCell GetCell (UITableView tableView, NSIndexPath indexPath)
            {
                var cell = (TimeEntryCell)tableView.DequeueReusableCell (EntryCellId, indexPath);
                var holder = (ITimeEntryHolder)collection.ElementAt (GetPlainIndexFromRow (collection, indexPath));
                cell.Bind (holder);
                return cell;
            }

            public override UIView GetViewForHeader (UITableView tableView, nint section)
            {
                var view = (SectionHeaderView)tableView.DequeueReusableHeaderFooterView (SectionHeaderId);
                view.Bind (collection.OfType<DateHolder> ().ElementAt ((int)section));
                return view;
            }

            public override nfloat GetHeightForHeader (UITableView tableView, nint section)
            {
                return EstimatedHeightForHeader (tableView, section);
            }

            public override nfloat EstimatedHeightForHeader (UITableView tableView, nint section)
            {
                return 42f;
            }

            public override nfloat EstimatedHeight (UITableView tableView, NSIndexPath indexPath)
            {
                return 60f;
            }

            public override nfloat GetHeightForRow (UITableView tableView, NSIndexPath indexPath)
            {
                return EstimatedHeight (tableView, indexPath);
            }

            public override bool CanEditRow (UITableView tableView, NSIndexPath indexPath)
            {
                return false;
            }

            public override void Scrolled (UIScrollView scrollView)
            {
                var currentOffset = scrollView.ContentOffset.Y;
                var maximumOffset = scrollView.ContentSize.Height - scrollView.Frame.Height;

                if (isLoading) {
                    isLoading &= maximumOffset - currentOffset <= 200.0;
                }

                if (!isLoading && maximumOffset - currentOffset <= 200.0 && onScrollEndAction != null) {
                    onScrollEndAction.Invoke ();
                    isLoading = true;
                }
            }
        }
        #endregion

        #region Cells
        class TimeEntryCell : SwipableTimeEntryTableViewCell
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
                projectLabel = new UILabel ().Apply (Style.Log.CellProjectLabel);
                clientLabel = new UILabel ().Apply (Style.Log.CellClientLabel);
                taskLabel = new UILabel ().Apply (Style.Log.CellTaskLabel);
                descriptionLabel = new UILabel ().Apply (Style.Log.CellDescriptionLabel);
                taskSeparatorImageView = new UIImageView ().Apply (Style.Log.CellTaskDescriptionSeparator);
                billableTagsImageView = new UIImageView ();
                durationLabel = new UILabel ().Apply (Style.Log.CellDurationLabel);
                runningImageView = new UIImageView ().Apply (Style.Log.CellRunningIndicator);

                textContentView.AddSubviews (
                    projectLabel, clientLabel,
                    taskLabel, descriptionLabel,
                    taskSeparatorImageView
                );

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

                ActualContentView.AddSubviews (
                    textContentView,
                    billableTagsImageView,
                    durationLabel,
                    runningImageView
                );
            }

            public void Bind (ITimeEntryHolder dataSource)
            {
                var projectName = "LogCellNoProject".Tr ();
                var projectColor = Color.Gray;
                var clientName = string.Empty;
                var info = dataSource.Info;

                if (!string.IsNullOrWhiteSpace (info.ProjectData.Name)) {
                    projectName = info.ProjectData.Name;
                    projectColor = UIColor.Clear.FromHex (ProjectModel.HexColors [info.ProjectData.Color % ProjectModel.HexColors.Length]);

                    if (!string.IsNullOrWhiteSpace (info.ClientData.Name)) {
                        clientName = info.ClientData.Name;
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

                var taskName = info.TaskData.Name;
                var taskHidden = string.IsNullOrWhiteSpace (taskName);
                var description = info.Description;
                var descHidden = string.IsNullOrWhiteSpace (description);

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

                RebindTags (dataSource);
                RebindDuration (dataSource);
                LayoutIfNeeded ();
            }

            private void RebindDuration (ITimeEntryHolder dataSource)
            {
                if (dataSource == null) {
                    return;
                }

                var duration = dataSource.GetDuration ();
                durationLabel.Text = TimeEntryModel.GetFormattedDuration (duration);
                runningImageView.Hidden = dataSource.Data.State != TimeEntryState.Running;
            }

            private void RebindTags (ITimeEntryHolder dataSource)
            {
                var hasTags = dataSource.Info.NumberOfTags > 0;
                var isBillable = dataSource.Info.IsBillable;

                if (hasTags && isBillable) {
                    billableTagsImageView.Apply (Style.Log.BillableAndTaggedEntry);
                } else if (hasTags) {
                    billableTagsImageView.Apply (Style.Log.TaggedEntry);
                } else if (isBillable) {
                    billableTagsImageView.Apply (Style.Log.BillableEntry);
                } else {
                    billableTagsImageView.Apply (Style.Log.PlainEntry);
                }
            }

            public Action<TimeEntryModel> ContinueCallback { get; set; }

            public override void LayoutSubviews ()
            {
                base.LayoutSubviews ();

                var contentFrame = ContentView.Frame;

                const float durationLabelWidth = 80f;
                durationLabel.Frame = new CGRect (
                    x: contentFrame.Width - durationLabelWidth - HorizPadding,
                    y: 0,
                    width: durationLabelWidth,
                    height: contentFrame.Height
                );

                const float billableTagsHeight = 20f;
                const float billableTagsWidth = 20f;
                billableTagsImageView.Frame = new CGRect (
                    y: (contentFrame.Height - billableTagsHeight) / 2,
                    height: billableTagsHeight,
                    x: durationLabel.Frame.X - billableTagsWidth,
                    width: billableTagsWidth
                );

                var runningHeight = runningImageView.Image.Size.Height;
                var runningWidth = runningImageView.Image.Size.Width;
                runningImageView.Frame = new CGRect (
                    y: (contentFrame.Height - runningHeight) / 2,
                    height: runningHeight,
                    x: contentFrame.Width - (HorizPadding + runningWidth) / 2,
                    width: runningWidth
                );

                textContentView.Frame = new CGRect (
                    x: 0, y: 0,
                    width: billableTagsImageView.Frame.X - 2f,
                    height: contentFrame.Height
                );
                textContentView.Layer.Mask.Bounds = textContentView.Frame;

                var bounds = GetBoundingRect (projectLabel);
                projectLabel.Frame = new CGRect (
                    x: HorizPadding,
                    y: contentFrame.Height / 2 - bounds.Height,
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

                const float secondLineTopMargin = 3f;
                nfloat offsetX = HorizPadding + 1f;
                if (!taskLabel.Hidden) {
                    bounds = GetBoundingRect (taskLabel);
                    taskLabel.Frame = new CGRect (
                        x: offsetX,
                        y: contentFrame.Height / 2 + secondLineTopMargin,
                        width: bounds.Width,
                        height: bounds.Height
                    );
                    offsetX += taskLabel.Frame.Width + 4f;

                    if (!taskSeparatorImageView.Hidden) {
                        const float separatorOffsetY = -2f;
                        var imageSize = taskSeparatorImageView.Image != null ? taskSeparatorImageView.Image.Size : CGSize.Empty;
                        taskSeparatorImageView.Frame = new CGRect (
                            x: offsetX,
                            y: taskLabel.Frame.Y + taskLabel.Font.Ascender - imageSize.Height + separatorOffsetY,
                            width: imageSize.Width,
                            height: imageSize.Height
                        );

                        offsetX += taskSeparatorImageView.Frame.Width + 4f;
                    }

                    if (!descriptionLabel.Hidden) {
                        bounds = GetBoundingRect (descriptionLabel);
                        descriptionLabel.Frame = new CGRect (
                            x: offsetX,
                            y: (float)Math.Floor (taskLabel.Frame.Y + taskLabel.Font.Ascender - descriptionLabel.Font.Ascender),
                            width: bounds.Width,
                            height: bounds.Height
                        );

                        offsetX += descriptionLabel.Frame.Width + 4f;
                    }
                } else if (!descriptionLabel.Hidden) {
                    bounds = GetBoundingRect (descriptionLabel);
                    descriptionLabel.Frame = new CGRect (
                        x: offsetX,
                        y: contentFrame.Height / 2 + secondLineTopMargin,
                        width: bounds.Width,
                        height: bounds.Height
                    );
                }
            }

            private static CGRect GetBoundingRect (UILabel view)
            {
                var attrs = new UIStringAttributes {
                    Font = view.Font,
                };
                var rect = ((NSString) (view.Text ?? string.Empty)).GetBoundingRect (
                               new CGSize (Single.MaxValue, Single.MaxValue),
                               NSStringDrawingOptions.UsesLineFragmentOrigin,
                               attrs, null);
                rect.Height = (float)Math.Ceiling (rect.Height);
                return rect;
            }

            protected override Task OnContinueAsync ()
            {
                Console.WriteLine ("OnContinueAsync");
                return null;
            }
        }

        class SectionHeaderView : UITableViewHeaderFooterView
        {
            private const float HorizSpacing = 15f;
            private readonly UILabel dateLabel;
            private readonly UILabel totalDurationLabel;

            public SectionHeaderView (IntPtr ptr) : base (ptr)
            {
                dateLabel = new UILabel ().Apply (Style.Log.HeaderDateLabel);
                ContentView.AddSubview (dateLabel);

                totalDurationLabel = new UILabel ().Apply (Style.Log.HeaderDurationLabel);
                ContentView.AddSubview (totalDurationLabel);

                BackgroundView = new UIView ().Apply (Style.Log.HeaderBackgroundView);
            }

            public override void LayoutSubviews ()
            {
                base.LayoutSubviews ();
                var contentFrame = ContentView.Frame;

                dateLabel.Frame = new CGRect (
                    x: HorizSpacing,
                    y: 0,
                    width: (contentFrame.Width - 3 * HorizSpacing) / 2,
                    height: contentFrame.Height
                );

                totalDurationLabel.Frame = new CGRect (
                    x: (contentFrame.Width - 3 * HorizSpacing) / 2 + 2 * HorizSpacing,
                    y: 0,
                    width: (contentFrame.Width - 3 * HorizSpacing) / 2,
                    height: contentFrame.Height
                );
            }

            public void Bind (DateHolder data)
            {
                dateLabel.Text = data.Date.ToLocalizedDateString ();
                totalDurationLabel.Text = FormatDuration (data.TotalDuration);
            }

            private string FormatDuration (TimeSpan duration)
            {
                if (duration.TotalHours >= 1f) {
                    return string.Format (
                               "LogHeaderDurationHoursMinutes".Tr (),
                               (int)duration.TotalHours,
                               duration.Minutes
                           );
                }

                if (duration.Minutes > 0) {
                    return string.Format (
                               "LogHeaderDurationMinutes".Tr (),
                               duration.Minutes
                           );
                }

                return string.Empty;
            }
        }
        #endregion
    }
}
