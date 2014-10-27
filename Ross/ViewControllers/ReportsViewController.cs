using System.Drawing;
using GoogleAnalytics.iOS;
using MonoTouch.UIKit;
using Toggl.Phoebe.Data;
using Toggl.Phoebe.Data.Reports;
using XPlatUtils;
using Toggl.Ross.Theme;
using Toggl.Ross.Views.Charting;
using Toggl.Ross.Views;
using System.Diagnostics;

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
        private DonutChartView pieChart;
        private BarChartView barChart;
        private bool _viewMoveOn;

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

            dateSelectorView = new DateSelectorView (new RectangleF (0, UIScreen.MainScreen.Bounds.Height - selectorHeight - navBarHeight, UIScreen.MainScreen.Bounds.Width, selectorHeight));
            dateSelectorView.LeftArrowPressed += (sender, e) => TimeSpaceIndex++;
            dateSelectorView.RightArrowPressed += (sender, e) => TimeSpaceIndex--;

            barChart = new BarChartView ( new RectangleF ( padding, padding/2, UIScreen.MainScreen.Bounds.Width - padding * 2, dateSelectorView.Frame.Y - 2 * selectorHeight));
            barChart.GoForwardInterval += (sender, e) => TimeSpaceIndex--;
            barChart.GoBackInterval += (sender, e) => TimeSpaceIndex++;

            pieChart = new DonutChartView (new RectangleF ( padding, barChart.Bounds.Height + padding, UIScreen.MainScreen.Bounds.Width - padding * 2, dateSelectorView.Frame.Y - padding));
            pieChart.GoForwardInterval += (sender, e) => TimeSpaceIndex--;
            pieChart.GoBackInterval += (sender, e) => TimeSpaceIndex++;
            pieChart.ChangeView += (sender, e) => ChangeView ((UISwipeGestureRecognizer)sender);

            Add (barChart);
            Add (pieChart);
            Add (dateSelectorView);

            var upGesture = new UISwipeGestureRecognizer (ChangeView) { Direction = UISwipeGestureRecognizerDirection.Up };
            var downGesture = new UISwipeGestureRecognizer (ChangeView) { Direction = UISwipeGestureRecognizerDirection.Down };
            View.AddGestureRecognizer (upGesture);
            View.AddGestureRecognizer (downGesture);

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

        private void ChangeView ( UISwipeGestureRecognizer recognizer)
        {
            if (_viewMoveOn) {
                return;
            }

            if (recognizer.Direction == UISwipeGestureRecognizerDirection.Up) {
                UIView.Animate (0.5, 0, UIViewAnimationOptions.CurveEaseInOut,
                () => {
                    _viewMoveOn = true;
                    barChart.Frame = new RectangleF ( barChart.Frame.X, - barChart.Frame.Height, barChart.Frame.Width, barChart.Frame.Height);
                    pieChart.Frame = new RectangleF ( pieChart.Frame.X, padding, pieChart.Frame.Width, pieChart.Frame.Height);
                },() => {
                    _viewMoveOn = false;
                });
            } else {
                UIView.Animate (0.5, 0, UIViewAnimationOptions.CurveEaseInOut,
                () => {
                    _viewMoveOn = true;
                    barChart.Frame = new RectangleF ( barChart.Frame.X, padding/2, barChart.Frame.Width, barChart.Frame.Height);
                    pieChart.Frame = new RectangleF ( pieChart.Frame.X, barChart.Bounds.Height + padding, pieChart.Frame.Width, pieChart.Frame.Height);
                },() => {
                    _viewMoveOn = false;
                });
            }
        }

        private async void ChangeReportState ()
        {
            dataSource.Period = _zoomLevel;
            dateSelectorView.DateContent = FormattedIntervalDate (_timeSpaceIndex);

            await dataSource.Load (_timeSpaceIndex);

            pieChart.ReportView =
                barChart.ReportView = dataSource;
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
