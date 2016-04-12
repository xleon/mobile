using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Linq;
using CoreAnimation;
using CoreGraphics;
using Foundation;
using GalaSoft.MvvmLight.Helpers;
using Toggl.Phoebe;
using Toggl.Phoebe.Data.Models;
using Toggl.Phoebe.Reactive;
using Toggl.Phoebe.ViewModels;
using Toggl.Phoebe.ViewModels.Timer;
using Toggl.Ross.DataSources;
using Toggl.Ross.Theme;
using Toggl.Ross.Views;
using UIKit;

namespace Toggl.Ross.ViewControllers
{
    public class
        LogViewController : UITableViewController
    {
        private const string DefaultDurationText = " 00:00:00 ";
        readonly static NSString EntryCellId = new NSString("EntryCellId");
        readonly static NSString SectionCellId = new NSString("SectionCellId");

        private SimpleEmptyView defaultEmptyView;
        private UIView obmEmptyView;
        private UIView reloadView;
        private UIButton durationButton;
        private UIButton actionButton;
        private UIBarButtonItem navigationButton;
        private UIActivityIndicatorView defaultFooterView;

        private Binding<LogTimeEntriesVM.LoadInfoType, LogTimeEntriesVM.LoadInfoType> loadInfoBinding, loadMoreBinding;
        private Binding<int, int> hasItemsBinding;
        private Binding<string, string> durationBinding;
        private Binding<Tuple<string, Guid>, Tuple<string, Guid>> constrainErrorBinding;
        private Binding<bool, bool> syncBinding, hasErrorBinding, isRunningBinding;
        private Binding<ObservableCollection<IHolder>, ObservableCollection<IHolder>> collectionBinding;

        protected LogTimeEntriesVM ViewModel { get; set;}

        public LogViewController() : base(UITableViewStyle.Plain)
        {
        }

        public override void LoadView()
        {
            base.LoadView();

            NavigationItem.LeftBarButtonItem = new UIBarButtonItem(
                Image.IconNav.ImageWithRenderingMode(UIImageRenderingMode.AlwaysOriginal),
                UIBarButtonItemStyle.Plain, OnNavigationButtonTouched);

            defaultEmptyView = new SimpleEmptyView
            {
                Title = "LogEmptyTitle".Tr(),
                Message = "LogEmptyMessage".Tr(),
            };

            obmEmptyView = new OBMEmptyView
            {
                Title = "LogOBMEmptyTitle".Tr(),
                Message = "LogOBMEmptyMessage".Tr(),
            };

            reloadView = new ReloadTableViewFooter()
            {
                SyncButtonPressedHandler = OnTryAgainBtnPressed
            };

            // Setup top toolbar
            if (durationButton == null)
            {
                durationButton = new UIButton().Apply(Style.NavTimer.DurationButton);
                durationButton.SetTitle(DefaultDurationText, UIControlState.Normal);  // Dummy content to use for sizing of the label
                durationButton.SizeToFit();
                durationButton.TouchUpInside += OnDurationButtonTouchUpInside;
            }

            if (navigationButton == null)
            {
                actionButton = new UIButton().Apply(Style.NavTimer.StartButton);
                actionButton.SizeToFit();
                actionButton.TouchUpInside += OnActionButtonTouchUpInside;
                navigationButton = new UIBarButtonItem(actionButton);
            }

            // Attach views
            var navigationItem = NavigationItem;
            navigationItem.TitleView = durationButton;
            navigationItem.RightBarButtonItem = navigationButton;
        }

        public override void ViewDidLoad()
        {
            base.ViewDidLoad();
            EdgesForExtendedLayout = UIRectEdge.None;
            TableView.RegisterClassForCellReuse(typeof(TimeEntryCell), EntryCellId);
            TableView.RegisterClassForCellReuse(typeof(SectionCell), SectionCellId);
            TableView.SetEditing(false, true);

            // Create view model
            ViewModel = new LogTimeEntriesVM(StoreManager.Singleton.AppState);

            var headerView = new TableViewRefreshView();
            RefreshControl = headerView;
            headerView.AdaptToTableView(TableView);
            headerView.ValueChanged += (sender, e) => ViewModel.TriggerFullSync();

            // Bindings
            syncBinding = this.SetBinding(() => ViewModel.IsFullSyncing).WhenSourceChanges(() =>
            {
                if (!ViewModel.IsFullSyncing)
                {
                    headerView.EndRefreshing();
                }
            });

            loadInfoBinding = this.SetBinding(() => ViewModel.LoadInfo).WhenSourceChanges(SetFooterState);
            hasItemsBinding = this.SetBinding(() => ViewModel.Collection.Count).WhenSourceChanges(SetCollectionState);
            loadMoreBinding = this.SetBinding(() => ViewModel.LoadInfo).WhenSourceChanges(LoadMoreIfNeeded);
            constrainErrorBinding = this.SetBinding(() => ViewModel.LastCRUDError).WhenSourceChanges(ShowConstrainError);
            collectionBinding = this.SetBinding(() => ViewModel.Collection).WhenSourceChanges(() =>
            {
                TableView.Source = new TimeEntriesSource(this, ViewModel);
            });
            isRunningBinding = this.SetBinding(() => ViewModel.IsEntryRunning).WhenSourceChanges(SetStartStopButtonState);
            durationBinding = this.SetBinding(() => ViewModel.Duration).WhenSourceChanges(() => durationButton.SetTitle(ViewModel.Duration, UIControlState.Normal));
        }

        public override void ViewWillDisappear(bool animated)
        {
            if (IsMovingFromParentViewController)
            {
                ViewModel.Dispose();
            }
            base.ViewWillDisappear(animated);
        }

        public override void ViewDidLayoutSubviews()
        {
            base.ViewDidLayoutSubviews();
            defaultEmptyView.Frame = new CGRect(25f, (View.Frame.Size.Height - 200f) / 2, View.Frame.Size.Width - 50f, 200f);
            obmEmptyView.Frame = new CGRect(25f, 15f, View.Frame.Size.Width - 50f, 200f);
            reloadView.Bounds = new CGRect(0f, 0f, View.Frame.Size.Width, 70f);
            reloadView.Center = new CGPoint(View.Center.X, reloadView.Center.Y);
        }

        private void OnDurationButtonTouchUpInside(object sender, EventArgs e)
        {

        }

        private async void OnActionButtonTouchUpInside(object sender, EventArgs e)
        {
            // Send experiment data.
            ViewModel.ReportExperiment(OBMExperimentManager.StartButtonActionKey,
                                       OBMExperimentManager.ClickActionValue);

            if (!ViewModel.IsEntryRunning)
            {
                var te = await ViewModel.StartNewTimeEntryAsync();

                // Show next viewController.
                var controllers = new List<UIViewController>(NavigationController.ViewControllers);
                var editController = new EditTimeEntryViewController(te.Id);
                controllers.Add(editController);
                if (StoreManager.Singleton.AppState.Settings.ChooseProjectForNew)
                {
                    controllers.Add(new ProjectSelectionViewController(te.WorkspaceId, editController));
                }
                NavigationController.SetViewControllers(controllers.ToArray(), true);
            }
            else
            {
                ViewModel.StopTimeEntry();
            }
        }

        private void SetStartStopButtonState()
        {
            if (ViewModel.IsEntryRunning)
            {
                actionButton.Apply(Style.NavTimer.StopButton);
            }
            else
            {
                actionButton.Apply(Style.NavTimer.StartButton);
            }
        }

        private void LoadMoreIfNeeded()
        {
            // ATTENTION: Small hack due to the scroll needs more than the
            // 10 items to work correctly and load more items. With this conditions,
            // we avoid the scroll spinner to dissapear forcing
            // an extra load.
            if (ViewModel.Collection.Count > 0 &&
                    ViewModel.Collection.Count < 10 &&
                    ViewModel.LoadInfo.HasMore &&
                    !ViewModel.LoadInfo.HadErrors &&
                    !ViewModel.LoadInfo.IsSyncing)
            {
                ViewModel.LoadMore();
            }
        }

        private void SetFooterState()
        {

            if (ViewModel.LoadInfo.HasMore && !ViewModel.LoadInfo.HadErrors)
            {
                if (defaultFooterView == null)
                {
                    defaultFooterView = new UIActivityIndicatorView(UIActivityIndicatorViewStyle.Gray);
                    defaultFooterView.Frame = new CGRect(0, 0, 50, 50);
                    defaultFooterView.StartAnimating();
                }
                TableView.TableFooterView = defaultFooterView;
            }
            else if (ViewModel.LoadInfo.HasMore && ViewModel.LoadInfo.HadErrors)
            {
                TableView.TableFooterView = reloadView;
            }
            else if (!ViewModel.LoadInfo.HasMore && !ViewModel.LoadInfo.HadErrors)
            {
                SetCollectionState();
            }
        }

        private void SetCollectionState()
        {
            // ATTENTION Needed condition to keep visible the list
            // while the first sync is finishing. Why? Because the scroll spinner
            // is used and we need the TableView visible.
            if (ViewModel.LoadInfo.IsSyncing && ViewModel.Collection.Count == 0)
            {
                return;
            }

            UIView emptyView = defaultEmptyView; // Default empty view.
            var showWelcome = ViewModel.ShowWelcomeScreen();
            var hasItems = ViewModel.Collection.Count > 0;
            var isInExperiment = ViewModel.IsInExperiment();

            // According to settings, show welcome message or no.
            ((SimpleEmptyView)emptyView).Title = showWelcome ? "LogWelcomeTitle".Tr() : "LogEmptyTitle".Tr();

            if (showWelcome && isInExperiment)
            {
                emptyView = obmEmptyView;
            }

            TableView.TableFooterView = hasItems ? new UIView() : emptyView;
        }

        private void OnCountinueTimeEntry(int index)
        {
            ViewModel.ContinueTimeEntry(index);
        }

        private void OnTryAgainBtnPressed()
        {
            ViewModel.LoadMore();
        }

        private void OnNavigationButtonTouched(object sender, EventArgs e)
        {
            var main = AppDelegate.TogglWindow.RootViewController as MainViewController;
            main.ToggleMenu();
        }

        private void ShowConstrainError()
        {
            if (ViewModel.LastCRUDError != null)
            {
                var alert = new UIAlertView(
                    "RequiredfieldMessage".Tr(),
                    ViewModel.LastCRUDError.Item1,
                    null, "RequiredfieldEditBtn".Tr());
                alert.Clicked += (sender, e) =>
                {
                    var controllers = new List<UIViewController>(NavigationController.ViewControllers);
                    var editController = new EditTimeEntryViewController(ViewModel.LastCRUDError.Item2);
                    controllers.Add(editController);
                    NavigationController.SetViewControllers(controllers.ToArray(), true);
                };
                alert.Show();
            }
        }

        #region TableViewSource
        class TimeEntriesSource : PlainObservableCollectionViewSource<IHolder>
        {
            private bool isLoading;
            private readonly LogTimeEntriesVM VM;
            private LogViewController owner;
            private IDisposable durationSuscriber;

            public TimeEntriesSource(LogViewController owner, LogTimeEntriesVM viewModel) : base(owner.TableView, viewModel.Collection)
            {
                this.owner = owner;
                VM = viewModel;
                durationSuscriber = viewModel.TimerObservable.Subscribe(x => UpdateDuration());
            }

            private void UpdateDuration()
            {
                foreach (var item in tableView.VisibleCells)
                {
                    ((IDurationCell)item).UpdateDuration();
                }
            }

            public override UITableViewCell GetCell(UITableView tableView, NSIndexPath indexPath)
            {
                UITableViewCell cell;
                var holder = collection.ElementAt(indexPath.Row);

                if (holder is ITimeEntryHolder)
                {
                    cell = tableView.DequeueReusableCell(EntryCellId, indexPath);
                    ((TimeEntryCell)cell).Bind((ITimeEntryHolder)holder, OnContinueTimeEntry);
                }
                else
                {
                    cell = tableView.DequeueReusableCell(SectionCellId, indexPath);
                    ((SectionCell)cell).Bind((DateHolder)holder);
                }

                return cell;
            }

            public override UIView GetViewForHeader(UITableView tableView, nint section)
            {
                return new UIView().Apply(Style.ProjectList.HeaderBackgroundView);
            }

            public override nfloat GetHeightForHeader(UITableView tableView, nint section)
            {
                return EstimatedHeightForHeader(tableView, section);
            }

            public override nfloat EstimatedHeightForHeader(UITableView tableView, nint section)
            {
                return -1f;
            }

            public override nfloat EstimatedHeight(UITableView tableView, NSIndexPath indexPath)
            {
                return 60f;
            }

            public override nfloat GetHeightForRow(UITableView tableView, NSIndexPath indexPath)
            {
                var holder = collection.ElementAt(indexPath.Row);
                if (holder is DateHolder)
                {
                    return 42f;
                }
                return EstimatedHeight(tableView, indexPath);
            }

            public override bool CanFocusRow(UITableView tableView, NSIndexPath indexPath)
            {
                return collection.ElementAt(indexPath.Row) is ITimeEntryHolder;
            }

            public override bool CanEditRow(UITableView tableView, NSIndexPath indexPath)
            {
                return collection.ElementAt(indexPath.Row) is ITimeEntryHolder;
            }

            public override void CommitEditingStyle(UITableView tableView, UITableViewCellEditingStyle editingStyle, NSIndexPath indexPath)
            {
                if (editingStyle == UITableViewCellEditingStyle.Delete)
                {
                    VM.RemoveTimeEntry(indexPath.Row);
                }
            }

            public override void RowSelected(UITableView tableView, NSIndexPath indexPath)
            {
                var holder = collection.ElementAt(indexPath.Row) as ITimeEntryHolder;
                if (holder != null)
                    owner.NavigationController.PushViewController(new EditTimeEntryViewController(holder.Entry.Data.Id), true);
                tableView.DeselectRow(indexPath, true);
            }

            public override void Scrolled(UIScrollView scrollView)
            {
                var currentOffset = scrollView.ContentOffset.Y;
                var maximumOffset = scrollView.ContentSize.Height - scrollView.Frame.Height;

                if (isLoading)
                {
                    isLoading &= maximumOffset - currentOffset <= 200.0;
                }

                if (!isLoading && maximumOffset - currentOffset <= 200.0)
                {
                    isLoading = true;
                    VM.LoadMore();
                }
            }

            private void OnContinueTimeEntry(TimeEntryCell cell)
            {
                var indexPath = TableView.IndexPathForCell(cell);
                VM.ContinueTimeEntry(indexPath.Row);
            }

            protected override void Dispose(bool disposing)
            {
                if (disposing)
                {
                    if (durationSuscriber != null)
                    {
                        durationSuscriber.Dispose();
                        durationSuscriber = null;
                    }
                }
                base.Dispose(disposing);
            }
        }
        #endregion

        #region Cells
        interface IDurationCell
        {
            void UpdateDuration();
        }

        class TimeEntryCell : SwipableTimeEntryTableViewCell, IDurationCell
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
            private bool isRunning;
            private TimeSpan duration;
            private Action<TimeEntryCell> OnContinueAction;
            private DateTime startTime;

            public TimeEntryCell(IntPtr ptr) : base(ptr)
            {
                textContentView = new UIView();
                projectLabel = new UILabel().Apply(Style.Log.CellProjectLabel);
                clientLabel = new UILabel().Apply(Style.Log.CellClientLabel);
                taskLabel = new UILabel().Apply(Style.Log.CellTaskLabel);
                descriptionLabel = new UILabel().Apply(Style.Log.CellDescriptionLabel);
                taskSeparatorImageView = new UIImageView().Apply(Style.Log.CellTaskDescriptionSeparator);
                billableTagsImageView = new UIImageView();
                durationLabel = new UILabel().Apply(Style.Log.CellDurationLabel);
                runningImageView = new UIImageView().Apply(Style.Log.CellRunningIndicator);

                textContentView.AddSubviews(
                    projectLabel, clientLabel,
                    taskLabel, descriptionLabel,
                    taskSeparatorImageView
                );

                var maskLayer = new CAGradientLayer()
                {
                    AnchorPoint = CGPoint.Empty,
                    StartPoint = new CGPoint(0.0f, 0.0f),
                    EndPoint = new CGPoint(1.0f, 0.0f),
                    Colors = new []
                    {
                        UIColor.FromWhiteAlpha(1, 1).CGColor,
                        UIColor.FromWhiteAlpha(1, 1).CGColor,
                        UIColor.FromWhiteAlpha(1, 0).CGColor,
                    },
                    Locations = new []
                    {
                        NSNumber.FromFloat(0f),
                        NSNumber.FromFloat(0.9f),
                        NSNumber.FromFloat(1f),
                    },
                };
                textContentView.Layer.Mask = maskLayer;

                ActualContentView.AddSubviews(
                    textContentView,
                    billableTagsImageView,
                    durationLabel,
                    runningImageView
                );
            }

            public void Bind(ITimeEntryHolder dataSource, Action<TimeEntryCell> OnContinueAction)
            {
                this.OnContinueAction = OnContinueAction;

                var projectName = "LogCellNoProject".Tr();
                var projectColor = Color.Gray;
                var clientName = string.Empty;
                var info = dataSource.Entry.Info;

                if (!string.IsNullOrWhiteSpace(info.ProjectData.Name))
                {
                    projectName = info.ProjectData.Name;
                    projectColor = UIColor.Clear.FromHex(ProjectData.HexColors [info.ProjectData.Color % ProjectData.HexColors.Length]);

                    if (!string.IsNullOrWhiteSpace(info.ClientData.Name))
                    {
                        clientName = info.ClientData.Name;
                    }
                }

                projectLabel.TextColor = projectColor;
                if (projectLabel.Text != projectName)
                {
                    projectLabel.Text = projectName;
                    SetNeedsLayout();
                }
                if (clientLabel.Text != clientName)
                {
                    clientLabel.Text = clientName;
                    SetNeedsLayout();
                }

                var taskName = info.TaskData.Name;
                var taskHidden = string.IsNullOrWhiteSpace(taskName);
                var description = dataSource.Entry.Data.Description;
                var descHidden = string.IsNullOrWhiteSpace(description);

                if (taskHidden && descHidden)
                {
                    description = "LogCellNoDescription".Tr();
                    descHidden = false;
                }
                var taskDeskSepHidden = taskHidden || descHidden;

                if (taskLabel.Hidden != taskHidden || taskLabel.Text != taskName)
                {
                    taskLabel.Hidden = taskHidden;
                    taskLabel.Text = taskName;
                    SetNeedsLayout();
                }
                if (descriptionLabel.Hidden != descHidden || descriptionLabel.Text != description)
                {
                    descriptionLabel.Hidden = descHidden;
                    descriptionLabel.Text = description;
                    SetNeedsLayout();
                }
                if (taskSeparatorImageView.Hidden != taskDeskSepHidden)
                {
                    taskSeparatorImageView.Hidden = taskDeskSepHidden;
                    SetNeedsLayout();
                }

                // Set duration
                duration = dataSource.GetDuration();
                startTime = dataSource.GetStartTime();
                isRunning = dataSource.Entry.Data.State == TimeEntryState.Running;
                RebindTags(dataSource);
                RebindDuration();
                LayoutIfNeeded();
            }

            public void UpdateDuration()
            {
                if (isRunning)
                {
                    duration = Time.UtcNow.Truncate(TimeSpan.TicksPerSecond) - startTime.ToUtc();
                }
                RebindDuration();
            }

            // Rebind duration with the saved state "lastDataSource"
            // TODO: Try to find a stateless method.
            private void RebindDuration()
            {
                runningImageView.Hidden = !isRunning;
                durationLabel.Text = string.Format("{0:D2}:{1:mm}:{1:ss}", (int)duration.TotalHours, duration);
            }

            private void RebindTags(ITimeEntryHolder dataSource)
            {
                var hasTags = dataSource.Entry.Data.Tags.Count > 0;
                var isBillable = dataSource.Entry.Data.IsBillable;

                if (hasTags && isBillable)
                {
                    billableTagsImageView.Apply(Style.Log.BillableAndTaggedEntry);
                }
                else if (hasTags)
                {
                    billableTagsImageView.Apply(Style.Log.TaggedEntry);
                }
                else if (isBillable)
                {
                    billableTagsImageView.Apply(Style.Log.BillableEntry);
                }
                else
                {
                    billableTagsImageView.Apply(Style.Log.PlainEntry);
                }
            }

            protected override void OnContinueGestureFinished()
            {
                if (OnContinueAction != null)
                {
                    OnContinueAction.Invoke(this);
                }
            }

            public override void LayoutSubviews()
            {
                base.LayoutSubviews();

                var contentFrame = ContentView.Frame;

                const float durationLabelWidth = 80f;
                durationLabel.Frame = new CGRect(
                    x: contentFrame.Width - durationLabelWidth - HorizPadding,
                    y: 0,
                    width: durationLabelWidth,
                    height: contentFrame.Height
                );

                const float billableTagsHeight = 20f;
                const float billableTagsWidth = 20f;
                billableTagsImageView.Frame = new CGRect(
                    y: (contentFrame.Height - billableTagsHeight) / 2,
                    height: billableTagsHeight,
                    x: durationLabel.Frame.X - billableTagsWidth,
                    width: billableTagsWidth
                );

                var runningHeight = runningImageView.Image.Size.Height;
                var runningWidth = runningImageView.Image.Size.Width;
                runningImageView.Frame = new CGRect(
                    y: (contentFrame.Height - runningHeight) / 2,
                    height: runningHeight,
                    x: contentFrame.Width - (HorizPadding + runningWidth) / 2,
                    width: runningWidth
                );

                textContentView.Frame = new CGRect(
                    x: 0, y: 0,
                    width: billableTagsImageView.Frame.X - 2f,
                    height: contentFrame.Height
                );
                textContentView.Layer.Mask.Bounds = textContentView.Frame;

                var bounds = GetBoundingRect(projectLabel);
                projectLabel.Frame = new CGRect(
                    x: HorizPadding,
                    y: contentFrame.Height / 2 - bounds.Height,
                    width: bounds.Width,
                    height: bounds.Height
                );

                const float clientLeftMargin = 7.5f;
                bounds = GetBoundingRect(clientLabel);
                clientLabel.Frame = new CGRect(
                    x: projectLabel.Frame.X + projectLabel.Frame.Width + clientLeftMargin,
                    y: (float)Math.Floor(projectLabel.Frame.Y + projectLabel.Font.Ascender - clientLabel.Font.Ascender),
                    width: bounds.Width,
                    height: bounds.Height
                );

                const float secondLineTopMargin = 3f;
                nfloat offsetX = HorizPadding + 1f;
                if (!taskLabel.Hidden)
                {
                    bounds = GetBoundingRect(taskLabel);
                    taskLabel.Frame = new CGRect(
                        x: offsetX,
                        y: contentFrame.Height / 2 + secondLineTopMargin,
                        width: bounds.Width,
                        height: bounds.Height
                    );
                    offsetX += taskLabel.Frame.Width + 4f;

                    if (!taskSeparatorImageView.Hidden)
                    {
                        const float separatorOffsetY = -2f;
                        var imageSize = taskSeparatorImageView.Image != null ? taskSeparatorImageView.Image.Size : CGSize.Empty;
                        taskSeparatorImageView.Frame = new CGRect(
                            x: offsetX,
                            y: taskLabel.Frame.Y + taskLabel.Font.Ascender - imageSize.Height + separatorOffsetY,
                            width: imageSize.Width,
                            height: imageSize.Height
                        );

                        offsetX += taskSeparatorImageView.Frame.Width + 4f;
                    }

                    if (!descriptionLabel.Hidden)
                    {
                        bounds = GetBoundingRect(descriptionLabel);
                        descriptionLabel.Frame = new CGRect(
                            x: offsetX,
                            y: (float)Math.Floor(taskLabel.Frame.Y + taskLabel.Font.Ascender - descriptionLabel.Font.Ascender),
                            width: bounds.Width,
                            height: bounds.Height
                        );

                        offsetX += descriptionLabel.Frame.Width + 4f;
                    }
                }
                else if (!descriptionLabel.Hidden)
                {
                    bounds = GetBoundingRect(descriptionLabel);
                    descriptionLabel.Frame = new CGRect(
                        x: offsetX,
                        y: contentFrame.Height / 2 + secondLineTopMargin,
                        width: bounds.Width,
                        height: bounds.Height
                    );
                }
            }

            private static CGRect GetBoundingRect(UILabel view)
            {
                var attrs = new UIStringAttributes
                {
                    Font = view.Font,
                };
                var rect = ((NSString)(view.Text ?? string.Empty)).GetBoundingRect(
                               new CGSize(float.MaxValue, float.MaxValue),
                               NSStringDrawingOptions.UsesLineFragmentOrigin,
                               attrs, null);
                rect.Height = (float)Math.Ceiling(rect.Height);
                return rect;
            }
        }

        private class SectionCell : UITableViewCell, IDurationCell
        {
            private const float HorizSpacing = 15f;
            private readonly UILabel dateLabel;
            private readonly UILabel totalDurationLabel;
            private bool isRunning;
            private TimeSpan duration;
            private DateTime date;

            public SectionCell(IntPtr handle) : base(handle)
            {
                UserInteractionEnabled = false;
                dateLabel = new UILabel().Apply(Style.Log.HeaderDateLabel);
                ContentView.AddSubview(dateLabel);

                totalDurationLabel = new UILabel().Apply(Style.Log.HeaderDurationLabel);
                ContentView.AddSubview(totalDurationLabel);

                BackgroundView = new UIView().Apply(Style.Log.HeaderBackgroundView);
            }

            public void UpdateDuration()
            {
                if (isRunning)
                {
                    duration = duration.Add(TimeSpan.FromSeconds(1));
                    SetContentData();
                }
            }

            public void Bind(DateHolder data)
            {
                date = data.Date;
                duration = data.TotalDuration;
                isRunning = data.IsRunning;
                SetContentData();
            }

            private void SetContentData()
            {
                dateLabel.Text = date.ToLocalizedDateString();
                totalDurationLabel.Text = FormatDuration(duration);
            }

            private string FormatDuration(TimeSpan dr)
            {
                if (dr.TotalHours >= 1f)
                {
                    return string.Format(
                               "LogHeaderDurationHoursMinutes".Tr(),
                               (int)dr.TotalHours,
                               dr.Minutes
                           );
                }

                if (dr.Minutes > 0)
                {
                    return string.Format(
                               "LogHeaderDurationMinutes".Tr(),
                               dr.Minutes
                           );
                }

                return string.Empty;
            }

            public override void LayoutSubviews()
            {
                base.LayoutSubviews();
                var contentFrame = ContentView.Frame;

                dateLabel.Frame = new CGRect(
                    x: HorizSpacing,
                    y: 0,
                    width: (contentFrame.Width - 3 * HorizSpacing) / 2,
                    height: contentFrame.Height
                );

                totalDurationLabel.Frame = new CGRect(
                    x: (contentFrame.Width - 3 * HorizSpacing) / 2 + 2 * HorizSpacing,
                    y: 0,
                    width: (contentFrame.Width - 3 * HorizSpacing) / 2,
                    height: contentFrame.Height
                );
            }
        }
        #endregion
    }
}
