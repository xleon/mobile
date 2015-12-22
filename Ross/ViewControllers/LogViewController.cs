using System;
using System.Collections.Generic;
using System.Collections.Specialized;
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
using Toggl.Phoebe.Data.Views;
using Toggl.Phoebe.Net;
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

        private NavigationMenuController navMenuController;
        private UIView emptyView;
        private UIButton durationButton;
        private UIButton actionButton;
        private UIBarButtonItem navigationButton;
        private UIView EmptyView { get; set; }

        private Binding<bool, bool> syncBinding;
        private Binding<TimeEntriesCollectionView, TimeEntriesCollectionView> collectionBinding;

        protected LogTimeEntriesViewModel ViewModel {get; set;}

        public LogViewController () : base (UITableViewStyle.Plain)
        {
            navMenuController = new NavigationMenuController ();
        }

        public async override void ViewDidLoad ()
        {
            base.ViewDidLoad ();

            EdgesForExtendedLayout = UIRectEdge.None;
            emptyView = new SimpleEmptyView {
                Title = "LogEmptyTitle".Tr (),
                Message = "LogEmptyMessage".Tr (),
            };

            // Create view model
            ViewModel = await LogTimeEntriesViewModel.Init ();
            ViewModel.CollectionView.CollectionChanged += HandleCollectionChanged;

            TableView.RegisterClassForCellReuse (typeof (TimeEntryCell), EntryCellId);
            TableView.RegisterClassForHeaderFooterViewReuse (typeof (SectionHeaderView), SectionHeaderId);
            TableView.Scrolled += async (sender, e) => {
                var currentOffset = TableView.ContentOffset.Y;
                var maximumOffset = TableView.ContentSize.Height - TableView.Frame.Height;
                if (maximumOffset - currentOffset <= 200.0) {
                    await ViewModel.LoadMoreItems ();
                }
            };

            var headerView = new TableViewRefreshView ();
            RefreshControl = headerView;
            headerView.AdaptToTableView (TableView);
            headerView.ValueChanged += (sender, e) => ViewModel.TriggerSync ();

            // Bindings
            syncBinding = this.SetBinding (() => ViewModel.IsAppSyncing).WhenSourceChanges (() => {
                if (!ViewModel.IsAppSyncing) {
                    headerView.EndRefreshing ();
                }
            });
            collectionBinding = this.SetBinding (() => ViewModel.CollectionView).WhenSourceChanges (() => {
                TableView.Source = new MySource (ViewModel.CollectionView.Data);
            });

            // Setup top toolbar
            SetupToolbar ();
            navMenuController.Attach (this);

            foreach (var item in ViewModel.CollectionView.Data) {
                Console.WriteLine (item);
            }
        }

        private void HandleCollectionChanged (object sender, NotifyCollectionChangedEventArgs e)
        {
            var collectionData = ViewModel.CollectionView.Data;

            if (e.Action == NotifyCollectionChangedAction.Reset) {
                TableView.ReloadData();
            }

            Console.WriteLine (e.Action + " " + e.NewStartingIndex);

            if (e.Action == NotifyCollectionChangedAction.Add) {
                if (e.NewItems [0] is DateHolder) {
                    var indexSet = GetSectionFromPlainIndex (collectionData, e.NewStartingIndex);
                    TableView.InsertSections (indexSet, UITableViewRowAnimation.Automatic);
                } else {
                    var indexPath = GetRowFromPlainIndex (collectionData, e.NewStartingIndex);
                    TableView.InsertRows (new [] {indexPath}, UITableViewRowAnimation.Automatic);
                }
            }

            if (e.Action == NotifyCollectionChangedAction.Remove) {
                if (e.OldItems [0] is DateHolder) {
                    var indexSet = GetSectionFromPlainIndex (collectionData, e.OldStartingIndex);
                    TableView.DeleteSections (indexSet, UITableViewRowAnimation.Automatic);
                } else {
                    var indexPath = GetRowFromPlainIndex (collectionData, e.OldStartingIndex);
                    TableView.DeleteRows (new [] {indexPath}, UITableViewRowAnimation.Automatic);
                }
            }

            if (e.Action == NotifyCollectionChangedAction.Replace) {
                if (e.NewItems [0] is DateHolder) {
                    var indexSet = GetSectionFromPlainIndex (collectionData, e.NewStartingIndex);
                    TableView.ReloadSections (indexSet, UITableViewRowAnimation.Automatic);
                } else {
                    var indexPath = GetRowFromPlainIndex (collectionData, e.NewStartingIndex);
                    TableView.ReloadRows (new [] {indexPath}, UITableViewRowAnimation.Automatic);
                }
            }

            if (e.Action == NotifyCollectionChangedAction.Move) {
                if (! (e.NewItems [0] is DateHolder)) {
                    var fromIndexPath = GetRowFromPlainIndex (collectionData, e.OldStartingIndex);
                    var toIndexPath = GetRowFromPlainIndex (collectionData, e.NewStartingIndex);
                    TableView.MoveRow (fromIndexPath, toIndexPath);
                }
            }
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

        #region IEnumerable Utils
        public static NSIndexSet GetSectionFromPlainIndex (IEnumerable<IHolder> collection, int headerIndex)
        {
            var index = collection.Take (headerIndex).OfType <DateHolder> ().Count ();
            return NSIndexSet.FromIndex (index);
        }

        public static NSIndexPath GetRowFromPlainIndex (IEnumerable<IHolder> collection, int holderIndex)
        {
            var enumerable = collection.ToArray ();
            var row = enumerable.Take (holderIndex).Reverse ().IndexOf (p => p is DateHolder);
            var section = enumerable.Take (holderIndex).OfType <DateHolder> ().Count () - 1; // less one this time.
            return NSIndexPath.FromRowSection (row, section);
        }

        public static int GetPlainIndexFromSection (IEnumerable<IHolder> collection, nint sectionIndex)
        {
            return collection.IndexOf (p => p == collection.OfType <DateHolder> ().ElementAt ((int)sectionIndex));
        }

        public static int GetPlainIndexFromRow (IEnumerable<IHolder> collection, NSIndexPath rowIndexPath)
        {
            return GetPlainIndexFromSection (collection, rowIndexPath.Section) + rowIndexPath.Row + 1;
        }

        public static int GetCurrentRowsBySection (IEnumerable<IHolder> collection, nint sectionIndex)
        {
            var enumerable = collection.ToArray ();
            var startIndex = GetPlainIndexFromSection (enumerable, sectionIndex);
            return enumerable.Skip (startIndex + 1).TakeWhile (p => p is ITimeEntryHolder).Count ();
        }
        #endregion

        class MySource : UITableViewSource
        {
            private IEnumerable<IHolder> data;

            public MySource (IEnumerable<IHolder> data)
            {
                this.data = data;
            }

            public override UITableViewCell GetCell (UITableView tableView, NSIndexPath indexPath)
            {
                var cell = (TimeEntryCell)tableView.DequeueReusableCell (EntryCellId, indexPath);
                var holder = (ITimeEntryHolder)data.ElementAt (LogViewController.GetPlainIndexFromRow (data, indexPath));
                cell.Bind (holder);
                return cell;
            }

            public override UIView GetViewForHeader (UITableView tableView, nint section)
            {
                var view = (SectionHeaderView)tableView.DequeueReusableHeaderFooterView (SectionHeaderId);
                view.Bind (data.OfType<DateHolder> ().ElementAt ((int)section));
                return view;
            }

            public override nint RowsInSection (UITableView tableview, nint section)
            {
                return LogViewController.GetCurrentRowsBySection (data, section);
            }

            public override nint NumberOfSections (UITableView tableView)
            {
                return data.OfType<DateHolder> ().Count ();
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
        }

        class ObservableSource : CollectionDataViewSource<IHolder, DateHolder, TimeEntryHolder>, IDisposable
        {
            readonly LogViewController controller;
            readonly TimeEntriesCollectionView dataView;
            private Subscription<SyncFinishedMessage> subscriptionSyncFinished;
            public UIRefreshControl HeaderView { get; set; }

            public ObservableSource (LogViewController controller, TimeEntriesCollectionView dataView) : base (controller.TableView, dataView)
            {
                this.controller = controller;
                this.dataView = dataView;

                NSTimer.CreateRepeatingScheduledTimer (5.0f, delegate {
                    var syncManager = ServiceContainer.Resolve<ISyncManager> ();
                    syncManager.Run (SyncMode.Pull);
                });

                controller.TableView.RegisterClassForCellReuse (typeof (TimeEntryCell), EntryCellId);
                controller.TableView.RegisterClassForHeaderFooterViewReuse (typeof (SectionHeaderView), SectionHeaderId);
            }

            public override void Attach ()
            {
                base.Attach ();

                var bus = ServiceContainer.Resolve<MessageBus> ();
                subscriptionSyncFinished = bus.Subscribe<SyncFinishedMessage> (OnSyncFinished);

                if (HeaderView != null) {
                    HeaderView.ValueChanged += (sender, e) => ServiceContainer.Resolve<ISyncManager> ().Run();
                    dataView.CollectionChanged += (sender, e) => HeaderView.EndRefreshing ();
                }
            }

            public override void OnCollectionChange (object sender, NotifyCollectionChangedEventArgs e)
            {
                // this line is needed to update
                // the section list every time
                // the collection is updated.
                base.OnCollectionChange (sender, e);

                if (e.Action == NotifyCollectionChangedAction.Reset) {
                    TableView.ReloadData ();
                    return;
                }

                TableView.BeginUpdates ();

                if (e.Action == NotifyCollectionChangedAction.Add) {
                    for (int i = 0; i < e.NewItems.Count; i++) {
                        var index = e.NewStartingIndex + i;
                        var elementToAdd = dataView.Data.ElementAt (index);

                        if (elementToAdd is DateHolder) {
                            var indexSet = GetSectionIndexFromItemIndex (index);
                            TableView.InsertSections (indexSet, UITableViewRowAnimation.Automatic);
                        } else {
                            var indexPath = GetRowPathFromItemIndex (index);
                            TableView.InsertRows (new [] {indexPath}, UITableViewRowAnimation.Automatic);
                        }
                    }
                }


                if (e.Action == NotifyCollectionChangedAction.Remove) {
                    if (e.OldItems[0] is DateHolder) {
                        var indexSet = GetSectionIndexFromItemIndex (e.OldStartingIndex);
                        TableView.DeleteSections (indexSet, UITableViewRowAnimation.Automatic);
                    } else {
                        var indexPath = GetRowPathFromItemIndex (e.OldStartingIndex);
                        TableView.DeleteRows (new [] {indexPath}, UITableViewRowAnimation.Automatic);
                    }
                }

                if (e.Action == NotifyCollectionChangedAction.Replace) {
                    if (dataView.Data.ElementAt (e.NewStartingIndex) is DateHolder) {
                        var indexSet = GetSectionIndexFromItemIndex (e.OldStartingIndex);
                        TableView.ReloadSections (indexSet, UITableViewRowAnimation.Automatic);
                    } else {
                        var indexPath = GetRowPathFromItemIndex (e.NewStartingIndex);
                        TableView.ReloadRows (new [] {indexPath}, UITableViewRowAnimation.Automatic);
                    }
                }

                /*
                if (e.Action == NotifyCollectionChangedAction.Move) {
                    var oldSectionSet = GetSectionIndexFromItemIndex (e.OldStartingIndex);
                    var newSectionSet = GetSectionIndexFromItemIndex (e.NewStartingIndex);

                    var plainOldSectionIndx = GetPlainSectionIndexOfItemIndex (e.OldStartingIndex);
                    var plainNewSectionIndx = GetPlainSectionIndexOfItemIndex (e.NewStartingIndex);

                    if (oldSectionSet.FirstIndex == newSectionSet.FirstIndex) {
                        TableView.ReloadSections (oldSectionSet, UITableViewRowAnimation.Automatic);
                    } else {
                        var oldSection = dataView.Data.ElementAt (plainOldSectionIndx) as TimeEntriesCollectionView.DateHolder;
                        if (oldSection != null && oldSection.DataObjects.Any ()) {
                            TableView.ReloadSections (oldSectionSet, UITableViewRowAnimation.Automatic);
                        } else {
                            TableView.DeleteSections (oldSectionSet, UITableViewRowAnimation.Automatic);
                        }

                        var newSection = dataView.Data.ElementAt (plainNewSectionIndx) as TimeEntriesCollectionView.DateHolder;
                        if (newSection != null && newSection.DataObjects.Any ()) {
                            TableView.ReloadSections (newSectionSet, UITableViewRowAnimation.Automatic);
                        } else {
                            TableView.InsertSections (newSectionSet, UITableViewRowAnimation.Automatic);
                        }
                    }
                }
                */
                TableView.EndUpdates ();
            }

            private void OnSyncFinished (SyncFinishedMessage msg)
            {
                HeaderView.EndRefreshing ();
            }

            public override nfloat EstimatedHeight (UITableView tableView, NSIndexPath indexPath)
            {
                return 60f;
            }

            public override nfloat GetHeightForRow (UITableView tableView, NSIndexPath indexPath)
            {
                return EstimatedHeight (tableView, indexPath);
            }

            public override UITableViewCell GetCell (UITableView tableView, NSIndexPath indexPath)
            {
                var cell = (TimeEntryCell)tableView.DequeueReusableCell (EntryCellId, indexPath);
                var index = GetItemIndexFromRowPath (indexPath);
                //cell.ContinueCallback = OnContinue;

                var data = (TimeEntryHolder)dataView.Data.ElementAt (index);
                cell.Bind (data);

                return cell;
            }

            public override UIView GetViewForHeader (UITableView tableView, nint section)
            {
                var view = (SectionHeaderView)tableView.DequeueReusableHeaderFooterView (SectionHeaderId);
                view.Bind (Sections.ElementAt ((int)section));
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

            public override bool CanEditRow (UITableView tableView, NSIndexPath indexPath)
            {
                return false;
            }


            public override void RowSelected (UITableView tableView, NSIndexPath indexPath)
            {
                var index = GetItemIndexFromRowPath (indexPath);
                var data = (TimeEntryHolder)dataView.Data.ElementAt (index);
                if (data != null) {
                    controller.NavigationController.PushViewController (
                        new EditTimeEntryViewController ((TimeEntryModel)data.Data), true);
                } else {
                    tableView.DeselectRow (indexPath, true);
                }
            }

            private int GetHolderIndex (TimeEntryHolder holder)
            {
                return dataView.Data.TakeWhile ((x) => x != holder).Count ();
            }

            private void OnContinue (TimeEntryHolder holder)
            {
                DurationOnlyNoticeAlertView.TryShow ();
                dataView.ContinueTimeEntry (GetHolderIndex (holder));
                controller.TableView.ScrollRectToVisible (new CGRect (0, 0, 1, 1), true);
            }

            private void OnDelete (TimeEntryHolder holder)
            {
                DurationOnlyNoticeAlertView.TryShow ();
                dataView.RemoveItemWithUndoAsync (GetHolderIndex (holder));
            }

            protected void Update ()
            {
                CATransaction.Begin ();
                CATransaction.CompletionBlock = delegate {
                    TableView.ReloadData ();
                };
                TableView.ReloadData ();
                CATransaction.Commit();
            }

            protected override void Dispose (bool disposing)
            {
                if (disposing) {
                    if (subscriptionSyncFinished != null) {
                        var bus = ServiceContainer.Resolve<MessageBus> ();
                        bus.Unsubscribe (subscriptionSyncFinished);
                        subscriptionSyncFinished = null;
                    }
                    TableView.Source = null;
                }
                base.Dispose (disposing);
            }

            protected override bool CompareDataSections (IHolder data, DateHolder section)
            {
                var dateGroup = data as DateHolder;
                return dateGroup.Date == section.Date;
            }
        }

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
            private nint rebindCounter;

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
                var clientName = String.Empty;
                var info = dataSource.Info;

                if (!String.IsNullOrWhiteSpace (info.ProjectData.Name)) {
                    projectName = info.ProjectData.Name;
                    projectColor = UIColor.Clear.FromHex (ProjectModel.HexColors [info.ProjectData.Color % ProjectModel.HexColors.Length]);

                    if (!String.IsNullOrWhiteSpace (info.ClientData.Name)) {
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
                var taskHidden = String.IsNullOrWhiteSpace (taskName);
                var description = info.Description;
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
                var rect = ((NSString) (view.Text ?? String.Empty)).GetBoundingRect (
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
                    return String.Format (
                               "LogHeaderDurationHoursMinutes".Tr (),
                               (int)duration.TotalHours,
                               duration.Minutes
                           );
                }

                if (duration.Minutes > 0) {
                    return String.Format (
                               "LogHeaderDurationMinutes".Tr (),
                               duration.Minutes
                           );
                }

                return String.Empty;
            }
        }
    }
}
