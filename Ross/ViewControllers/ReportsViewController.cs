using System.Diagnostics;
using System.Drawing;
using GoogleAnalytics.iOS;
using MonoTouch.UIKit;
using Toggl.Phoebe.Data;
using Toggl.Phoebe.Data.Reports;
using XPlatUtils;
using Toggl.Ross.Theme;
using Toggl.Ross.Views;
using Toggl.Ross.Views.Charting;

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
                _zoomLevel = value;
                TimeSpaceIndex = 0;
            }
        }

        private int _timeSpaceIndex;

        public int TimeSpaceIndex
        {
            get {
                return _timeSpaceIndex;
            } set {
                _timeSpaceIndex = value;
                ChangeReportState ();
            }
        }

        private SummaryReportView dataSource;
        private ReportsMenuController menuController;
        private DateSelectorView dateSelectorView;
        private PieChart pieChart;


        public ReportsViewController ()
        {
            EdgesForExtendedLayout = UIRectEdge.None;
            menuController = new ReportsMenuController ();

            dataSource = new SummaryReportView ();
            _zoomLevel = ZoomLevel.Week;
            _timeSpaceIndex = 0;
        }

        protected override void Dispose (bool disposing)
        {
            if (disposing) {
                if (menuController != null) {
                    menuController.Detach ();
                    menuController = null;
                }
            }

            base.Dispose (disposing);
        }

        public override void ViewDidLoad ()
        {
            base.ViewDidLoad ();

            View.BackgroundColor = UIColor.White;

            menuController.Attach (this);

            float navBarHeight = 64;
            float selectorHeight = 50;

            dateSelectorView = new DateSelectorView (new RectangleF (0, UIScreen.MainScreen.Bounds.Height - selectorHeight - navBarHeight, UIScreen.MainScreen.Bounds.Width, selectorHeight));
            dateSelectorView.LeftArrowPressed += (sender, e) => TimeSpaceIndex++;
            dateSelectorView.RightArrowPressed += (sender, e) => TimeSpaceIndex--;
            Add (dateSelectorView);

            float padding = 24;
            pieChart = new PieChart (new RectangleF ( padding, 0, UIScreen.MainScreen.Bounds.Width - padding * 2, dateSelectorView.Frame.Y - padding));
            Add (pieChart);

            ChangeReportState ();
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

        public override void ViewWillAppear (bool animated)
        {
            base.ViewWillAppear (animated);
        }

        private async void ChangeReportState ()
        {
            dataSource.Period = _zoomLevel;
            dateSelectorView.DateContent = FormattedIntervalDate (_timeSpaceIndex);

            Debug.WriteLine (_timeSpaceIndex);

            await dataSource.Load (_timeSpaceIndex);
            pieChart.ReportView = dataSource;
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
            } else if (backDate == 1) {
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
    }
}
