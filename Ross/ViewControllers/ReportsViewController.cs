using System;
using System.Drawing;
using GoogleAnalytics.iOS;
using MonoTouch.CoreGraphics;
using MonoTouch.UIKit;
using Toggl.Phoebe.Data;
using Toggl.Phoebe.Data.Reports;
using XPlatUtils;
using Toggl.Ross.Theme;
using Toggl.Ross.Views;

namespace Toggl.Ross.ViewControllers
{
    public class ReportsViewController : UIViewController
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

                /*
                centerView.ZoomLevel =
                    leftView.ZoomLevel =
                        rightView.ZoomLevel = _zoomLevel;
                */
                ChangeReportState ();
            }
        }

        private ReportsMenuController menuController;
        private DateSelectorView dateSelectorView;
        private TopBorder topBorder;
        private SummaryReportView dataSource;
        InfiniteScrollView scrollView;
        private int _timeSpaceIndex;

        const float padding  = 24;
        const float navBarHeight = 64;
        const float selectorHeight = 50;


        public ReportsViewController ()
        {
            EdgesForExtendedLayout = UIRectEdge.None;
            menuController = new ReportsMenuController ();
            dataSource = new SummaryReportView ();

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

            topBorder = new TopBorder (new RectangleF (0.0f, 0.0f, UIScreen.MainScreen.Bounds.Width, 2.0f));

            dateSelectorView = new DateSelectorView (new RectangleF (0, UIScreen.MainScreen.Bounds.Height - selectorHeight - navBarHeight, UIScreen.MainScreen.Bounds.Width, selectorHeight));
            dateSelectorView.LeftArrowPressed += (sender, e) => scrollView.SetPageIndex (-1, true);
            dateSelectorView.RightArrowPressed += (sender, e) => {
                if ( _timeSpaceIndex >= 1) { return; }
                scrollView.SetPageIndex ( 1, true);
            };

            scrollView = new InfiniteScrollView (new RectangleF (0.0f, 0.0f, UIScreen.MainScreen.Bounds.Width, dateSelectorView.Frame.Y));
            scrollView.OnChangeReport += (sender, e) => {
                _timeSpaceIndex = scrollView.PageIndex;
                var reportView = scrollView.VisibleReport;
                reportView.ZoomLevel = ZoomLevel;
                reportView.TimeSpaceIndex = _timeSpaceIndex;
                reportView.LoadData();
                ChangeReportState();
            };

            Add (scrollView);
            Add (dateSelectorView);
            Add (topBorder);

            ChangeReportState ();
            NavigationController.InteractivePopGestureRecognizer.Enabled = false;
        }

        public override void LoadView ()
        {
            View = new UIView ().Apply (Style.Screen);
        }

        public override void ViewDidAppear (bool animated)
        {
            base.ViewDidAppear (animated);

            var tracker = ServiceContainer.Resolve<IGAITracker> ();
            tracker.Set (GAIConstants.ScreenName, "Reports View");
            tracker.Send (GAIDictionaryBuilder.CreateAppView ().Build ());
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
                    result = "ThisWeekSelector".Tr ();
                    break;
                case ZoomLevel.Month:
                    result = "ThisMonthSelector".Tr ();
                    break;
                case ZoomLevel.Year:
                    result = "ThisYearSelector".Tr ();
                    break;
                }
            } else if (backDate == -1) {
                switch (ZoomLevel) {
                case ZoomLevel.Week:
                    result = "LastWeekSelector".Tr ();
                    break;
                case ZoomLevel.Month:
                    result = "LastMonthSelector".Tr ();
                    break;
                case ZoomLevel.Year:
                    result = "LastYearSelector".Tr ();
                    break;
                }
            } else {
                var startDate = dataSource.ResolveStartDate (_timeSpaceIndex);
                var endDate = dataSource.ResolveEndDate (startDate);

                switch (ZoomLevel) {
                case ZoomLevel.Week:
                    if (startDate.Month == endDate.Month) {
                        result = startDate.ToString ("StartWeekInterval".Tr ()) + " - " + endDate.ToString ("EndWeekInterval".Tr ());
                    } else {
                        result = startDate.Day + "th " + startDate.ToString ("MMM") + " - " + endDate.Day + "th " + startDate.ToString ("MMM");
                    }
                    break;
                case ZoomLevel.Month:
                    result = startDate.ToString ("MonthInterval".Tr ());
                    break;
                case ZoomLevel.Year:
                    result = startDate.ToString ("YearInterval".Tr ());
                    break;
                }
            }
            return result;
        }

        internal class TopBorder : UIView
        {
            public TopBorder ( RectangleF frame) : base ( frame)
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