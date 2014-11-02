using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using MonoTouch.CoreAnimation;
using MonoTouch.CoreGraphics;
using MonoTouch.Foundation;
using MonoTouch.ObjCRuntime;
using MonoTouch.UIKit;

namespace Toggl.Ross.Views.Charting
{
    public interface IAnimationDelegate
    {
        void AnimationDidStart (CABasicAnimation anim);

        void AnimationDidStop (CABasicAnimation anim, bool finished);
    }

    public interface IXYDonutChartDataSource
    {
        int NumberOfSlicesInPieChart (XYDonutChart pieChart);

        float ValueForSliceAtIndex (XYDonutChart pieChart, int index);

        UIColor ColorForSliceAtIndex (XYDonutChart pieChart, int index);

        string TextForSliceAtIndex (XYDonutChart pieChart, int index);
    }

    public class XYDonutChart : UIView, IAnimationDelegate
    {
        #region Event handlers

        public event EventHandler<SelectedSliceEventArgs> WillSelectSliceAtIndex;

        public event EventHandler<SelectedSliceEventArgs> DidSelectSliceAtIndex;

        public event EventHandler<SelectedSliceEventArgs> WillDeselectSliceAtIndex;

        public event EventHandler<SelectedSliceEventArgs> DidDeselectSliceAtIndex;

        public event EventHandler<SelectedSliceEventArgs> DidDeselectAllSlices;

        #endregion

        #region Properties

        public IXYDonutChartDataSource DataSource { get; set; }

        public double StartPieAngle { get; set; }

        public float AnimationSpeed { get; set; }

        public UIFont LabelFont { get; set; }

        public UIColor LabelColor { get; set; }

        public UIColor LabelShadowColor { get; set; }

        public float LabelRadius { get; set; }

        public float SelectedSliceStroke { get; set; }

        public float SelectedSliceOffsetRadius { get; set; }

        public bool IsDonut { get; set; }

        public float DonutLineStroke { get; set; }


        private bool _showPercentage;

        public bool ShowPercentage
        {
            get {
                return _showPercentage;
            } set {
                _showPercentage = value;

                var sliceLayers = _pieView.Layer.Sublayers ?? new CALayer[0];
                foreach (SliceLayer layer in sliceLayers) {
                    var textLayer = layer.Sublayers [0];
                    textLayer.Hidden = !_showPercentage;
                    updateLabelForLayer (layer, layer.Value);
                }
            }
        }

        public bool ShowLabel { get; set; }

        private PointF _pieCenter;

        public PointF PieCenter
        {
            get {
                return _pieCenter;
            } set {
                _pieView.Center = value;
                _pieCenter = new PointF (_pieView.Frame.Width / 2, _pieView.Frame.Height / 2);
            }
        }

        public float PieRadius
        {
            get;
            set;
        }

        public UIColor PieBackgroundColor
        {
            get {
                return _pieView.BackgroundColor;
            } set {
                _pieView.BackgroundColor = value;
            }
        }

        #endregion

        int _selectedSliceIndex;
        UIView _pieView;
        NSTimer _animationTimer;
        const int defaultSliceZOrder = 100;
        List<CABasicAnimation> _animations;
        AnimationDelegate _animationDelegate;


        public XYDonutChart (RectangleF frame) : base (frame)
        {
            _pieView = new UIView (frame);
            PieBackgroundColor = UIColor.Clear;
            AddSubview (_pieView);

            _selectedSliceIndex = -1;
            _animations = new List<CABasicAnimation> ();
            _animationDelegate = new AnimationDelegate (this);

            AnimationSpeed = 0.5f;
            StartPieAngle = Math.PI * 3;
            SelectedSliceStroke = 2.0f;

            PieRadius = Math.Min (frame.Width / 2, frame.Height / 2) - 10;
            PieCenter = new PointF (frame.Width / 2, frame.Height / 2);
            LabelFont = UIFont.BoldSystemFontOfSize (Math.Max (PieRadius / 10, 5));
            LabelColor = UIColor.White;
            LabelRadius = PieRadius / 2;
            SelectedSliceOffsetRadius = Math.Max (10, PieRadius / 10);
            DonutLineStroke = PieRadius / 4;

            IsDonut = true;
            ShowLabel = true;
            ShowPercentage = true;
        }

        protected override void Dispose (bool disposing)
        {
            layerPool.Clear ();
            base.Dispose (disposing);
            CALayer parentLayer = _pieView.Layer;
            var pieLayers = parentLayer.Sublayers ?? new CALayer[0];
            foreach (SliceLayer layer in pieLayers) {
                var shapeLayer = (CAShapeLayer)layer.Sublayers [0];
                layerPool.Remove (layer);
                shapeLayer.FillColor = PieBackgroundColor.CGColor;
                shapeLayer.Path.Dispose ();
                layer.Delegate = null;
                layer.ZPosition = 0;
                layer.Sublayers [1].Hidden = true;
                layer.RemoveFromSuperLayer ();
            }
        }

        #region Touch Handing (Selection Notification)

        public override void TouchesBegan (NSSet touches, UIEvent evt)
        {
            TouchesMoved (touches, evt);
        }

        public override void TouchesMoved (NSSet touches, UIEvent evt)
        {
            var touch = (UITouch)touches.AnyObject;
            PointF point = touch.LocationInView (_pieView);
            getCurrentSelectedOnTouch (point);
        }

        public override void TouchesEnded (NSSet touches, UIEvent evt)
        {
            var touch = (UITouch)touches.AnyObject;
            PointF point = touch.LocationInView (_pieView);
            var selectedIndex = getCurrentSelectedOnTouch (point);
            notifyDelegateOfSelectionChangeFrom (_selectedSliceIndex, selectedIndex);
            TouchesCancelled (touches, evt);
        }

        public override void TouchesCancelled (NSSet touches, UIEvent evt)
        {
            CALayer parentLayer = _pieView.Layer;
            var pieLayers = parentLayer.Sublayers;
            if (pieLayers == null) {
                return;
            }
        }

        private int getCurrentSelectedOnTouch (PointF point)
        {
            int selectedIndex = -1;
            CGAffineTransform transform = CGAffineTransform.MakeIdentity ();

            CALayer parentLayer = _pieView.Layer;
            var pieLayers = parentLayer.Sublayers ?? new CALayer[0];
            int idx = 0;

            foreach (SliceLayer item in pieLayers) {
                var shapeLayer = (CAShapeLayer)item.Sublayers [0];
                CGPath path = shapeLayer.Path;
                if (path.ContainsPoint (transform, point, false)) {
                    shapeLayer.LineWidth = SelectedSliceStroke;
                    shapeLayer.StrokeColor = UIColor.White.CGColor;
                    shapeLayer.LineJoin = CAShapeLayer.JoinBevel;
                    item.ZPosition = float.MaxValue;
                    selectedIndex = idx;
                } else {
                    item.ZPosition = defaultSliceZOrder;
                    shapeLayer.LineWidth = 0.0f;
                }
                idx++;
            }
            return selectedIndex;
        }

        #endregion

        private List<SliceLayer> layerPool = new List<SliceLayer>();

        public void ReloadData ()
        {
            if (DataSource == null) {
                return;
            }

            CALayer parentLayer = _pieView.Layer;
            var sliceLayers = parentLayer.Sublayers ?? new CALayer[0];

            _selectedSliceIndex = -1;
            for (int i = 0; i < sliceLayers.Length; i++) {
                var layer = (SliceLayer)sliceLayers [i];
                if (layer.IsSelected) {
                    SetSliceDeselectedAtIndex (i);
                    layer.Opacity = 1;
                }
            }

            double startToAngle = 0.0f;
            double endToAngle = startToAngle;

            int sliceCount = DataSource.NumberOfSlicesInPieChart (this);
            float sum = 0.0f;
            var values = new float[sliceCount];
            for (int index = 0; index < sliceCount; index++) {
                values [index] = DataSource.ValueForSliceAtIndex (this, index);
                sum += values [index];
            }

            var angles = new double[sliceCount];
            for (int index = 0; index < sliceCount; index++) {
                double div;
                if ( equals (sum,0.0f)) {
                    div = 0;
                } else {
                    div = values [index] / sum;
                }
                angles [index] = Math.PI * 2 * div;
            }

            //CATransaction.Begin ();
            //CATransaction.AnimationDuration = Convert.ToDouble (AnimationSpeed);

            _pieView.UserInteractionEnabled = false;

            var layersToRemove = new List<SliceLayer> ();
            bool isOnStart = (sliceLayers.Length == 0 && sliceCount > 0);
            int diff = sliceCount - sliceLayers.Length;

            for (int i = 0; i < sliceLayers.Length; i++) {
                layersToRemove.Add ((SliceLayer)sliceLayers [i]);
            }

            bool isOnEnd = (sliceLayers.Length > 0) && (sliceCount == 0 || sum <= 0);
            if (isOnEnd) {
                foreach (var item in sliceLayers) {
                    var layer = (SliceLayer)item;
                    updateLabelForLayer (layer, 0.0f);
                    layer.CreateArcAnimationForKey ("startAngle", StartPieAngle, StartPieAngle, _animationDelegate);
                    layer.CreateArcAnimationForKey ("endAngle", StartPieAngle, StartPieAngle, _animationDelegate);
                }
                //CATransaction.Commit ();
                return;
            }

            for (int index = 0; index < sliceCount; index++) {
                SliceLayer layer = null;
                double angle = angles [index];
                endToAngle += angle;
                double startFromAngle = StartPieAngle + startToAngle;
                double endFromAngle = StartPieAngle + endToAngle;

                if (index >= sliceLayers.Length) {
                    layer = createSliceLayer ();
                    if (isOnStart) {
                        startFromAngle = endFromAngle = StartPieAngle;
                    }
                    parentLayer.AddSublayer (layer);
                    diff--;
                } else {
                    var onelayer = (SliceLayer)sliceLayers [index];
                    if (diff == 0 || equals ( onelayer.Value, values [index])) {
                        layer = onelayer;
                        layersToRemove.Remove (layer);
                    } else if (diff > 0) {
                        layer = createSliceLayer ();
                        parentLayer.InsertSublayer (layer, index);
                        diff--;
                    } else if (diff < 0) {
                        while (diff < 0) {
                            onelayer.RemoveFromSuperLayer ();
                            parentLayer.AddSublayer (onelayer); // TODO: check removing this code with the new Xamarin compiler
                            diff++;
                            onelayer = (SliceLayer)sliceLayers [index];
                            if ( equals ( onelayer.Value,values [index]) || diff == 0) {
                                layer = onelayer;
                                layersToRemove.Remove (layer);
                                break;
                            }
                        }
                    }
                }

                layer.Value = values [index];
                layer.Percentage = (sum > 0) ? layer.Value / sum : 0;
                UIColor color;

                if (DataSource.ColorForSliceAtIndex (this, index) != null) {
                    color = DataSource.ColorForSliceAtIndex (this, index);
                } else {
                    color = UIColor.FromHSBA ((float) (index / 8f % 20.0f / 20.0 + 0.02f), (float) ((index % 8 + 3) / 10.0), (float) (91 / 100.0), 1);
                }

                layer.ChangeToColor (color);

                if (!String.IsNullOrEmpty (DataSource.TextForSliceAtIndex (this, index))) {
                    layer.Text = DataSource.TextForSliceAtIndex (this, index);
                }

                updateLabelForLayer (layer, values [index]);
                layer.CreateArcAnimationForKey ("startAngle", startFromAngle, startToAngle + StartPieAngle, _animationDelegate);
                layer.CreateArcAnimationForKey ("endAngle", endFromAngle, endToAngle + StartPieAngle, _animationDelegate);
                startToAngle = endToAngle;
            }

            CATransaction.DisableActions = true;
            foreach (var layer in layersToRemove) {
                var shapeLayer = (CAShapeLayer)layer.Sublayers [0];
                layerPool.Remove (layer);
                shapeLayer.FillColor = PieBackgroundColor.CGColor;
                shapeLayer.Path.Dispose ();
                layer.Delegate = null;
                layer.ZPosition = 0;
                layer.Sublayers [1].Hidden = true;
                layer.RemoveFromSuperLayer ();
            }
            layersToRemove.Clear ();

            foreach (var layer in sliceLayers) {
                layer.ZPosition = defaultSliceZOrder;
            }

            _pieView.UserInteractionEnabled = true;
            CATransaction.DisableActions = false;
            //CATransaction.Commit ();
        }

        public void SetSliceSelectedAtIndex (int index)
        {
            if (SelectedSliceOffsetRadius <= 0) {
                return;
            }

            var layer = (SliceLayer)_pieView.Layer.Sublayers [index];
            if (layer != null && !layer.IsSelected) {
                PointF currPos = layer.Position;
                double middleAngle = (layer.StartAngle + layer.EndAngle) / 2.0;
                var newPos = new PointF (currPos.X + SelectedSliceOffsetRadius * Convert.ToSingle (Math.Cos (middleAngle)), currPos.Y + SelectedSliceOffsetRadius * Convert.ToSingle (Math.Sin (middleAngle)));
                layer.MoveToPosition (newPos);
                layer.IsSelected = true;
                _selectedSliceIndex = index;
            }

            foreach (var item in layerPool) {
                item.Opacity = (item.IsSelected) ? 1.0f : 0.5f;
            }
        }

        public void SetSliceDeselectedAtIndex (int index)
        {
            if (SelectedSliceOffsetRadius <= 0) {
                return;
            }

            var layer = (SliceLayer)_pieView.Layer.Sublayers [index];
            if (layer != null && layer.IsSelected) {
                layer.MoveToPosition (new PointF (0, 0));
                layer.IsSelected = false;
                _selectedSliceIndex = -1;
            }
        }

        public void DeselectAllSlices()
        {
            for (int i = 0; i < layerPool.Count; i++) {
                SetSliceDeselectedAtIndex (i);
                layerPool[i].Opacity = 1;
            }
        }

        private void notifyDelegateOfSelectionChangeFrom (int previousSelection, int newSelection)
        {
            if (previousSelection != newSelection) {
                if (previousSelection != -1) {
                    int tempPre = previousSelection;
                    if (WillDeselectSliceAtIndex != null)
                        WillDeselectSliceAtIndex.Invoke (this, new SelectedSliceEventArgs () { Index = tempPre });
                    SetSliceDeselectedAtIndex (tempPre);
                    if (DidDeselectSliceAtIndex != null)
                        DidDeselectSliceAtIndex.Invoke (this, new SelectedSliceEventArgs () { Index = tempPre });
                }
                if (newSelection != -1) {
                    if (WillSelectSliceAtIndex != null)
                        WillSelectSliceAtIndex.Invoke (this, new SelectedSliceEventArgs () { Index = newSelection });
                    SetSliceSelectedAtIndex (newSelection);
                    _selectedSliceIndex = newSelection;
                    if (DidSelectSliceAtIndex != null)
                        DidSelectSliceAtIndex.Invoke (this, new SelectedSliceEventArgs () { Index = newSelection });
                }
            } else if (newSelection != -1) {
                var layer = (SliceLayer)_pieView.Layer.Sublayers [newSelection];
                if (SelectedSliceOffsetRadius > 0 && layer != null) {
                    if (layer.IsSelected) {
                        if (WillDeselectSliceAtIndex != null)
                            WillDeselectSliceAtIndex.Invoke (this, new SelectedSliceEventArgs () { Index = newSelection });
                        SetSliceDeselectedAtIndex (newSelection);
                        if (newSelection != -1 && DidDeselectSliceAtIndex != null)
                            DidDeselectSliceAtIndex.Invoke (this, new SelectedSliceEventArgs () { Index = newSelection });
                    } else {
                        if (WillSelectSliceAtIndex != null)
                            WillSelectSliceAtIndex.Invoke (this, new SelectedSliceEventArgs () { Index = newSelection });
                        SetSliceSelectedAtIndex (newSelection);
                        if (newSelection != -1 && DidSelectSliceAtIndex != null)
                            DidSelectSliceAtIndex.Invoke (this, new SelectedSliceEventArgs () { Index = newSelection });
                    }
                }
            }

            if (newSelection == -1 || newSelection == previousSelection) {
                if ( DidDeselectAllSlices != null) {
                    DidDeselectAllSlices.Invoke (this, new SelectedSliceEventArgs ());
                }
                foreach (var item in layerPool) {
                    item.Opacity = 1.0f;
                }
            }
        }

        private int containsLayer ( List<SliceLayer> list, SliceLayer layer)
        {
            int result = -1;
            for (int i = 0; i < list.Count; i++) {
                var item = list [i];
                if (layer.StartAngle.CompareTo ( item.StartAngle) == 0 &&
                        layer.EndAngle.CompareTo ( item.EndAngle) == 0) {
                    result = i;
                }
            }
            return result;
        }

        private bool equals ( float a, float b)
        {
            return (Math.Abs (a - b) < float.Epsilon);
        }

        #region Animation Delegate + Run Loop Timer

        [Export ("updateTimerFired:")]
        private void UpdateTimerFired (NSTimer timer)
        {
            if (_pieView.Layer.Sublayers == null) {
                return;
            }
            foreach (var layer in _pieView.Layer.Sublayers) {
                if (layer.PresentationLayer != null) {
                    var shapeLayer = (CAShapeLayer)layer.Sublayers [0];
                    var currentStartAngle = (NSNumber)layer.PresentationLayer.ValueForKey (new NSString ("startAngle"));
                    var interpolatedStartAngle = currentStartAngle.DoubleValue;
                    var currentEndAngle = (NSNumber)layer.PresentationLayer.ValueForKey (new NSString ("endAngle"));
                    double interpolatedEndAngle = currentEndAngle.DoubleValue;
                    var path = CGPathCreateArc (_pieCenter, PieRadius, interpolatedStartAngle, interpolatedEndAngle);
                    shapeLayer.Path = path;
                    path.Dispose ();
                    CALayer labelLayer = layer.Sublayers [1];
                    double interpolatedMidAngle = (interpolatedEndAngle + interpolatedStartAngle) / 2;
                    CATransaction.DisableActions = true;
                    labelLayer.Position = new PointF (_pieCenter.X + (LabelRadius * Convert.ToSingle (Math.Cos (interpolatedMidAngle))), _pieCenter.Y + (LabelRadius * Convert.ToSingle (Math.Sin (interpolatedMidAngle))));
                    CATransaction.DisableActions = false;
                }
            }
        }

        public void AnimationDidStart (CABasicAnimation anim)
        {
            if (_animationTimer == null) {
                const double timeInterval = 1.0f / 60.0f;
                _animationTimer = NSTimer.CreateTimer (timeInterval, this, new Selector ("updateTimerFired:"),null, true);
                NSRunLoop.Main.AddTimer (_animationTimer, NSRunLoopMode.Common);
            }
            _animations.Add (anim);
        }

        public void AnimationDidStop (CABasicAnimation anim, bool isFinished)
        {
            _animations.Remove (anim);
            if (_animations.Count == 0) {
                _animationTimer.Invalidate ();
                _animationTimer = null;
            }
        }

        #endregion

        #region Pie Layer Creation Method

        private CGPath CGPathCreateArc (PointF center, float radius, double startAngle, double endAngle)
        {
            var path = new CGPath ();
            CGPath resultPath;
            if (IsDonut) {
                path.AddArc (center.X, center.Y, radius, Convert.ToSingle (startAngle), Convert.ToSingle (endAngle), false);
                resultPath = path.CopyByStrokingPath (DonutLineStroke, CGLineCap.Butt, CGLineJoin.Miter, 10);
                path.Dispose ();
            } else {
                path.MoveToPoint (center.X, center.Y);
                path.AddArc (center.X, center.Y, radius, Convert.ToSingle (startAngle), Convert.ToSingle (endAngle), false);
                path.CloseSubpath ();
                resultPath = path;
            }
            return resultPath;
        }

        private SliceLayer createSliceLayer ()
        {
            var pieLayer = new SliceLayer ();
            var arcLayer = new CAShapeLayer ();
            arcLayer.ZPosition = 0;
            arcLayer.StrokeColor = null;
            pieLayer.AddSublayer (arcLayer);

            var textLayer = new CATextLayer ();
            textLayer.ContentsScale = UIScreen.MainScreen.Scale;
            CGFont font = CGFont.CreateWithFontName (LabelFont.Name);

            if (font != null) {
                textLayer.SetFont (font);
                font.Dispose ();
            }

            textLayer.FontSize = LabelFont.PointSize;
            textLayer.AnchorPoint = new PointF (0.5f, 0.5f);
            textLayer.AlignmentMode = CATextLayer.AlignmentCenter;
            textLayer.BackgroundColor = UIColor.Clear.CGColor;
            textLayer.ForegroundColor = LabelColor.CGColor;

            if (LabelShadowColor != null) {
                textLayer.ShadowColor = LabelShadowColor.CGColor;
                textLayer.ShadowOffset = SizeF.Empty;
                textLayer.ShadowOpacity = 1.0f;
                textLayer.ShadowRadius = 2.0f;
            }

            SizeF size = ((NSString)"0").StringSize (LabelFont);

            CATransaction.DisableActions = true;
            textLayer.Frame = new RectangleF (new PointF (0, 0), size);
            textLayer.Position = new PointF (_pieCenter.X + (LabelRadius * Convert.ToSingle (Math.Cos (0))), _pieCenter.Y + (LabelRadius * Convert.ToSingle (Math.Sin (0))));
            CATransaction.DisableActions = false;
            pieLayer.AddSublayer (textLayer);
            layerPool.Add (pieLayer); // TODO: remove pool!
            return pieLayer;
        }

        #endregion

        private void updateLabelForLayer (SliceLayer sliceLayer, float value)
        {
            var textLayer = (CATextLayer)sliceLayer.Sublayers [1];
            textLayer.Hidden = !ShowLabel;
            if (!ShowLabel) {
                return;
            }

            String label = ShowPercentage ? sliceLayer.Percentage.ToString ("P1") : sliceLayer.Value.ToString ("0.00");
            var nsString = new NSString (label);
            SizeF size = nsString.GetSizeUsingAttributes (new UIStringAttributes () { Font = LabelFont });

            CATransaction.DisableActions = true;
            if (Math.PI * 2 * LabelRadius * sliceLayer.Percentage < Math.Max (size.Width, size.Height) || value <= 0) {
                textLayer.String = "";
            } else {
                textLayer.String = label;
                textLayer.Bounds = new RectangleF (0, 0, size.Width, size.Height);
            }
            CATransaction.DisableActions = false;
        }
    }

    sealed class AnimationDelegate : CAAnimationDelegate
    {
        private readonly IAnimationDelegate _owner;

        public AnimationDelegate (IAnimationDelegate owner)
        {
            _owner = owner;
        }

        public override void AnimationStarted (CAAnimation anim)
        {
            _owner.AnimationDidStart ((CABasicAnimation)anim);
        }

        public override void AnimationStopped (CAAnimation anim, bool finished)
        {
            _owner.AnimationDidStop ((CABasicAnimation)anim, finished);
        }
    }

    class SliceLayer : CALayer
    {
        [Export ("startAngle")]
        public double StartAngle { get; set; }

        [Export ("endAngle")]
        public double EndAngle { get; set; }

        public float Value { get; set; }

        public float Percentage { get; set; }

        public bool IsSelected { get; set; }

        public string Text { get; set; }

        public SliceLayer ()
        {
        }

        public SliceLayer (IntPtr ptr) : base (ptr)
        {
        }

        [Export ("initWithLayer:")]
        public SliceLayer (CALayer other) : base (other)
        {
        }

        public override void Clone (CALayer other)
        {
            base.Clone ( other);
            var sliceLayer = other as SliceLayer;
            if (sliceLayer != null) {
                StartAngle = sliceLayer.StartAngle;
                EndAngle = sliceLayer.EndAngle;
                Value = sliceLayer.Value;
                Percentage = sliceLayer.Percentage;
                IsSelected = sliceLayer.IsSelected;
                Text = sliceLayer.Text;
            }
        }

        [Export ("needsDisplayForKey:")]
        static bool NeedsDisplayForKey (NSString key)
        {
            switch (key.ToString ()) {
            case "startAngle":
                return true;
            case "endAngle":
                return true;
            default:
                return CALayer.NeedsDisplayForKey (key);
            }
        }

        public override NSObject ActionForKey (string eventKey)
        {
            // disable implicit animations and use explicit animations with MoveToPosition function
            // More control and implicit animations don't works good
            // need more tests!
            if (eventKey == "position" || eventKey == "fillColor") {
                return null;
            }
            return base.ActionForKey (eventKey);
        }

        public void CreateArcAnimationForKey (string key, double fromValue, double toValue, CAAnimationDelegate @delegate)
        {
            var _fromValue = new NSNumber (fromValue);
            var _toValue = new NSNumber (toValue);
            var _key = new NSString (key);

            var arcAnimation = CABasicAnimation.FromKeyPath (key);
            var currentAngle = _fromValue;
            if (PresentationLayer != null) {
                currentAngle = (NSNumber)PresentationLayer.ValueForKey (_key);
            }
            arcAnimation.Duration = 1.0f;
            arcAnimation.From = currentAngle;
            arcAnimation.To = _toValue;
            arcAnimation.Delegate = @delegate;
            arcAnimation.TimingFunction = CAMediaTimingFunction.FromName (CAMediaTimingFunction.Default);
            AddAnimation (arcAnimation, key);
            SetValueForKey (_toValue, _key);
        }

        public void MoveToPosition (PointF newPos)
        {
            var posAnim = CABasicAnimation.FromKeyPath ("position");
            posAnim.From = NSValue.FromPointF (Position);
            posAnim.To = NSValue.FromPointF (newPos);
            posAnim.Duration = 0.4f;
            posAnim.TimingFunction = CAMediaTimingFunction.FromName (CAMediaTimingFunction.Default);
            AddAnimation (posAnim, "position");
            Position = newPos;
        }

        public void ChangeToColor (UIColor color)
        {
            var shapeLayer = (CAShapeLayer)Sublayers [0];
            var colorAnim = CABasicAnimation.FromKeyPath ("fillColor");
            colorAnim.From = NSObject.FromObject (shapeLayer.FillColor);
            colorAnim.To = NSObject.FromObject (color.CGColor);
            colorAnim.Duration = 1.0f;
            colorAnim.TimingFunction = CAMediaTimingFunction.FromName (CAMediaTimingFunction.Default);
            shapeLayer.AddAnimation (colorAnim, "fillColor");
            shapeLayer.FillColor = color.CGColor;
        }
    }

    public sealed class SelectedSliceEventArgs : EventArgs
    {
        public int Index { get; set; }
    }
}