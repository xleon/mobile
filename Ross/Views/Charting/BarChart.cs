using System;
using CoreAnimation;
using CoreGraphics;
using Foundation;
using Toggl.Ross.Theme;
using UIKit;

namespace Toggl.Ross.Views.Charting
{

    public interface IBarChartDataSource
    {
        int NumberOfBarsOnChart (BarChart barChart);

        nfloat ValueForBarAtIndex (BarChart barChart, int index);

        nfloat ValueForSecondaryBarAtIndex ( BarChart barChart, int index);

        string TextForBarAtIndex (BarChart barChart, int index);

        string TimeIntervalAtIndex (int index);

        string TimeForBarAtIndex (int index);
    }

    public class BarChart : UIView
    {
        public IBarChartDataSource DataSource { get; set; }

        public nfloat AnimationSpeed { get; set; }

        public UIFont LabelFont { get; set; }

        public UIColor LabelColor { get; set; }

        public UIColor MainBarColor { get; set; }

        public UIColor SecondaryBarColor { get; set; }

        public BarChart ()
        {
            _barChartView = new UIView ();
            Add (_barChartView);

            LabelFont = UIFont.SystemFontOfSize (10.0f);
            LabelColor = UIColor.LightGray;
            MainBarColor = Color.TimeBarColor;
            SecondaryBarColor = Color.MoneyBarColor;
            AnimationSpeed = 0.5f;
            BackgroundColor = UIColor.White;
        }

        private UIView _barChartView;
        private CATextLayer[] xAxisText = new CATextLayer[5];
        private nint lastZoomedIndex;

        static readonly nint defaultSliceZOrder = 100;
        static readonly nfloat minBarScale = 0.005f;
        static readonly nfloat xAxisMargin = 35;
        static readonly nfloat yAxisMargin = 20;
        static readonly nfloat topBarMargin = 10;
        static readonly nfloat xTextMargin = 35;

        public override void LayoutSubviews ()
        {
            base.LayoutSubviews ();
            _barChartView.Frame = Bounds;
        }

        public override void Draw (CGRect rect)
        {
            base.Draw (rect);

            using (var context = UIGraphics.GetCurrentContext ()) {

                UIBezierPath mainAxis = UIBezierPath.FromRoundedRect ( new CGRect (xAxisMargin, 0.0f, 2.0f, rect.Height), 2.0f);
                UIColor.FromRGB ( 203, 203, 203).SetFill();
                mainAxis.Fill();

                nfloat sepInterval = (nfloat)Math.Floor ((rect.Width - xAxisMargin - xTextMargin)/ 5);
                for (int i = 1; i < 6; i++) {
                    var separatorAxis = new UIBezierPath();
                    separatorAxis.MoveTo (new CGPoint (xAxisMargin + sepInterval * i, 0));
                    separatorAxis.AddLineTo (new CGPoint ( xAxisMargin + sepInterval * i, rect.Height - yAxisMargin));
                    UIColor.FromRGB ( 203, 203, 203).SetStroke();
                    separatorAxis.LineWidth = 1.0f;
                    separatorAxis.SetLineDash ( new nfloat[] {1.0f,1.0f}, 1);
                    separatorAxis.Stroke ();

                    var textLayer = new CATextLayer ();
                    textLayer.ContentsScale = UIScreen.MainScreen.Scale;
                    CGFont font = CGFont.CreateWithFontName (LabelFont.Name);

                    if (font != null) {
                        textLayer.SetFont (font);
                        font.Dispose ();
                    }

                    textLayer.FontSize = LabelFont.PointSize;
                    textLayer.AnchorPoint = new CGPoint ( 0.5f, 0.0f);
                    textLayer.AlignmentMode = CATextLayer.AlignmentCenter;
                    textLayer.BackgroundColor = UIColor.Clear.CGColor;
                    textLayer.ForegroundColor = LabelColor.CGColor;

                    CGSize size = ((NSString)"0000 h").StringSize (LabelFont);
                    textLayer.String = "00 h";
                    textLayer.Bounds = new CGRect (0, 0, size.Width, size.Height);
                    textLayer.Position = new CGPoint ( xAxisMargin + sepInterval * i, rect.Height - yAxisMargin + 5.0f);
                    Layer.AddSublayer (textLayer);
                    xAxisText [i - 1] = textLayer;
                }
            }
        }

        public void ReloadData()
        {
            if (DataSource == null) {
                return;
            }

            CALayer parentLayer = _barChartView.Layer;
            var barsCount = DataSource.NumberOfBarsOnChart (this);

            for (int i = 0; i < xAxisText.Length; i++) {
                xAxisText [i].String = DataSource.TimeIntervalAtIndex (i);
            }

            _barChartView.UserInteractionEnabled = false;
            nfloat barHeight = (Bounds.Height - topBarMargin - yAxisMargin) / barsCount;
            nfloat padding = 1.0f;
            nfloat initialY = barHeight / 2 + topBarMargin;

            for (int i = 0; i < barsCount; i++) {
                BarLayer oneLayer = CreateBarLayer (barHeight - padding);
                parentLayer.AddSublayer (oneLayer);
                oneLayer.Position = new CGPoint ( 0, initialY + i * barHeight);
                oneLayer.TimeValue = DataSource.ValueForBarAtIndex (this, i);
                oneLayer.MoneyValue = DataSource.ValueForSecondaryBarAtIndex (this, i);

                var timeTextLayer = (CATextLayer)oneLayer.Sublayers [BarLayer.TimeTextIndex];
                timeTextLayer.String = DataSource.TimeForBarAtIndex (i);
                timeTextLayer.Hidden = (string.Compare (timeTextLayer.String, "0.00", StringComparison.Ordinal) == 0);
                timeTextLayer.Position = new CGPoint ( timeTextLayer.Position.X, oneLayer.Bounds.Height/2);
                timeTextLayer.FontSize = (barsCount > 12) ? 9.0f : 10.0f;

                var nsString = new NSString ( DataSource.TextForBarAtIndex (this, i));
                CGSize size = nsString.GetSizeUsingAttributes (new UIStringAttributes () { Font = LabelFont });
                var textLayer = (CATextLayer)oneLayer.Sublayers [BarLayer.DateTextIndex];
                textLayer.String = DataSource.TextForBarAtIndex (this, i);
                textLayer.FontSize = (barsCount > 12) ? 9.0f : 10.0f;
                textLayer.Bounds = new CGRect (0, 0, size.Width, size.Height);
                textLayer.Position = new CGPoint ( 0.0f, oneLayer.Bounds.Height/2);

                if (barsCount > 12) {
                    timeTextLayer.Opacity = 0.0f;
                    textLayer.Opacity = (i % 3 == 0) ? 1.0f : 0.0f;
                } else {
                    timeTextLayer.Opacity = (string.Compare (timeTextLayer.String, "0.00", StringComparison.Ordinal) == 0) ? 0.0f : 1.0f;
                }
            }
            _barChartView.UserInteractionEnabled = true;
        }

        private BarLayer CreateBarLayer ( nfloat barHeight)
        {
            var barLayer = new BarLayer () {
                ZPosition = 0,
                BorderColor = UIColor.White.CGColor,
                AnchorPoint = new CGPoint ( 0.0f, 0.5f),
                Frame = new CGRect ( 0, 0, Bounds.Width, barHeight)
            };

            var mainBar = new CALayer ();
            mainBar.AnchorPoint = new CGPoint (0.0f, 0.0f);
            mainBar.Bounds = new CGRect ( 0, 0, Bounds.Width - xAxisMargin - xTextMargin, barHeight);
            mainBar.Position = new CGPoint ( xAxisMargin, 0);
            mainBar.BackgroundColor = MainBarColor.CGColor;
            mainBar.SetValueForKeyPath ( new NSNumber ( minBarScale), new NSString ( "transform.scale.x"));
            barLayer.AddSublayer (mainBar);

            var secondaryBar = new CALayer ();
            secondaryBar.AnchorPoint = new CGPoint (0.0f, 0.0f);
            secondaryBar.Bounds = new CGRect ( 0, 0, Bounds.Width - xAxisMargin - xTextMargin, barHeight);
            secondaryBar.Position = new CGPoint ( xAxisMargin, 0);
            secondaryBar.BackgroundColor = SecondaryBarColor.CGColor;
            secondaryBar.SetValueForKeyPath ( new NSNumber ( minBarScale), new NSString ( "transform.scale.x"));
            barLayer.AddSublayer (secondaryBar);

            var emptyBar = new CALayer ();
            emptyBar.AnchorPoint = new CGPoint (0.0f, 0.0f);
            emptyBar.Bounds = new CGRect ( 0, 0, 2.0f, barHeight);
            emptyBar.Position = new CGPoint ( xAxisMargin, 0);
            emptyBar.BackgroundColor = UIColor.Gray.CGColor;
            barLayer.AddSublayer (emptyBar);

            CGFont font = CGFont.CreateWithFontName (LabelFont.Name);
            CGSize size = ((NSString)"00.00:00").StringSize (LabelFont);

            var textLayer = new CATextLayer () {
                ContentsScale = UIScreen.MainScreen.Scale,
                FontSize = 10.0f,
                AnchorPoint = new CGPoint (0.0f, 0.5f),
                AlignmentMode = CATextLayer.AlignmentLeft,
                BackgroundColor = UIColor.Clear.CGColor,
                ForegroundColor = LabelColor.CGColor
            };
            barLayer.AddSublayer (textLayer);

            var timeTextLayer = new CATextLayer () {
                ContentsScale = UIScreen.MainScreen.Scale,
                FontSize = 10.0f,
                AnchorPoint = new CGPoint (0.0f, 0.5f),
                AlignmentMode = CATextLayer.AlignmentLeft,
                BackgroundColor = UIColor.Clear.CGColor,
                ForegroundColor = SecondaryBarColor.CGColor
            };
            barLayer.AddSublayer (timeTextLayer);

            if (font != null) {
                textLayer.SetFont (font);
                timeTextLayer.SetFont (font);
                font.Dispose ();
            }

            CATransaction.DisableActions = true;
            textLayer.Bounds = new CGRect (new CGPoint (0, 0), size);
            textLayer.Position = mainBar.Position;
            timeTextLayer.Bounds = new CGRect (new CGPoint (0, 0), size);
            timeTextLayer.Position = new CGPoint ( mainBar.Position.X + 5.0f, mainBar.Position.Y);
            CATransaction.DisableActions = false;

            return barLayer;
        }

        #region Touch Handing (Selection Notification)

        public override void TouchesBegan (NSSet touches, UIEvent evt)
        {

        }

        public override void TouchesMoved (NSSet touches, UIEvent evt)
        {
            ZoomOut ();
        }

        public override void TouchesEnded (NSSet touches, UIEvent evt)
        {
            ZoomIn ( touches);
        }

        public override void TouchesCancelled (NSSet touches, UIEvent evt)
        {
            ZoomOut ();
        }

        private int GetSelectedBarChart ( CGPoint point)
        {
            CALayer parentLayer = _barChartView.Layer;
            var barLayers = parentLayer.Sublayers ?? new CALayer[0];
            int idx = 0;
            int selectedIndex = -1;

            foreach (BarLayer item in barLayers) {
                if (item.Contains ( _barChartView.Layer.ConvertPointToLayer (point, item) )) {
                    item.ZPosition = nfloat.MaxValue;
                    selectedIndex = idx;
                } else {
                    item.ZPosition = defaultSliceZOrder;
                }
                idx++;
            }
            return selectedIndex;
        }

        private void ZoomIn ( NSSet touches)
        {
            CALayer parentLayer = _barChartView.Layer;
            var barLayers = parentLayer.Sublayers ?? new CALayer[0];
            if (barLayers.Length <= 12) {
                return;
            }

            // detect touched layer
            var touch = (UITouch)touches.AnyObject;
            CGPoint point = touch.LocationInView (_barChartView);
            var index = GetSelectedBarChart (point);

            if (index == -1 || ( index >= lastZoomedIndex - 1 && index <= lastZoomedIndex + 1)) {
                ZoomOut ();
                return;
            }

            var currentHeight = barLayers [0].Bounds.Height;
            var totalHeight = Frame.Height - topBarMargin - yAxisMargin;
            nfloat posY = 0.0f;

            nfloat padding = 1.0f;
            nfloat maxBarHeight = 35f;
            nint zoomCount = 3;
            nfloat initialY = barLayers[0].Position.Y;

            var minHeight = (totalHeight - maxBarHeight * zoomCount) / (barLayers.Length - zoomCount);
            nfloat barHeight;

            for (int i = 0; i < barLayers.Length; i++) {
                var oneLayer = (BarLayer)barLayers [i];
                if ((index == barLayers.Length - 1 && i > barLayers.Length - 1 - zoomCount) ||
                        (i >= index - 1 && i <= index + 1) ||
                        (index == 0 && i < zoomCount)) {
                    barHeight = maxBarHeight;
                    oneLayer.Sublayers[ BarLayer.TimeTextIndex].Opacity = 1.0f;
                    oneLayer.Sublayers [BarLayer.DateTextIndex].Opacity = 1.0f;
                } else {
                    barHeight = minHeight;
                    oneLayer.Sublayers[ BarLayer.TimeTextIndex].Opacity = 0.0f;
                    oneLayer.Sublayers[ BarLayer.DateTextIndex].Opacity = (i % 3 == 0) ? 1.0f : 0.0f;
                }

                oneLayer.CreateBarAnimationForHeight ( oneLayer.Sublayers[ BarLayer.MainBarIndex], barHeight - padding, null);
                oneLayer.CreateBarAnimationForHeight ( oneLayer.Sublayers[ BarLayer.SecondaryBarIndex], barHeight - padding, null);
                oneLayer.CreateBarAnimationForHeight ( oneLayer.Sublayers[ BarLayer.EmptyBarIndex], barHeight - padding, null);
                oneLayer.Sublayers[ BarLayer.DateTextIndex].Position = new CGPoint ( oneLayer.Sublayers[ BarLayer.DateTextIndex].Position.X, ( barHeight - padding)/2);
                oneLayer.Sublayers[ BarLayer.TimeTextIndex].Position = new CGPoint ( oneLayer.Sublayers[ BarLayer.TimeTextIndex].Position.X, ( barHeight - padding)/2);
                oneLayer.Position = new CGPoint ( 0, initialY + posY);
                posY += barHeight;
            }

            lastZoomedIndex = index;
        }

        private void ZoomOut()
        {
            CALayer parentLayer = _barChartView.Layer;
            var barLayers = parentLayer.Sublayers ?? new CALayer[0];
            if (barLayers.Length <= 12) {
                return;
            }

            nfloat barHeight = (Frame.Height - topBarMargin - yAxisMargin) / barLayers.Length;
            nfloat padding = 1.0f;
            nfloat initialY = barHeight / 2 + topBarMargin;

            for (int i = 0; i < barLayers.Length; i++) {
                var oneLayer = (BarLayer)barLayers [i];
                oneLayer.CreateBarAnimationForHeight ( oneLayer.Sublayers[ BarLayer.MainBarIndex], barHeight - padding, null);
                oneLayer.CreateBarAnimationForHeight ( oneLayer.Sublayers[ BarLayer.SecondaryBarIndex], barHeight - padding, null);
                oneLayer.CreateBarAnimationForHeight ( oneLayer.Sublayers[ BarLayer.EmptyBarIndex], barHeight - padding, null);
                oneLayer.Sublayers[ BarLayer.DateTextIndex].Position = new CGPoint ( oneLayer.Sublayers[ BarLayer.DateTextIndex].Position.X, ( barHeight - padding)/2);
                oneLayer.Sublayers[ BarLayer.TimeTextIndex].Position = new CGPoint ( oneLayer.Sublayers[ BarLayer.TimeTextIndex].Position.X, ( barHeight - padding)/2);
                oneLayer.Sublayers[ BarLayer.TimeTextIndex].Opacity = 0.0f;
                oneLayer.Sublayers[ BarLayer.DateTextIndex].Opacity = (i % 3 == 0) ? 1.0f : 0.0f;
                oneLayer.Position = new CGPoint ( 0, initialY + i * barHeight);
            }
            lastZoomedIndex = -1;
        }

        #endregion
    }

    sealed class BarLayer : CALayer
    {
        public const int MainBarIndex = 0;
        public const int SecondaryBarIndex = 1;
        public const int EmptyBarIndex = 2;
        public const int DateTextIndex = 3;
        public const int TimeTextIndex = 4;

        private nfloat _timeValue;

        [Export ("timeValue")]
        public nfloat TimeValue
        {
            get {
                return _timeValue;
            }

            set {
                if (_timeValue.CompareTo (value) == 0) {
                    return;
                }
                _timeValue = value;
                var mainBar = Sublayers [0];
                if (mainBar != null ) {
                    var xScale = (_timeValue > minBarScale) ? _timeValue : minBarScale;
                    CreateBarAnimationForKeyPath (mainBar, "transform.scale.x", xScale );
                    CreateBarAnimationForKeyPath (Sublayers [TimeTextIndex], "position.x", mainBar.Bounds.Width * xScale + 5.0f + mainBar.Position.X );
                }
                Sublayers [EmptyBarIndex].Opacity = (_timeValue > 0) ? 0.0f : 1.0f;
            }
        }

        private nfloat _moneyValue;

        [Export ("moneyValue")]
        public nfloat MoneyValue
        {
            get {
                return _moneyValue;
            }

            set {
                if (_moneyValue.CompareTo (value) == 0) {
                    return;
                }
                _moneyValue = value;
                var secondaryBar = Sublayers [SecondaryBarIndex];
                if (secondaryBar != null ) {
                    var xScale = (_moneyValue > minBarScale) ? _moneyValue : minBarScale;
                    CreateBarAnimationForKeyPath (secondaryBar, "transform.scale.x", xScale);
                }
                if ( _moneyValue > _timeValue && _moneyValue > 0) { // TODO: weird case where _moneyValue > _timeValue!
                    Sublayers [EmptyBarIndex].Opacity = (value > 0) ? 0.0f : 1.0f;
                }
            }
        }

        readonly static nfloat minBarScale = 0.005f;

        public BarLayer ()
        {
        }

        public BarLayer (IntPtr ptr) : base (ptr)
        {
        }


        [Export ("initWithLayer:")]
        public BarLayer (CALayer other) : base (other)
        {
        }

        public override void Clone (CALayer other)
        {
            base.Clone (other);
            var _other = (BarLayer) other;
            if (_other != null) {
                MoneyValue = _other.MoneyValue;
                TimeValue = _other.TimeValue;
            }
        }

        [Export ("needsDisplayForKey:")]
        static bool NeedsDisplayForKey (NSString key)
        {
            switch (key.ToString ()) {
            case "timeValue":
                return true;
            case "moneyValue":
                return true;
            default:
                return CALayer.NeedsDisplayForKey (key);
            }
        }

        public void CreateBarAnimationForKeyPath ( CALayer layer, string key, nfloat toValue )
        {
            var _fromValue =  layer.ValueForKeyPath ( new NSString ( key));
            var _toValue = new NSNumber (toValue);
            var _key = new NSString (key);

            var barAnimation = CABasicAnimation.FromKeyPath (key);
            var currentValue = _fromValue;
            if (layer.PresentationLayer != null) {
                currentValue = layer.PresentationLayer.ValueForKeyPath (_key);
            }
            barAnimation.From = currentValue;
            barAnimation.To = _toValue;
            barAnimation.TimingFunction = CAMediaTimingFunction.FromName (CAMediaTimingFunction.Default);
            layer.AddAnimation (barAnimation, key);
            layer.SetValueForKeyPath (_toValue, _key);
        }

        public void CreateBarAnimationForHeight ( CALayer layer, nfloat toValue, CAAnimationDelegate @delegate)
        {
            const string key = "bounds.size.height";
            var _fromValue =  layer.ValueForKeyPath ( new NSString ( key));
            var _toValue = new NSNumber (toValue);
            var _key = new NSString (key);

            var barAnimation = CABasicAnimation.FromKeyPath (key);
            var currentValue = _fromValue;
            if (layer.PresentationLayer != null) {
                currentValue = layer.PresentationLayer.ValueForKeyPath (_key);
            }
            barAnimation.From = currentValue;
            barAnimation.To = _toValue;
            barAnimation.Delegate = @delegate;
            barAnimation.TimingFunction = CAMediaTimingFunction.FromName (CAMediaTimingFunction.Default);
            layer.AddAnimation (barAnimation, key);
            layer.SetValueForKeyPath (_toValue, _key);
        }
    }
}
