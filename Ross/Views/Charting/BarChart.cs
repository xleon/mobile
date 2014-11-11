using System;
using System.Drawing;
using MonoTouch.CoreAnimation;
using MonoTouch.CoreGraphics;
using MonoTouch.Foundation;
using MonoTouch.UIKit;
using Toggl.Ross.Theme;

namespace Toggl.Ross.Views.Charting
{

    public interface IBarChartDataSource
    {
        int NumberOfBarsOnChart (BarChart barChart);

        float ValueForBarAtIndex (BarChart barChart, int index);

        float ValueForSecondaryBarAtIndex ( BarChart barChart, int index);

        string TextForBarAtIndex (BarChart barChart, int index);

        string TimeIntervalAtIndex (int index);
    }

    public class BarChart : UIView, IAnimationDelegate
    {
        public IBarChartDataSource DataSource { get; set; }

        public float AnimationSpeed { get; set; }

        public UIFont LabelFont { get; set; }

        public UIColor LabelColor { get; set; }

        public UIColor MainBarColor { get; set; }

        public UIColor SecondaryBarColor { get; set; }

        public BarChart (RectangleF frame) : base (frame)
        {
            _barChartView = new UIView ( new RectangleF ( 0, 0, frame.Width, frame.Height));
            AddSubview (_barChartView);

            LabelFont = UIFont.SystemFontOfSize (10.0f);
            LabelColor = UIColor.LightGray;
            MainBarColor = Color.TimeBarColor;
            SecondaryBarColor = Color.MoneyBarColor;
            AnimationSpeed = 0.5f;
            BackgroundColor = UIColor.White;
        }

        UIView _barChartView;
        CATextLayer[] xAxisText = new CATextLayer[5];
        float maxBarSize = 400;

        const float minBarScale = 0.005f;
        const int defaultOrder = 100;
        const float xAxisMargin = 35;
        const float yAxisMargin = 20;
        const float topBarMargin = 10;
        const float xTextMargin = 35;


        public override void Draw (RectangleF rect)
        {
            base.Draw (rect);

            using (var context = UIGraphics.GetCurrentContext ()) {

                UIBezierPath mainAxis = UIBezierPath.FromRoundedRect ( new RectangleF (xAxisMargin, 0.0f, 2.0f, rect.Height), 2.0f);
                UIColor.FromRGB ( 203, 203, 203).SetFill();
                mainAxis.Fill();

                float sepInterval = Convert.ToSingle ( Math.Floor ((rect.Width - xAxisMargin - xTextMargin)/ 5));
                for (int i = 1; i < 6; i++) {
                    UIBezierPath separatorAxis = new UIBezierPath();
                    separatorAxis.MoveTo (new PointF (xAxisMargin + sepInterval * i, 0));
                    separatorAxis.AddLineTo (new PointF ( xAxisMargin + sepInterval * i, rect.Height - yAxisMargin));
                    UIColor.FromRGB ( 203, 203, 203).SetStroke();
                    separatorAxis.LineWidth = 1.0f;
                    separatorAxis.SetLineDash ( new [] {1.0f,1.0f}, 1);
                    separatorAxis.Stroke ();

                    var textLayer = new CATextLayer ();
                    textLayer.ContentsScale = UIScreen.MainScreen.Scale;
                    CGFont font = CGFont.CreateWithFontName (LabelFont.Name);

                    if (font != null) {
                        textLayer.SetFont (font);
                        font.Dispose ();
                    }

                    textLayer.FontSize = LabelFont.PointSize;
                    textLayer.AnchorPoint = new PointF ( 0.5f, 0.0f);
                    textLayer.AlignmentMode = CATextLayer.AlignmentCenter;
                    textLayer.BackgroundColor = UIColor.Clear.CGColor;
                    textLayer.ForegroundColor = LabelColor.CGColor;

                    SizeF size = ((NSString)"0000 h").StringSize (LabelFont);
                    textLayer.String = "00 h";
                    textLayer.Bounds = new RectangleF (0, 0, size.Width, size.Height);
                    textLayer.Position = new PointF ( xAxisMargin + sepInterval * i, rect.Height - yAxisMargin + 5.0f);
                    Layer.AddSublayer (textLayer);
                    xAxisText [i - 1] = textLayer;
                }
            };
        }

        public void ReloadData()
        {
            if (DataSource == null) {
                return;
            }

            CALayer parentLayer = _barChartView.Layer;
            var barLayers = parentLayer.Sublayers ?? new CALayer[0];

            int barsCount = DataSource.NumberOfBarsOnChart (this);

            for (int i = 0; i < xAxisText.Length; i++) {
                xAxisText [i].String = DataSource.TimeIntervalAtIndex (i);
            }

            CATransaction.Begin ();
            CATransaction.AnimationDuration = Convert.ToDouble (AnimationSpeed);
            _barChartView.UserInteractionEnabled = false;

            float barHeight = (Frame.Height - topBarMargin - yAxisMargin) / Convert.ToSingle (barsCount);
            const float padding = 1.0f;
            float initialY = barHeight / 2 + topBarMargin;

            for (int i = 0; i < barsCount; i++) {
                BarLayer oneLayer;
                if (i >= barLayers.Length) {
                    oneLayer = CreateBarLayer (barHeight - padding);
                    parentLayer.AddSublayer (oneLayer);
                } else {
                    oneLayer = (BarLayer)barLayers [i];
                    oneLayer.HeightValue = barHeight - padding;
                }
                oneLayer.TimeValue = DataSource.ValueForBarAtIndex (this, i);
                oneLayer.MoneyValue = DataSource.ValueForSecondaryBarAtIndex (this, i);
                oneLayer.Position = new PointF ( 0, initialY + i * barHeight);

                string labelText;
                if (barsCount > 12) {
                    labelText = (i % 3 == 0) ? DataSource.TextForBarAtIndex (this, i) : "";
                    LabelFont = LabelFont.WithSize (10.0f);
                } else {
                    labelText = DataSource.TextForBarAtIndex (this, i);
                    LabelFont = LabelFont.WithSize (12.0f);
                }

                CATransaction.DisableActions = true;
                var nsString = new NSString ( labelText);
                SizeF size = nsString.GetSizeUsingAttributes (new UIStringAttributes () { Font = LabelFont });
                var textLayer = (CATextLayer)oneLayer.Sublayers [3];
                textLayer.String = labelText;
                textLayer.FontSize = (barsCount > 12) ? 9.0f : 12.0f;
                textLayer.Bounds = new RectangleF (0, 0, size.Width, size.Height);
                textLayer.Position = new PointF ( 0.0f, oneLayer.Bounds.Height/2);
                CATransaction.DisableActions = false;
            }

            if ( (barsCount - barLayers.Length) < 0) {
                for (int i = barsCount; i < barLayers.Length; i++) {
                    var removeLayer = barLayers [i];
                    removeLayer.RemoveFromSuperLayer ();
                }
            }

            _barChartView.UserInteractionEnabled = true;
            CATransaction.Commit ();
        }

        private BarLayer CreateBarLayer ( float barHeight)
        {
            var barLayer = new BarLayer () {
                ZPosition = 0,
                BorderColor = UIColor.White.CGColor,
                AnchorPoint = new PointF ( 0.0f, 0.5f),
                Frame = new RectangleF ( 0, 0, Frame.Width, barHeight)
            };

            var mainBar = new CALayer ();
            mainBar.AnchorPoint = new PointF (0.0f, 0.0f);
            mainBar.Bounds = new RectangleF ( 0, 0, Frame.Width - xAxisMargin - xTextMargin, barHeight);
            mainBar.Position = new PointF ( xAxisMargin, 0);
            mainBar.BackgroundColor = MainBarColor.CGColor;
            mainBar.SetValueForKeyPath ( new NSNumber ( minBarScale), new NSString ( "transform.scale.x"));
            barLayer.AddSublayer (mainBar);

            var secondaryBar = new CALayer ();
            secondaryBar.AnchorPoint = new PointF (0.0f, 0.0f);
            secondaryBar.Bounds = new RectangleF ( 0, 0, Frame.Width - xAxisMargin - xTextMargin, barHeight);
            secondaryBar.Position = new PointF ( xAxisMargin, 0);
            secondaryBar.BackgroundColor = SecondaryBarColor.CGColor;
            secondaryBar.SetValueForKeyPath ( new NSNumber ( minBarScale), new NSString ( "transform.scale.x"));
            barLayer.AddSublayer (secondaryBar);

            var emptyBar = new CALayer ();
            emptyBar.AnchorPoint = new PointF (0.0f, 0.0f);
            emptyBar.Bounds = new RectangleF ( 0, 0, 2.0f, barHeight);
            emptyBar.Position = new PointF ( xAxisMargin, 0);
            emptyBar.BackgroundColor = UIColor.Gray.CGColor;
            barLayer.AddSublayer (emptyBar);

            var textLayer = new CATextLayer ();
            textLayer.ContentsScale = UIScreen.MainScreen.Scale;
            CGFont font = CGFont.CreateWithFontName (LabelFont.Name);

            if (font != null) {
                textLayer.SetFont (font);
                font.Dispose ();
            }

            textLayer.FontSize = LabelFont.PointSize;
            textLayer.AnchorPoint = new PointF ( 0.0f, 0.5f);
            textLayer.AlignmentMode = CATextLayer.AlignmentLeft;
            textLayer.BackgroundColor = UIColor.Clear.CGColor;
            textLayer.ForegroundColor = LabelColor.CGColor;

            SizeF size = ((NSString)"0").StringSize (LabelFont);

            CATransaction.DisableActions = true;
            textLayer.Bounds = new RectangleF (new PointF (0, 0), size);
            textLayer.Position = mainBar.Position;
            CATransaction.DisableActions = false;
            barLayer.AddSublayer (textLayer);

            return barLayer;
        }

        #region Touch Handing (Selection Notification)

        public override void TouchesBegan (NSSet touches, UIEvent evt)
        {
            TouchesMoved (touches, evt);
        }

        public override void TouchesMoved (NSSet touches, UIEvent evt)
        {
        }

        public override void TouchesEnded (NSSet touches, UIEvent evt)
        {
        }

        public override void TouchesCancelled (NSSet touches, UIEvent evt)
        {
        }

        #endregion

        #region IAnimationDelegate implementation

        public void AnimationDidStart (CABasicAnimation anim)
        {
            throw new NotImplementedException ();
        }

        public void AnimationDidStop (CABasicAnimation anim, bool finished)
        {
            throw new NotImplementedException ();
        }

        #endregion
    }

    class BarLayer : CALayer
    {
        public const int MainBarIndex = 0;
        public const int SecondaryBarIndex = 1;
        public const int EmptyBarIndex = 2;
        public const int DateTextIndex = 3;

        private float _timeValue;

        [Export ("timeValue")]
        public float TimeValue
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
                    CreateBarAnimationForKeyPath (mainBar, "transform.scale.x", xScale, null);
                }
                Sublayers [EmptyBarIndex].Opacity = (_timeValue > 0) ? 0.0f : 1.0f;
            }
        }

        private float _moneyValue;

        [Export ("moneyValue")]
        public float MoneyValue
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
                    CreateBarAnimationForKeyPath (secondaryBar, "transform.scale.x", xScale, null);
                }
                if ( _moneyValue > _timeValue && _moneyValue > 0) { // TODO: weird case where _moneyValue > _timeValue!
                    Sublayers [EmptyBarIndex].Opacity = (value > 0) ? 0.0f : 1.0f;
                }
            }
        }

        [Export ("heightValue")]
        public float HeightValue
        {
            get {
                return Sublayers [0] != null ? Sublayers [0].Bounds.Height : 0;
            } set {
                var mainBar = Sublayers [0];
                var secondaryBar = Sublayers [1];
                var emptyBar = Sublayers [EmptyBarIndex];
                if (value > 0 && mainBar != null) {
                    float barHeight = value;
                    Bounds = new RectangleF ( 0.0f, 0.0f, Bounds.Width, barHeight);
                    mainBar.Bounds = new RectangleF ( 0.0f, 0.0f, mainBar.Bounds.Width, barHeight);
                    secondaryBar.Bounds = new RectangleF (0.0f, 0.0f, secondaryBar.Bounds.Width, barHeight);
                    emptyBar.Bounds = new RectangleF ( 0.0f, 0.0f, emptyBar.Bounds.Width, barHeight);
                }
            }
        }

        public bool IsSelected { get; set; }

        public string Text { get; set; }

        const float minBarScale = 0.005f;

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
                HeightValue = _other.HeightValue;
                TimeValue = _other.TimeValue;
                var emptyBar = Sublayers [EmptyBarIndex];
                if (emptyBar != null) {
                    emptyBar.Opacity = 0.0f;
                }
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
            case "heightValue":
                return true;
            default:
                return CALayer.NeedsDisplayForKey (key);
            }
        }

        public void CreateBarAnimationForKeyPath ( CALayer layer, string key, float toValue, CAAnimationDelegate @delegate)
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
            barAnimation.Delegate = @delegate;
            barAnimation.TimingFunction = CAMediaTimingFunction.FromName (CAMediaTimingFunction.Default);
            layer.AddAnimation (barAnimation, key);
            layer.SetValueForKeyPath (_toValue, _key);
        }
    }
}

