using System;
using MonoTouch.UIKit;
using System.Drawing;
using MonoTouch.CoreAnimation;
using MonoTouch.Foundation;
using MonoTouch.CoreGraphics;
using System.Collections.Generic;
using System.Diagnostics;

namespace Toggl.Ross.Views.Charting
{

    public interface IBarChartDataSource
    {
        int NumberOfBarsOnChart (BarChart barChart);

        float ValueForBarAtIndex (BarChart barChart, int index);

        string TextForBarAtIndex (BarChart barChart, int index);

        string TimeIntervalAtIndex (int index);
    }

    public class BarChart : UIView, IAnimationDelegate
    {
        public event EventHandler<SelectedSliceEventArgs> WillSelectBarAtIndex;

        public event EventHandler<SelectedSliceEventArgs> DidSelectBarAtIndex;

        public event EventHandler<SelectedSliceEventArgs> WillDeselectBarAtIndex;

        public event EventHandler<SelectedSliceEventArgs> DidDeselectBarAtIndex;

        public IBarChartDataSource DataSource { get; set; }


        public float AnimationSpeed { get; set; }

        public UIFont LabelFont { get; set; }

        public UIColor LabelColor { get; set; }

        int _selectedSliceIndex;
        UIView _barChartView;
        NSTimer _animationTimer;
        const int defaultSliceZOrder = 100;
        const float xAxisMargin = 35;
        const float yAxisMargin = 20;
        CATextLayer[] xAxisText = new CATextLayer[5];
        const float topBarMargin = 10;
        List<CABasicAnimation> _animations;
        AnimationDelegate _animationDelegate;


        public BarChart (RectangleF frame) : base (frame)
        {
            _barChartView = new UIView ( new RectangleF ( 0, 0, frame.Width, frame.Height));
            AddSubview (_barChartView);

            _selectedSliceIndex = -1;
            _animations = new List<CABasicAnimation> ();
            _animationDelegate = new AnimationDelegate (this);
            LabelFont = UIFont.SystemFontOfSize (12.0f);
            LabelColor = UIColor.LightGray;

            AnimationSpeed = 0.5f;
            BackgroundColor = UIColor.White;
        }

        public override void Draw (RectangleF rect)
        {
            base.Draw (rect);

            using (var context = UIGraphics.GetCurrentContext ()) {

                UIBezierPath mainAxis = new UIBezierPath();
                mainAxis.MoveTo (new PointF (xAxisMargin + 1.0f, 0));
                mainAxis.AddLineTo (new PointF ( xAxisMargin + 1.0f, rect.Height));
                UIColor.FromRGB ( 203, 203, 203).SetStroke();
                mainAxis.LineWidth = 4.0f;
                mainAxis.Stroke ();

                float sepInterval = Convert.ToSingle ( Math.Floor ( (rect.Width - xAxisMargin)/ 5));
                for (int i = 1; i < 6; i++) {
                    UIBezierPath separatorAxis = new UIBezierPath();
                    separatorAxis.MoveTo (new PointF (xAxisMargin + sepInterval * i, 0));
                    separatorAxis.AddLineTo (new PointF ( xAxisMargin + sepInterval * i, rect.Height - yAxisMargin));
                    UIColor.FromRGB ( 203, 203, 203).SetStroke();
                    separatorAxis.LineWidth = 1.0f;
                    separatorAxis.SetLineDash ( new [] {2.0f,2.0f}, 1);
                    separatorAxis.Stroke ();

                    var textLayer = new CATextLayer ();
                    textLayer.ContentsScale = UIScreen.MainScreen.Scale;
                    CGFont font = CGFont.CreateWithFontName (LabelFont.Name);

                    if (font != null) {
                        textLayer.SetFont (font);
                        font.Dispose ();
                    }

                    textLayer.FontSize = LabelFont.PointSize;
                    textLayer.AnchorPoint = new PointF ( 1f, 0.0f);
                    textLayer.AlignmentMode = CATextLayer.AlignmentCenter;
                    textLayer.BackgroundColor = UIColor.Clear.CGColor;
                    textLayer.ForegroundColor = LabelColor.CGColor;

                    SizeF size = ((NSString)"00 h").StringSize (LabelFont);
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

            /*
            _selectedSliceIndex = -1;
            for (int i = 0; i < sliceLayers.Length; i++) {
                var layer = (SliceLayer)sliceLayers [i];
                if (layer.IsSelected) {
                    SetSliceDeselectedAtIndex (i);
                }
            }
            */

            int barsCount = DataSource.NumberOfBarsOnChart (this);

            for (int i = 0; i < xAxisText.Length; i++) {
                xAxisText [i].String = DataSource.TimeIntervalAtIndex (i);
            }

            var values = new float[barsCount];
            for (int index = 0; index < barsCount; index++) {
                values [index] = DataSource.ValueForBarAtIndex (this, index);
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
                    oneLayer = createBarLayer (barHeight - padding);
                    parentLayer.AddSublayer (oneLayer);
                } else {
                    oneLayer = (BarLayer)barLayers [i];
                    oneLayer.HeightValue = barHeight - padding;
                }
                oneLayer.TimeValue = values [i];
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
                var textLayer = oneLayer.DateTextLayer;
                textLayer.String = labelText;
                textLayer.FontSize = (barsCount > 12) ? 9.0f : 12.0f;
                textLayer.Bounds = new RectangleF (0, 0, size.Width, size.Height);
                textLayer.Position = new PointF ( (xAxisMargin - size.Width)/2, oneLayer.Bounds.Height/2);
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

        private void updateLabelForLayer (BarLayer barLayer, string text)
        {

        }

        private BarLayer createBarLayer ( float barHeight)
        {
            var barLayer = new BarLayer () {
                ZPosition = 0,
                BorderColor = UIColor.White.CGColor,
                AnchorPoint = new PointF ( 0.0f, 0.5f),
                Frame = new RectangleF ( 0, 0, Frame.Width, barHeight)
            };

            var mainBar = new CALayer ();
            mainBar.AnchorPoint = new PointF (0.0f, 0.0f);
            mainBar.Bounds = new RectangleF ( 0, 0, Frame.Width - xAxisMargin, barHeight);
            mainBar.Position = new PointF ( xAxisMargin, 0);
            mainBar.BackgroundColor = UIColor.Red.CGColor;

            barLayer.AddSublayer (mainBar);
            barLayer.MainBar = mainBar;

            var secondaryBar = new CALayer ();
            secondaryBar.Frame = new RectangleF ( xAxisMargin, 0, Frame.Width - xAxisMargin, barHeight);
            secondaryBar.AnchorPoint = new PointF (0.0f, 0.5f);
            secondaryBar.BackgroundColor = UIColor.Yellow.CGColor;
            //barLayer.AddSublayer (secondaryBar);
            //barLayer.SecondaryBar = secondaryBar;

            var textLayer = new CATextLayer ();
            textLayer.ContentsScale = UIScreen.MainScreen.Scale;
            CGFont font = CGFont.CreateWithFontName (LabelFont.Name);

            if (font != null) {
                textLayer.SetFont (font);
                font.Dispose ();
            }

            textLayer.FontSize = LabelFont.PointSize;
            textLayer.AnchorPoint = new PointF ( 0.0f, 0.5f);
            textLayer.Position = new PointF ( 100, textLayer.Position.Y);
            textLayer.AlignmentMode = CATextLayer.AlignmentLeft;
            textLayer.BackgroundColor = UIColor.Clear.CGColor;
            textLayer.ForegroundColor = LabelColor.CGColor;

            SizeF size = ((NSString)"0").StringSize (LabelFont);

            CATransaction.DisableActions = true;
            textLayer.Frame = new RectangleF (new PointF (0, 0), size);
            textLayer.Position = mainBar.Position;
            CATransaction.DisableActions = false;
            barLayer.AddSublayer (textLayer);
            barLayer.DateTextLayer = textLayer;

            return barLayer;
        }

        #region Touch Handing (Selection Notification)

        public override void TouchesBegan (NSSet touches, UIEvent evt)
        {
            TouchesMoved (touches, evt);
        }

        public override void TouchesMoved (NSSet touches, UIEvent evt)
        {
            /*
            var touch = (UITouch)touches.AnyObject;
            PointF point = touch.LocationInView (_pieView);
            getCurrentSelectedOnTouch (point);
            */
        }

        public override void TouchesEnded (NSSet touches, UIEvent evt)
        {
            /*
            var touch = (UITouch)touches.AnyObject;
            PointF point = touch.LocationInView (_pieView);
            var selectedIndex = getCurrentSelectedOnTouch (point);
            notifyDelegateOfSelectionChangeFrom (_selectedSliceIndex, selectedIndex);
            TouchesCancelled (touches, evt);
            */
        }

        public override void TouchesCancelled (NSSet touches, UIEvent evt)
        {
            /*
            CALayer parentLayer = _pieView.Layer;
            var pieLayers = parentLayer.Sublayers;
            foreach (SliceLayer item in pieLayers) {
                item.ZPosition = 0;
                item.LineWidth = 0.0f;
            }
            */
        }

        private int getCurrentSelectedOnTouch (PointF point)
        {
            int selectedIndex = -1;
            /*
            CGAffineTransform transform = CGAffineTransform.MakeIdentity ();

            CALayer parentLayer = _pieView.Layer;
            var pieLayers = parentLayer.Sublayers ?? new CALayer[0];
            int idx = 0;

            foreach (SliceLayer item in pieLayers) {
                CGPath path = item.Path;
                if (path.ContainsPoint (transform, point, false)) {
                    item.LineWidth = SelectedSliceStroke;
                    item.StrokeColor = UIColor.White.CGColor;
                    item.LineJoin = CAShapeLayer.JoinBevel;
                    item.ZPosition = float.MaxValue;
                    selectedIndex = idx;
                } else {
                    item.ZPosition = defaultSliceZOrder;
                    item.LineWidth = 0.0f;
                }
                idx++;
            }
            */
            return selectedIndex;
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

    public class BarLayer : CALayer
    {
        [Export ("timeValue")]
        public float TimeValue
        {
            get {
                return 0.0f;
            }

            set {
                if (MainBar != null ) {
                    var xScale = (value > 0) ? value : 0.005f;
                    MainBar.AffineTransform = CGAffineTransform.MakeScale ( xScale, 1.0f);
                    //MainBar.Opacity = (value > 0) ? 1 : 0;
                }
            }
        }

        [Export ("moneyValue")]
        public float MoneyValue { get; set; }

        [Export ("heightValue")]
        public float HeightValue
        {
            get {
                return MainBar != null ? MainBar.Bounds.Height : 0;
            } set {
                if (value > 0 && MainBar != null) {
                    float barHeight = value;
                    Frame = new RectangleF ( Frame.X, Frame.Y, Frame.Width, barHeight);
                    MainBar.Frame = new RectangleF (MainBar.Frame.X, MainBar.Frame.Y, MainBar.Frame.Width, barHeight);
                }
            }
        }

        public bool IsSelected { get; set; }

        public string Text { get; set; }

        public CALayer MainBar;

        public CALayer SecondaryBar;

        public CATextLayer TextLayer;

        public CATextLayer DateTextLayer;

        public CALayer EmptyBar;

        public BarLayer ()
        {
        }

        public BarLayer (IntPtr ptr) : base (ptr)
        {
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

        public void CreateBarAnimationForKey (string key, double fromValue, double toValue, CAAnimationDelegate @delegate)
        {
            var _fromValue = new NSNumber (fromValue);
            var _toValue = new NSNumber (toValue);
            var _key = new NSString (key);

            var barAnimation = CABasicAnimation.FromKeyPath (key);
            var currentValue = _fromValue;
            if (PresentationLayer != null) {
                currentValue = (NSNumber)PresentationLayer.ValueForKey (_key);
            }
            barAnimation.From = currentValue;
            barAnimation.To = _toValue;
            barAnimation.Delegate = @delegate;
            barAnimation.TimingFunction = CAMediaTimingFunction.FromName (CAMediaTimingFunction.Default);
            AddAnimation (barAnimation, key);
            SetValueForKey (_toValue, _key);
        }
    }
}

