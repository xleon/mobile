using System;
using System.Drawing;
using MonoTouch.CoreGraphics;
using MonoTouch.Foundation;
using MonoTouch.UIKit;
using MonoTouch.CoreAnimation;

namespace Toggl.Ross.Views.Charting
{

    public class ChartSurface : UIView
    {
        public UIColor[] Colors;

        public EventHandler OnReadyToDraw;

        public ChartSurface ( UIColor backgroundColor, UIColor[] colors)
        {
            Colors = colors;
            BackgroundColor = backgroundColor;
        }

        public void DrawBar (DoubleDrawingData data)
        {
            using (CGContext g = UIGraphics.GetCurrentContext()) {
                g.SetLineWidth (1);
                Colors[data.ColorNo].SetFill();
                Colors[data.ColorNo].SetStroke();

                RectangleF rect = new RectangleF (data.XFrom, data.YFrom, (data.XTo - data.XFrom), (data.YTo - data.YFrom));
                g.AddRect (rect);

                g.DrawPath (CGPathDrawingMode.FillStroke);
            }
        }

        public void DrawCircle (SingleDrawingData data)
        {
            using (CGContext g = UIGraphics.GetCurrentContext()) {
                g.SetLineWidth (2);
                Colors[data.ColorNo].SetFill();
                Colors[data.ColorNo].SetStroke();

                float startAngle = (float)- (Math.PI / 2);
                float endAngle = (float) ((2 * Math.PI) + startAngle);
                g.AddArc (data.X, data.Y, data.Size, startAngle, endAngle, true);

                g.DrawPath (CGPathDrawingMode.FillStroke);
            }
        }

        public void DrawLine (DoubleDrawingData data, float lineHeight)
        {
            using (CGContext g = UIGraphics.GetCurrentContext()) {
                g.SetLineWidth (lineHeight);
                Colors[data.ColorNo].SetFill();
                Colors[data.ColorNo].SetStroke();

                g.MoveTo (data.XFrom, data.YFrom);
                g.AddLineToPoint (data.XTo, data.YTo);

                g.DrawPath (CGPathDrawingMode.FillStroke);
            }
        }

        public void DrawText (TextDrawingData data, UIStringAttributes attrs)
        {
            NSString str = new NSString (data.Text);
            str.DrawString (new PointF (data.X, data.Y), attrs);
        }

        public CATextLayer DrawTextOnLayer (TextDrawingData data, UIStringAttributes attrs)
        {
            var textLayer = new CATextLayer () {
                ContentsScale = UIScreen.MainScreen.Scale,
                //AnchorPoint = new PointF( 0.0f, 0.0f),
                ForegroundColor = attrs.ForegroundColor.CGColor,
                String = data.Text,
                FontSize = attrs.Font.PointSize,
                Frame = new RectangleF ( data.X, data.Y, 30, 15)
            };
            textLayer.SetFont ( attrs.Font.Name);
            Layer.AddSublayer (textLayer);
            return textLayer;
        }

        public CALayer DrawBarOnLayer (RectDrawingData data)
        {
            var layer = new CALayer () {
                ContentsScale = UIScreen.MainScreen.Scale,
                BackgroundColor = Colors[data.ColorNo].CGColor,
                AnchorPoint = new PointF ( 0.0f, 0.0f),
                Frame = new RectangleF ( data.X, data.Y, data.Width, data.Height)
            };
            Layer.AddSublayer (layer);
            return layer;
        }

        public override void Draw (RectangleF rect)
        {
            base.Draw (rect);
            if (OnReadyToDraw != null) {
                OnReadyToDraw.Invoke (this, null);
            }
        }
    }

    public sealed class DoubleDrawingData
    {
        public int ColorNo { get; set; }
        public float XFrom { get; set; }
        public float YFrom { get; set; }
        public float XTo { get; set; }
        public float YTo { get; set; }

        public DoubleDrawingData (float xFrom, float yFrom, float xTo, float yTo, int colorNo)
        {
            XFrom = xFrom;
            YFrom = yFrom;
            XTo = xTo;
            YTo = yTo;
            ColorNo = colorNo;
        }
    }

    public sealed class SingleDrawingData
    {
        public int ColorNo { get; set; }
        public float X { get; set; }
        public float Y { get; set; }
        public float Size { get; set; }

        public SingleDrawingData (float x, float y, int colorNo)
        {
            X = x;
            Y = y;
            ColorNo = colorNo;
            Size = 5;
        }
        public SingleDrawingData (float x, float y, int colorNo, float size)
        {
            X = x;
            Y = y;
            ColorNo = colorNo;
            Size = size;
        }
    }

    public sealed class RectDrawingData
    {
        public int ColorNo { get; set; }
        public float X { get; set; }
        public float Y { get; set; }
        public float Width { get; set; }
        public float Height { get; set; }

        public RectDrawingData (float x, float y, float width, float height, int colorNo)
        {
            X = x;
            Y = y;
            Width = width;
            Height = height;
            ColorNo = colorNo;
        }
    }

    public class TextDrawingData
    {
        public float X { get; set; }
        public float Y { get; set; }
        public string Text { get; set; }

        public TextDrawingData (string text, float x, float y)
        {
            Text = text;
            X = x;
            Y = y;
        }
    }
}