using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Timers;
using CoreAnimation;
using CoreGraphics;
using Foundation;
using GalaSoft.MvvmLight.Helpers;
using Toggl.Phoebe;
using Toggl.Phoebe.Data;
using Toggl.Phoebe.Data.DataObjects;
using Toggl.Phoebe.Data.Models;
using Toggl.Phoebe.Data.Utils;
using Toggl.Phoebe.Data.ViewModels;
using Toggl.Ross.Data;
using Toggl.Ross.DataSources;
using Toggl.Ross.Theme;
using Toggl.Ross.Views;
using UIKit;
using XPlatUtils;

namespace Toggl.Ross.ViewControllers
{
    public class LogViewController : UITableViewController
    {
        private const string DefaultDurationText = " 00:00:00 ";
        readonly static NSString EntryCellId = new NSString ("EntryCellId");
        readonly static NSString SectionHeaderId = new NSString ("SectionHeaderId");

        private SimpleEmptyView defaultEmptyView;
        private UIView obmEmptyView;
        private UIView reloadView;
        private UIButton durationButton;
        private UIButton actionButton;
        private UIBarButtonItem navigationButton;
        private UIActivityIndicatorView defaultFooterView;

        private Binding<string, string> durationBinding;
        private Binding<bool, bool> syncBinding, hasMoreBinding, hasErrorBinding, isRunningBinding;
        private Binding<LogTimeEntriesViewModel.CollectionState, LogTimeEntriesViewModel.CollectionState> hasItemsBinding, loadMoreBinding;
        private Binding<ObservableCollection<IHolder>, ObservableCollection<IHolder>> collectionBinding;

        protected LogTimeEntriesViewModel ViewModel {get; set;}

        public LogViewController () : base (UITableViewStyle.Plain)
        {
        }

        public override void LoadView()
        {
            base.LoadView();


            defaultEmptyView = new SimpleEmptyView {
                Title = "LogEmptyTitle".Tr (),
                Message = "LogEmptyMessage".Tr (),
            };

            obmEmptyView = new OBMEmptyView {
                Title = "LogOBMEmptyTitle".Tr (),
                Message = "LogOBMEmptyMessage".Tr (),
            };

            reloadView = new ReloadTableViewFooter () {
                SyncButtonPressedHandler = OnTryAgainBtnPressed
            };

            // Setup top toolbar
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

        public async override void ViewDidLoad ()
        {
            base.ViewDidLoad ();

            NavigationItem.LeftBarButtonItem = new UIBarButtonItem (
                Image.IconNav.ImageWithRenderingMode (UIImageRenderingMode.AlwaysOriginal),
                UIBarButtonItemStyle.Plain, OnNavigationButtonTouched);

            EdgesForExtendedLayout = UIRectEdge.None;
            TableView.RegisterClassForCellReuse (typeof (TimeEntryCell), EntryCellId);
            TableView.RegisterClassForHeaderFooterViewReuse (typeof (SectionHeaderView), SectionHeaderId);
            TableView.SetEditing (false, true);

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
            hasItemsBinding = this.SetBinding (() => ViewModel.HasItems).WhenSourceChanges (SetCollectionState);
            loadMoreBinding = this.SetBinding (() => ViewModel.HasItems).WhenSourceChanges (LoadMoreIfNeeded);
            collectionBinding = this.SetBinding (() => ViewModel.Collection).WhenSourceChanges (() => {
                TableView.Source = new TimeEntriesSource (this, ViewModel);
            });
            isRunningBinding = this.SetBinding (() => ViewModel.IsTimeEntryRunning).WhenSourceChanges (SetStartStopButtonState);
            durationBinding = this.SetBinding (() => ViewModel.Duration).WhenSourceChanges (() => durationButton.SetTitle (ViewModel.Duration, UIControlState.Normal));

            // TODO: Review this line.
            // Get data to fill the list. For the moment,
            // until a screenloader is added to the screen
            // is better to load the items after create
            // the viewModel and show the loader from RecyclerView
            await ViewModel.LoadMore ();
        }

        public override void ViewWillDisappear (bool animated)
        {
            if (IsMovingFromParentViewController) {
                ViewModel.Dispose ();
            }
            base.ViewWillDisappear (animated);
        }

        public override void ViewDidLayoutSubviews ()
        {
            base.ViewDidLayoutSubviews ();
            defaultEmptyView.Frame = new CGRect (25f, (View.Frame.Size.Height - 200f) / 2, View.Frame.Size.Width - 50f, 200f);
            obmEmptyView.Frame = new CGRect (25f, 15f, View.Frame.Size.Width - 50f, 200f);
            reloadView.Bounds = new CGRect (0f, 0f, View.Frame.Size.Width, 70f);
            reloadView.Center = new CGPoint (View.Center.X, reloadView.Center.Y);
        }

        private void OnDurationButtonTouchUpInside (object sender, EventArgs e)
        {

        }

        private async void OnActionButtonTouchUpInside (object sender, EventArgs e)
        {
            // Send experiment data.
            ViewModel.ReportExperiment (OBMExperimentManager.StartButtonActionKey,
                                        OBMExperimentManager.ClickActionValue);

            var entry = await ViewModel.StartStopTimeEntry ();
            if (entry.State == TimeEntryState.Running) {
                // Show next viewController.
                var controllers = new List<UIViewController> (NavigationController.ViewControllers);
                var tagList = await ServiceContainer.Resolve<IDataStore> ().GetTimeEntryTags (entry.Id);
                var editController = new EditTimeEntryViewController (entry, tagList);
                controllers.Add (editController);
                if (ServiceContainer.Resolve<SettingsStore> ().ChooseProjectForNew) {
                    controllers.Add (new ProjectSelectionViewController (entry.WorkspaceId, editController));
                }
                NavigationController.SetViewControllers (controllers.ToArray (), true);
            }
        }

        private void SetStartStopButtonState ()
        {
            if (ViewModel.IsTimeEntryRunning) {
                actionButton.Apply (Style.NavTimer.StopButton);
            } else {
                actionButton.Apply (Style.NavTimer.StartButton);
            }
        }

        private async void LoadMoreIfNeeded ()
        {
            // TODO: Small hack due to the scroll needs more than the
            // 10 items to work correctly and load more itens.
            if (ViewModel.Collection.Count > 0 && ViewModel.Collection.Count < 10) {
                await ViewModel.LoadMore ();
            }
        }

        private void SetFooterState ()
        {
            if (ViewModel.HasMoreItems && !ViewModel.HasLoadErrors) {
                if (defaultFooterView == null) {
                    defaultFooterView = new UIActivityIndicatorView (UIActivityIndicatorViewStyle.Gray);
                    defaultFooterView.Frame = new CGRect (0, 0, 50, 50);
                    defaultFooterView.StartAnimating ();
                }
                TableView.TableFooterView = defaultFooterView;
            } else if (ViewModel.HasMoreItems && ViewModel.HasLoadErrors) {
                TableView.TableFooterView = reloadView;
            } else if (!ViewModel.HasMoreItems && !ViewModel.HasLoadErrors) {
                SetCollectionState ();
            }
        }

        private void SetCollectionState ()
        {
            if (ViewModel.HasItems != LogTimeEntriesViewModel.CollectionState.NotReady) {
                UIView emptyView = defaultEmptyView; // Default empty view.
                var isWelcome = ServiceContainer.Resolve<ISettingsStore> ().ShowWelcome;
                var isInExperiment = OBMExperimentManager.IncludedInExperiment ();
                var hasItems = ViewModel.HasItems == LogTimeEntriesViewModel.CollectionState.NotEmpty;

                // According to settings, show welcome message or no.
                ((SimpleEmptyView)emptyView).Title = isWelcome ? "LogWelcomeTitle".Tr () : "LogEmptyTitle".Tr ();

                if (isWelcome && isInExperiment) {
                    emptyView = obmEmptyView;
                }

                TableView.TableFooterView = hasItems ? new UIView () : emptyView;
            }
        }

        private async void OnCountinueTimeEntry (int index)
        {
            await ViewModel.ContinueTimeEntryAsync (index);
        }

        private async void OnTryAgainBtnPressed ()
        {
            await ViewModel.LoadMore ();
        }

        private void OnNavigationButtonTouched (object sender, EventArgs e)
        {
            var main = AppDelegate.TogglWindow.RootViewController as MainViewController;
            main.ToggleMenu ();
        }

        #region TableViewSource
        class TimeEntriesSource : ObservableCollectionViewSource<IHolder, DateHolder, ITimeEntryHolder>
        {
            private bool isLoading;
            private LogTimeEntriesViewModel VM;
            private LogViewController owner;

            public TimeEntriesSource (LogViewController owner, LogTimeEntriesViewModel viewModel) : base (owner.TableView, viewModel.Collection)
            {
                this.owner = owner;
                VM = viewModel;
            }

            public override UITableViewCell GetCell (UITableView tableView, NSIndexPath indexPath)
            {
                var cell = (TimeEntryCell)tableView.DequeueReusableCell (EntryCellId, indexPath);
                var holder = (ITimeEntryHolder)collection.ElementAt (GetPlainIndexFromRow (collection, indexPath));
                cell.Bind (holder, OnContinueTimeEntry);
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
                return true;
            }

            public async override void CommitEditingStyle (UITableView tableView, UITableViewCellEditingStyle editingStyle, NSIndexPath indexPath)
            {
                if (editingStyle == UITableViewCellEditingStyle.Delete) {
                    var rowIndex = GetPlainIndexFromRow (collection, indexPath);
                    await VM.RemoveTimeEntryAsync (rowIndex);
                }
            }

            public async override void RowSelected (UITableView tableView, NSIndexPath indexPath)
            {
                var rowIndex = GetPlainIndexFromRow (collection, indexPath);
                var holder = collection.ElementAt (rowIndex) as ITimeEntryHolder;

                var teData = (TimeEntryModel)holder.Data;
                List<TagData> tags = await ServiceContainer.Resolve<IDataStore> ().GetTimeEntryTags (teData.Id);
                owner.NavigationController.PushViewController (new EditTimeEntryViewController (teData, tags), true);
            }

            public async override void Scrolled (UIScrollView scrollView)
            {
                var currentOffset = scrollView.ContentOffset.Y;
                var maximumOffset = scrollView.ContentSize.Height - scrollView.Frame.Height;

                if (isLoading) {
                    isLoading &= maximumOffset - currentOffset <= 200.0;
                }

                if (!isLoading && maximumOffset - currentOffset <= 200.0) {
                    isLoading = true;
                    await VM.LoadMore ();
                }
            }

            private async void OnContinueTimeEntry (TimeEntryCell cell)
            {
                var indexPath = TableView.IndexPathForCell (cell);
                var rowIndex = GetPlainIndexFromRow (collection, indexPath);
                await VM.ContinueTimeEntryAsync (rowIndex);
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
            private Timer timer;
            private bool isRunning;
            private TimeSpan duration;
            private Action<TimeEntryCell> OnContinueAction;

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

            public void Bind (ITimeEntryHolder dataSource, Action<TimeEntryCell> OnContinueAction)
            {
                this.OnContinueAction = OnContinueAction;

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

                // Set duration
                duration = dataSource.GetDuration ();
                isRunning = dataSource.Data.State == TimeEntryState.Running;

                RebindTags (dataSource);
                RebindDuration ();
                LayoutIfNeeded ();
            }

            // Rebind duration with the saved state "lastDataSource"
            // TODO: Try to find a stateless method.
            private void RebindDuration ()
            {
                if (timer != null) {
                    timer.Stop ();
                    timer.Elapsed -= OnDurationElapsed;
                    timer = null;
                }

                if (isRunning) {
                    timer = new Timer (1000 - duration.Milliseconds);
                    timer.Elapsed += OnDurationElapsed;
                    timer.Start ();
                }

                durationLabel.Text = TimeEntryModel.GetFormattedDuration (duration);
                runningImageView.Hidden = !isRunning;
            }

            private void OnDurationElapsed (object sender, ElapsedEventArgs e)
            {
                // Update duration with new time.
                duration = duration.Add (TimeSpan.FromMilliseconds (timer.Interval));
                InvokeOnMainThread (() => RebindDuration ());
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

            protected override void OnContinueGestureFinished ()
            {
                if (OnContinueAction != null) {
                    OnContinueAction.Invoke (this);
                }
            }

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
        }

        class SectionHeaderView : UITableViewHeaderFooterView
        {
            private const float HorizSpacing = 15f;
            private readonly UILabel dateLabel;
            private readonly UILabel totalDurationLabel;
            private Timer timer;
            private bool isRunning;
            private TimeSpan duration;
            private DateTime date;

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
                date = data.Date;
                duration = data.TotalDuration;
                isRunning = data.IsRunning;
                SetContentData ();
            }

            private void SetContentData ()
            {
                if (timer != null) {
                    timer.Stop ();
                    timer.Elapsed -= OnDurationElapsed;
                    timer = null;
                }

                if (isRunning) {
                    timer = new Timer (60000 - duration.Seconds * 1000 - duration.Milliseconds);
                    timer.Elapsed += OnDurationElapsed;
                    timer.Start ();
                }

                dateLabel.Text = date.ToLocalizedDateString ();
                totalDurationLabel.Text = FormatDuration (duration);
            }

            private void OnDurationElapsed (object sender, ElapsedEventArgs e)
            {
                // Update duration with new time.
                duration = duration.Add (TimeSpan.FromMilliseconds (timer.Interval));
                InvokeOnMainThread (() => SetContentData ());
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
