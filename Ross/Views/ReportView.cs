using System.Drawing;
using MonoTouch.UIKit;
using Toggl.Phoebe.Data;
using Toggl.Phoebe.Data.Reports;
using Toggl.Ross.Views.Charting;
using System.Diagnostics;

namespace Toggl.Ross.Views
{
    public sealed class ReportView : UIView
    {
        public ZoomLevel ZoomLevel
        {
            get;
            set;
        }

        public int TimeSpaceIndex
        {
            get;
            set;
        }

        public float BarChartHeight
        {
            get { return barChart.Frame.Height + padding; }
        }

        public bool Clean
        {
            get;
            set;
        }

        public ReportView ( RectangleF frame) : base ( frame)
        {
            Frame = new RectangleF (frame.X, frame.Y, frame.Width, frame.Height * 2);
            BackgroundColor = UIColor.White;
            barChart = new BarChartView ( new RectangleF ( padding, padding, UIScreen.MainScreen.Bounds.Width - padding * 2, frame.Height - 2 * selectorHeight));
            pieChart = new DonutChartView (new RectangleF ( 0.0f, barChart.Bounds.Height + padding, frame.Width, frame.Height));
            dragHelper = new UIView (frame);
            dragHelper.Add (barChart);
            dragHelper.Add (pieChart);
            Clean = true;
        }

        private UIView dragHelper;
        private DonutChartView pieChart;
        private BarChartView barChart;
        private SummaryReportView dataSource;
        private bool _loading;
        private bool _viewMoveOn;

        const float padding = 12;
        const float navBarHeight = 64;
        const float selectorHeight = 50;

        public override void LayoutSubviews ()
        {
            base.LayoutSubviews ();
            barChart.Frame = new RectangleF ( padding, padding, UIScreen.MainScreen.Bounds.Width - padding * 2, Frame.Height - 2 * selectorHeight);
            pieChart.Frame = new RectangleF ( padding, barChart.Bounds.Height + padding, UIScreen.MainScreen.Bounds.Width - padding * 2, Frame.Height);
        }

        public async void LoadData()
        {
            if ( Clean) {

                _loading = true;
                dataSource = new SummaryReportView ();
                dataSource.Period = ZoomLevel;
                await dataSource.Load (TimeSpaceIndex);
                _loading = false;

                if (dataSource.Activity != null) {
                    barChart.ReportView = dataSource;
                    pieChart.ReportView  = dataSource;
                    Clean = false;
                }
            }
        }

        public void StopReloadData()
        {
            Debug.WriteLine ("Stop Reload Data");
            if (_loading) {
                dataSource.CancelLoad ();
                Clean = true;
                _loading = false;
            }
        }

        protected override void Dispose (bool disposing)
        {
            barChart.Dispose ();
            pieChart.Dispose ();
            base.Dispose (disposing);
        }

        /*
        private void changeChart ( ChartPosition position)
        {
            if (_viewMoveOn) {
                return;
            }

            if ( position == ChartPosition.Top) {
                UIView.Animate (0.4, 0, UIViewAnimationOptions.CurveEaseOut,
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
                UIView.Animate (0.4, 0, UIViewAnimationOptions.CurveEaseOut,
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

                } else if (pg.State == UIGestureRecognizerState.Ended) {
                    if ( _position == ChartPosition.Top && centerView.Center.Y <= _centerPos.Y - navX) {
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
         */
    }
}