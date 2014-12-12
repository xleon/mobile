using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using MonoTouch.CoreGraphics;
using MonoTouch.UIKit;
using Toggl.Phoebe.Analytics;
using Toggl.Phoebe.Data;
using Toggl.Phoebe.Data.Reports;
using XPlatUtils;
using Toggl.Ross.Theme;
using Toggl.Ross.Views;

namespace Toggl.Ross.ViewControllers
{
    public class ReportsViewController : UIViewController, InfiniteScrollView<ReportView>.IInfiniteScrollViewSource
    {
        private ZoomLevel _zoomLevel;

        public ZoomLevel ZoomLevel
        {
            get {
                return _zoomLevel;
            } set {
                if (_zoomLevel == value) {
                    return;
                }
                _zoomLevel = value;
                scrollView.RefreshVisibleView ();
            }
        }

        private ReportsMenuController menuController;
        private DateSelectorView dateSelectorView;
        private TopBorder topBorder;
        private SummaryReportView dataSource;
        private InfiniteScrollView<ReportView> scrollView;
        private SyncStatusViewController.StatusView statusView;
        private List<ReportView> cachedReports;
        private int _timeSpaceIndex;
        private bool showStatus;

        const float padding  = 24;
        const float navBarHeight = 64;
        const float selectorHeight = 50;


        public ReportsViewController ()
        {
            EdgesForExtendedLayout = UIRectEdge.None;
            menuController = new ReportsMenuController ();
            dataSource = new SummaryReportView ();
            cachedReports = new List<ReportView>();

            _zoomLevel = ZoomLevel.Week;
            _timeSpaceIndex = 0;
        }

        public override void ViewWillDisappear (bool animated)
        {
            NavigationController.InteractivePopGestureRecognizer.Enabled = true;
            base.ViewWillDisappear (animated);
        }

        public override void ViewDidDisappear (bool animated)
        {
            base.ViewDidDisappear (animated);
            if (menuController != null) {
                menuController.Detach ();
                menuController = null;
            }
        }

        public override void ViewDidLoad ()
        {
            base.ViewDidLoad ();

            View.BackgroundColor = UIColor.White;
            menuController.Attach (this);

            topBorder = new TopBorder ();
            dateSelectorView = new DateSelectorView ();
            dateSelectorView.LeftArrowPressed += (sender, e) => scrollView.SetPageIndex (-1, true);
            dateSelectorView.RightArrowPressed += (sender, e) => {
                if ( _timeSpaceIndex >= 1) { return; }
                scrollView.SetPageIndex ( 1, true);
            };

            scrollView = new InfiniteScrollView<ReportView> ( this);
            scrollView.Delegate = new InfiniteScrollDelegate();
            scrollView.OnChangePage += (sender, e) => {
                _timeSpaceIndex = scrollView.PageIndex;
                var reportView = (ReportView)scrollView.CurrentPage;
                reportView.ZoomLevel = ZoomLevel;
                reportView.TimeSpaceIndex = _timeSpaceIndex;
                reportView.LoadData();
                ChangeReportState();
            };

            statusView = new SyncStatusViewController.StatusView () {
                Retry = RetrySync,
                Cancel = Dismiss,
            };

            Add (scrollView);
            Add (dateSelectorView);
            Add (topBorder);
            Add (statusView);

            ChangeReportState ();
            NavigationController.InteractivePopGestureRecognizer.Enabled = false;
        }

        public override void ViewDidLayoutSubviews ()
        {
            base.ViewDidLayoutSubviews ();
            topBorder.Frame = new RectangleF (0.0f, 0.0f, View.Bounds.Width, 2.0f);
            dateSelectorView.Frame = new RectangleF (0, View.Bounds.Height - selectorHeight, View.Bounds.Width, selectorHeight);
            scrollView.Frame = new RectangleF (0.0f, 0.0f, View.Bounds.Width, View.Bounds.Height - selectorHeight);
            LayoutStatusBar ();
        }

        public override void LoadView ()
        {
            View = new UIView ().Apply (Style.Screen);
        }

        public override void ViewDidAppear (bool animated)
        {
            base.ViewDidAppear (animated);

            ServiceContainer.Resolve<ITracker> ().CurrentScreen = "Reports";
        }

        private void ChangeReportState ()
        {
            dataSource.Period = _zoomLevel;
            dateSelectorView.DateContent = FormattedIntervalDate (_timeSpaceIndex);
        }

        private string FormattedIntervalDate (int backDate)
        {
            string result = "";

            if (backDate == 0) {
                switch (ZoomLevel) {
                case ZoomLevel.Week:
                    result = "ReportsThisWeekSelector".Tr ();
                    break;
                case ZoomLevel.Month:
                    result = "ReportsThisMonthSelector".Tr ();
                    break;
                case ZoomLevel.Year:
                    result = "ReportsThisYearSelector".Tr ();
                    break;
                }
            } else if (backDate == -1) {
                switch (ZoomLevel) {
                case ZoomLevel.Week:
                    result = "ReportsLastWeekSelector".Tr ();
                    break;
                case ZoomLevel.Month:
                    result = "ReportsLastMonthSelector".Tr ();
                    break;
                case ZoomLevel.Year:
                    result = "ReportsLastYearSelector".Tr ();
                    break;
                }
            } else {
                var startDate = dataSource.ResolveStartDate (_timeSpaceIndex);
                var endDate = dataSource.ResolveEndDate (startDate);

                switch (ZoomLevel) {
                case ZoomLevel.Week:
                    if (startDate.Month == endDate.Month) {
                        result = startDate.ToString ("ReportsStartWeekInterval".Tr ()) + " - " + endDate.ToString ("ReportsEndWeekInterval".Tr ());
                    } else {
                        result = startDate.Day + "th " + startDate.ToString ("MMM") + " - " + endDate.Day + "th " + startDate.ToString ("MMM");
                    }
                    break;
                case ZoomLevel.Month:
                    result = startDate.ToString ("ReportsMonthInterval".Tr ());
                    break;
                case ZoomLevel.Year:
                    result = startDate.ToString ("ReportsYearInterval".Tr ());
                    break;
                }
            }
            return result;
        }

        #region StatusBar

        private void LayoutStatusBar ()
        {
            var size = View.Frame.Size;
            var statusY = showStatus ? size.Height - selectorHeight : size.Height + 2f;
            statusView.Frame = new RectangleF ( 0, statusY, size.Width, selectorHeight);
        }

        private void RetrySync ()
        {
            Debug.WriteLine ("RetrySync");
        }

        private void Dismiss ()
        {
            StatusBarShown = false;
        }

        private bool StatusBarShown
        {
            get { return showStatus; }
            set {
                if (showStatus == value) {
                    return;
                }
                showStatus = value;
                UIView.Animate (0.5f, LayoutStatusBar);
            }
        }

        #endregion

        #region IInfiniteScrollViewSource implementation

        public ReportView CreateView ()
        {
            ReportView view;
            if (cachedReports.Count == 0) {
                view = new ReportView ();
            } else {
                view = cachedReports[0];
                cachedReports.RemoveAt (0);
            }
            if ( scrollView.Pages.Count > 0) {
                view.Position = scrollView.CurrentPage.Position;
            }
            return view;
        }

        public void Dispose (ReportView view)
        {
            var reportView = view;
            if (reportView.IsClean) {
                reportView.StopReloadData ();
            }
        }

        public bool ShouldStartScroll ()
        {
            var currentReport = scrollView.CurrentPage;

            if (!currentReport.Dragging) {
                currentReport.ScrollEnabled = false;
                foreach (var item in scrollView.Pages) {
                    var report = item;
                    report.Position = currentReport.Position;
                }
            }
            return !currentReport.Dragging;
        }

        #endregion

        internal class InfiniteScrollDelegate : UIScrollViewDelegate
        {
            public override void DecelerationEnded (UIScrollView scrollView)
            {
                var infiniteScroll = (InfiniteScrollView<ReportView>)scrollView;
                infiniteScroll.CurrentPage.ScrollEnabled = true;
            }
        }

        internal class TopBorder : UIView
        {
            public TopBorder ()
            {
                BackgroundColor = UIColor.Clear;
            }

            public override void Draw (RectangleF rect)
            {
                using (CGContext g = UIGraphics.GetCurrentContext()) {
                    Color.TimeBarBoderColor.SetColor ();
                    g.FillRect (new RectangleF (0.0f, 0.0f, rect.Width, 1.0f / ContentScaleFactor));
                }
            }
        }
    }
}