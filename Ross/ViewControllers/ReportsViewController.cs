using System;
using System.Drawing;
using GoogleAnalytics.iOS;
using MonoTouch.UIKit;
using Toggl.Phoebe.Data;
using Toggl.Phoebe.Data.Reports;
using XPlatUtils;
using Toggl.Ross.Theme;
using Toggl.Ross.Views;
using MonoTouch.CoreGraphics;

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

                centerView.ZoomLevel =
                    leftView.ZoomLevel =
                        rightView.ZoomLevel = _zoomLevel;
                ChangeReportState ();
            }
        }

        private ReportsMenuController menuController;
        private DateSelectorView dateSelectorView;
        private ReportsView centerView;
        private ReportsView rightView;
        private ReportsView leftView;
        private TopBorder topBorder;
        private SummaryReportView dataSource;
        private bool _viewMoveOn;
        private UIPanGestureRecognizer panGesture;
        private ChartPosition _position;
        private PointF _centerPos;
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
            _position = ChartPosition.Top;
        }

        public override void ViewWillDisappear (bool animated)
        {
            leftView.Hidden = true;
            rightView.Hidden = true;
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
            DisposeReportViewAt (Side.Center);
            DisposeReportViewAt (Side.Left);
            DisposeReportViewAt (Side.Right);
            panGesture.Dispose ();
        }

        public override void ViewDidLoad ()
        {
            base.ViewDidLoad ();

            View.BackgroundColor = UIColor.White;

            menuController.Attach (this);

            topBorder = new TopBorder (new RectangleF (0.0f, 0.0f, UIScreen.MainScreen.Bounds.Width, 2.0f));
            dateSelectorView = new DateSelectorView (new RectangleF (0, UIScreen.MainScreen.Bounds.Height - selectorHeight - navBarHeight, UIScreen.MainScreen.Bounds.Width, selectorHeight));
            dateSelectorView.LeftArrowPressed += (sender, e) => {
                leftView.ReloadData();
                changeToViewAt (Side.Left);
            };
            dateSelectorView.RightArrowPressed += (sender, e) => {
                if ( _timeSpaceIndex >= 1) { return; }
                rightView.ReloadData();
                changeToViewAt (Side.Right);
            };

            centerView = createReportViewAt ();
            leftView = createReportViewAt ( Side.Left);
            rightView = createReportViewAt ( Side.Right);

            Add (centerView);
            Add (leftView);
            Add (rightView);
            Add (dateSelectorView);
            Add (topBorder);
            _centerPos = centerView.Center;

            panGesture = createPanGesture ();
            centerView.AddGestureRecognizer (panGesture);
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

        private void changeToViewAt ( Side side)
        {
            if (_viewMoveOn) {  return; }

            centerView.RemoveGestureRecognizer ( panGesture);

            if (side == Side.Right) {
                UIView.Animate (0.4, 0, UIViewAnimationOptions.CurveEaseOut,
                () => {
                    _viewMoveOn = true;
                    rightView.Center = new PointF ( UIScreen.MainScreen.Bounds.Width/2, rightView.Center.Y);
                    centerView.Center = new PointF ( -UIScreen.MainScreen.Bounds.Width/2, centerView.Center.Y);
                },() => {
                    _viewMoveOn = false;
                    DisposeReportViewAt ( Side.Left);
                    leftView = centerView;
                    centerView = rightView;
                    _timeSpaceIndex ++;
                    rightView = createReportViewAt ( Side.Right);
                    centerView.AddGestureRecognizer ( panGesture);
                    dateSelectorView.DateContent = FormattedIntervalDate ( centerView.TimeSpaceIndex);

                });
            } else {
                UIView.Animate (0.4, 0, UIViewAnimationOptions.CurveEaseOut,
                () => {
                    _viewMoveOn = true;
                    leftView.Center = new PointF ( UIScreen.MainScreen.Bounds.Width/2, leftView.Center.Y);
                    centerView.Center = new PointF ( UIScreen.MainScreen.Bounds.Width * 1.5f, centerView.Center.Y);
                },() => {
                    _viewMoveOn = false;
                    DisposeReportViewAt ( Side.Right);
                    rightView = centerView;
                    centerView = leftView;
                    _timeSpaceIndex --;
                    leftView = createReportViewAt ( Side.Left);
                    centerView.AddGestureRecognizer ( panGesture);
                    dateSelectorView.DateContent = FormattedIntervalDate ( centerView.TimeSpaceIndex);
                });
            }
        }

        private void changeChart ( ChartPosition position)
        {
            if (_viewMoveOn) {
                return;
            }

            if ( position == ChartPosition.Top) {
                UIView.Animate (0.2, 0, UIViewAnimationOptions.CurveEaseOut,
                () => {
                    _viewMoveOn = true;
                    centerView.Center = _centerPos;
                },() => {
                    _viewMoveOn = false;
                    leftView.Center = new PointF ( leftView.Center.X, _centerPos.Y);
                    rightView.Center = new PointF ( rightView.Center.X, _centerPos.Y);
                    _position = position;
                });
            } else {
                UIView.Animate (0.2, 0, UIViewAnimationOptions.CurveEaseOut,
                () => {
                    _viewMoveOn = true;
                    centerView.Center = new PointF ( centerView.Center.X, _centerPos.Y - centerView.BarChartHeight);
                },() => {
                    _viewMoveOn = false;
                    leftView.Center = new PointF ( leftView.Center.X, _centerPos.Y - centerView.BarChartHeight);
                    rightView.Center = new PointF ( rightView.Center.X, _centerPos.Y - centerView.BarChartHeight);
                    _position = position;
                });
            }
        }

        private void ChangeReportState ()
        {
            dataSource.Period = _zoomLevel;
            dateSelectorView.DateContent = FormattedIntervalDate (_timeSpaceIndex);
            centerView.ReloadData ();
        }

        private ReportsView createReportViewAt ( Side side = Side.Center)
        {
            var xOffset = UIScreen.MainScreen.Bounds.Width * (int)side;
            var result = new ReportsView ( new RectangleF ( xOffset + padding, padding / 2, UIScreen.MainScreen.Bounds.Width - padding * 2, dateSelectorView.Frame.Y));
            if (_position == ChartPosition.Down) {
                result.Center = new PointF (result.Center.X, result.Center.Y - result.BarChartHeight);
            }
            result.TimeSpaceIndex = _timeSpaceIndex + (int)side;
            result.ZoomLevel = _zoomLevel;

            View.Add (result);
            dateSelectorView.RemoveFromSuperview ();
            View.Add (dateSelectorView);
            topBorder.RemoveFromSuperview ();
            View.Add (topBorder);

            return result;
        }

        private void DisposeReportViewAt ( Side side)
        {
            ReportsView view;

            if (side == Side.Left) {
                view = leftView;
            } else if (side == Side.Right) {
                view = rightView;
            } else {
                view = centerView;
            }

            view.RemoveGestureRecognizer (panGesture);
            view.RemoveFromSuperview ();
            view.Dispose ();
        }

        private void snapViewToPoint ( PointF point)
        {
            UIView.Animate (0.5, 0, UIViewAnimationOptions.CurveEaseInOut,
            () => {
                _viewMoveOn = true;
                centerView.Center = point;
                leftView.Center = new PointF ( centerView.Center.X - UIScreen.MainScreen.Bounds.Width, centerView.Center.Y);
                rightView.Center = new PointF ( centerView.Center.X + UIScreen.MainScreen.Bounds.Width, centerView.Center.Y);
            },() => {
                _viewMoveOn = false;
            });
        }

        private UIPanGestureRecognizer createPanGesture()
        {
            UIPanGestureRecognizer result;
            float dx = 0;
            float dy = 0;
            const float navX = 70;
            const float loadX = 40;
            bool movingOnX = false;

            result = new UIPanGestureRecognizer (pg => {
                if ((pg.State == UIGestureRecognizerState.Began || pg.State == UIGestureRecognizerState.Changed) && (pg.NumberOfTouches == 1)) {

                    if (_viewMoveOn) { return; }

                    var p0 = pg.LocationInView (View);
                    if (dx == 0) {
                        dx = p0.X - centerView.Center.X;
                        movingOnX = Math.Abs ( pg.VelocityInView ( View).X) < Math.Abs ( pg.VelocityInView ( View).Y);
                    }
                    if (dy == 0) {
                        dy = p0.Y - centerView.Center.Y;
                    }

                    var currentY = (_position == ChartPosition.Top) ? _centerPos.Y : _centerPos.Y - centerView.BarChartHeight;
                    PointF p1 = (movingOnX) ? new PointF ( _centerPos.X, p0.Y - dy) : new PointF (p0.X - dx, currentY);

                    if ( p1.Y > _centerPos.Y || p1.Y < _centerPos.Y - centerView.BarChartHeight) { return; }

                    if ( p1.X < _centerPos.X && _timeSpaceIndex == 1) { return; }

                    centerView.Center = p1;
                    leftView.Center = new PointF ( p1.X - _centerPos.X * 2, p1.Y);
                    rightView.Center = new PointF ( p1.X + _centerPos.X * 2, p1.Y);

                    // reload or not!
                    /*
                    if ( centerView.Center.X <= _centerPos.X - loadX ) {
                        rightView.ReloadData();
                    } else if ( centerView.Center.X >= _centerPos.X + loadX  ) {
                        leftView.ReloadData();
                    }
                    */

                } else if (pg.State == UIGestureRecognizerState.Ended) {
                    if ( centerView.Center.X <= _centerPos.X - navX) {
                        if ( _timeSpaceIndex >= 1) { return; }
                        changeToViewAt ( Side.Right);
                    } else if ( centerView.Center.X >= _centerPos.X + navX) {
                        changeToViewAt ( Side.Left);
                    } else if ( _position == ChartPosition.Top && centerView.Center.Y <= _centerPos.Y - navX) {
                        changeChart ( ChartPosition.Down);
                    } else if ( _position == ChartPosition.Down && centerView.Center.Y >= _centerPos.Y - centerView.BarChartHeight + navX) {
                        changeChart ( ChartPosition.Top);
                    } else {
                        var currentY = (_position == ChartPosition.Top) ? _centerPos.Y : _centerPos.Y - centerView.BarChartHeight;
                        snapViewToPoint (new PointF ( _centerPos.X, currentY));
                    }
                    dx = 0;
                    dy = 0;
                    movingOnX = false;
                }
            });

            return result;
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

        enum Side {
            Center = 0,
            Left = -1,
            Right = 1
        }

        enum ChartPosition {
            Top,
            Down
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