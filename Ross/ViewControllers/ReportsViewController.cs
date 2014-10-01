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

            dateSelectorView = new DateSelectorView (new RectangleF (0, UIScreen.MainScreen.Bounds.Height - 50 - 64, UIScreen.MainScreen.Bounds.Width, 50));
            dateSelectorView.LeftArrowPressed += (sender, e) => TimeSpaceIndex++;
            dateSelectorView.RightArrowPressed += (sender, e) => TimeSpaceIndex--;
            Add (dateSelectorView);

            pieChart = new PieChart (new RectangleF (0, 0, UIScreen.MainScreen.Bounds.Width, 420));
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

            if (dataSource != null) {
                // reportViewProvider.Updated += OnTagsUpdated;
            }
            //RebindTags ();
        }

        private async void ChangeReportState ()
        {
            dataSource.Period = _zoomLevel;
            dateSelectorView.DateContent = dataSource.FormattedStartDate (_timeSpaceIndex) + " " + dataSource.FormattedEndDate (_timeSpaceIndex);

            Debug.WriteLine (_timeSpaceIndex);

            await dataSource.Load (_timeSpaceIndex);
            pieChart.ReportView = dataSource;
        }
    }
}
