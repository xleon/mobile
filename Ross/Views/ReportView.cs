using System.Drawing;
using MonoTouch.UIKit;
using Toggl.Phoebe.Data;
using Toggl.Phoebe.Data.Reports;
using Toggl.Ross.Views.Charting;

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

        public bool IsClean
        {
            get;
            set;
        }

        private ChartPosition _position;

        public ChartPosition Position
        {
            get {
                return _position;
            } set {
                if (_position == value) { return; }
                _position = value;
                //var posY = ( _position == ChartPosition.Top) ? topY : downY;
                //_containerView.Center = new PointF (_containerView.Center.X, posY);
            }
        }

        private bool _isDragging;

        public bool Dragging
        {
            get { return _isDragging; }
        }

        private bool _scrollEnabled;

        public bool ScrollEnabled
        {
            set {
                _scrollEnabled = value;
                if (panGesture != null) {
                    panGesture.Enabled = _scrollEnabled;
                }
            }
        }

        public ReportView ()
        {
            InitView();
        }

        public ReportView ( RectangleF frame) : base ( frame)
        {
            InitView();
        }

        private void InitView()
        {
            ClipsToBounds = true;
            BackgroundColor = UIColor.White;

            barChart = new BarChartView ();
            pieChart = new DonutChartView ();

            _containerView = new UIView ();
            _containerView.Add (barChart);
            _containerView.Add (pieChart);
            AddSubview (_containerView);

            IsClean = true;
            panGesture = CreatePanGesture ();
            _containerView.AddGestureRecognizer (panGesture);
            _position = ChartPosition.Top;
        }

        UIPanGestureRecognizer panGesture;
        private UIView _containerView;
        private DonutChartView pieChart;
        private BarChartView barChart;
        private SummaryReportView dataSource;
        private bool _loading;
        private float topY;
        private float downY;

        const float padding = 30;
        const float navBarHeight = 64;
        const float selectorHeight = 60;

        public override void LayoutSubviews ()
        {
            base.LayoutSubviews ();

            _containerView.Bounds = new RectangleF ( 0, 0, Bounds.Width, Bounds.Height * 2);
            barChart.Frame = new RectangleF ( padding/2, padding/2, Bounds.Width - padding, Bounds.Height - padding - selectorHeight );
            pieChart.Frame = new RectangleF (padding/2, barChart.Bounds.Height + padding, Bounds.Width - padding, Bounds.Height);
            topY = _containerView.Bounds.Height/2;
            downY = _containerView.Bounds.Height/2 - (barChart.Bounds.Height + padding);
            var posY = ( _position == ChartPosition.Top) ? topY : downY;
            _containerView.Center = new PointF ( Bounds.Width/2, posY);
        }

        public async void LoadData()
        {
            if ( IsClean) {
                _loading = true;
                dataSource = new SummaryReportView ();
                dataSource.Period = ZoomLevel;
                await dataSource.Load (TimeSpaceIndex);
                _loading = false;

                if (dataSource.Activity != null) {
                    barChart.ReportView = dataSource;
                    pieChart.ReportView  = dataSource;
                    IsClean = false;
                }
            }
        }

        public void StopReloadData()
        {
            if (_loading) {
                dataSource.CancelLoad ();
                IsClean = true;
                _loading = false;
            }
        }

        protected override void Dispose (bool disposing)
        {
            barChart.Dispose ();
            pieChart.Dispose ();
            base.Dispose (disposing);
        }

        public enum ChartPosition {
            Top = 0,
            Down = 1
        }

        private UIPanGestureRecognizer CreatePanGesture()
        {
            UIPanGestureRecognizer result;
            float dy = 0;
            const float navX = 70;

            result = new UIPanGestureRecognizer (pg => {
                if ((pg.State == UIGestureRecognizerState.Began || pg.State == UIGestureRecognizerState.Changed) && (pg.NumberOfTouches == 1)) {

                    _isDragging = true;

                    var p0 = pg.LocationInView (this);
                    var currentY = (_position == ChartPosition.Top) ? topY : downY;

                    if (dy == 0) {
                        dy = p0.Y - currentY;
                    }

                    var p1 = new PointF ( _containerView.Center.X, p0.Y - dy);
                    if ( p1.Y > topY || p1.Y < downY) { return; }
                    _containerView.Center = p1;

                } else if (pg.State == UIGestureRecognizerState.Ended) {

                    float newY;
                    ChartPosition newPosition;

                    if ( _position == ChartPosition.Top && _containerView.Center.Y <= topY - navX) {
                        newPosition = ChartPosition.Down;
                        newY = downY;
                    } else if ( _position == ChartPosition.Down && _containerView.Center.Y >= downY + navX) {
                        newPosition = ChartPosition.Top;
                        newY = topY;
                    } else {
                        newPosition = _position;
                        newY = (_position == ChartPosition.Top) ? topY : downY;
                    }

                    UIView.Animate (0.3, 0, UIViewAnimationOptions.CurveEaseOut,
                    () => {
                        _containerView.Center = new PointF ( _containerView.Center.X, newY);
                    },() => {
                        _isDragging = false;
                        _position = newPosition;
                    });
                    dy = 0;
                }
            });

            return result;
        }
    }
}