using System.Drawing;
using MonoTouch.Foundation;
using MonoTouch.UIKit;
using MonoTouch.CoreAnimation;

namespace Toggl.Ross.Views.Charting
{
    public class SingleBar : CALayer
    {
        float Xpos = 20;
        float YPos = 20;
        const float topY = 30;
        const float graphHeight = 180;
        const float barHeight = 30;
        const float dateSpace = 30;

        readonly ChartSurface surface;

        readonly UIStringAttributes dateAttrs = new UIStringAttributes {
            ForegroundColor = UIColor.FromRGB (0x87, 0x87, 0x87),
            BackgroundColor = UIColor.Clear,
            Font = UIFont.FromName ("HelveticaNeue", 12f)
        };

        readonly UIStringAttributes symbolAttrs = new UIStringAttributes {
            ForegroundColor = UIColor.FromRGB (0x81, 0xD3, 0xF9),
            BackgroundColor = UIColor.Clear,
            Font = UIFont.FromName ("HelveticaNeue", 10f)
        };

        readonly UIStringAttributes timeAttrs = new UIStringAttributes {
            ForegroundColor = UIColor.FromRGB (0x03, 0xA9, 0xF3),
            BackgroundColor = UIColor.Clear,
            Font = UIFont.FromName ("HelveticaNeue", 10f)
        };

        TextDrawingData dateTextData;
        RectDrawingData timeBarData;
        RectDrawingData moneyBarData;
        TextDrawingData symbolTextData;
        TextDrawingData timeTextData;

        CALayer timeBar;
        CATextLayer symbolText;
        CALayer moneyBar;
        CATextLayer timeText;

        const float minimalTimeValue = 2;

        public SingleBar ( ChartSurface chartSurface, string date, float money, float time, string symbol, float x, float y)
        {
            Xpos = x;
            YPos = y;
            surface = chartSurface;

            if (time == 0) {
                time = minimalTimeValue;
            }

            dateTextData = new TextDrawingData (date, Xpos, YPos + 8);
            timeBarData = new RectDrawingData (Xpos + dateSpace, YPos, time, barHeight, 2);
            moneyBarData = new RectDrawingData (Xpos + dateSpace, YPos, money, barHeight, 1);
            symbolTextData = new TextDrawingData (symbol, timeBarData.Width + money / 2, YPos + 9);
            timeTextData = new TextDrawingData (time.ToString (), timeBarData.Width , YPos + 18);

            // create
            surface.DrawText (dateTextData, dateAttrs);
            timeBar = surface.DrawBarOnLayer (timeBarData);
            moneyBar = surface.DrawBarOnLayer (moneyBarData);
            symbolText = surface.DrawTextOnLayer (symbolTextData, symbolAttrs);
            timeText = surface.DrawTextOnLayer (timeTextData, timeAttrs);

            // paint
            paintChart ();
        }

        public void ReloadData ( string date, float money, float time, string symbol)
        {
            if (time == 0) {
                time = minimalTimeValue;
            }
            dateTextData = new TextDrawingData (date, Xpos, YPos + 8);
            timeBarData = new RectDrawingData ( Xpos + dateSpace, YPos, time, barHeight, 2);
            moneyBarData = new RectDrawingData (Xpos + dateSpace, YPos, money, barHeight, 1);
            symbolTextData = new TextDrawingData (symbol, timeBarData.Width + money / 2, YPos + 9);
            timeTextData = new TextDrawingData (time.ToString (), timeBarData.Width , YPos + 18);

            // paint
            paintChart ();
        }

        void paintChart()
        {
            symbolText.Hidden = (moneyBarData.Width < 10);
            timeText.Hidden = (timeBarData.Width == minimalTimeValue);

            // animate
            var colorAnimToBlue = CABasicAnimation.FromKeyPath ("backgroundColor");
            colorAnimToBlue.TimingFunction = CAMediaTimingFunction.FromName (CAMediaTimingFunction.EaseInEaseOut);
            colorAnimToBlue.From = NSObject.FromObject (surface.Colors [3].CGColor.Handle);
            colorAnimToBlue.To = NSObject.FromObject (surface.Colors [2].CGColor.Handle);
            colorAnimToBlue.Duration = 1;

            var colorAnimToGray = CABasicAnimation.FromKeyPath ("backgroundColor");
            colorAnimToGray.TimingFunction = CAMediaTimingFunction.FromName (CAMediaTimingFunction.EaseInEaseOut);
            colorAnimToGray.From = NSObject.FromObject (surface.Colors [2].CGColor.Handle);
            colorAnimToGray.To =NSObject.FromObject (surface.Colors [3].CGColor.Handle);
            colorAnimToGray.Duration = 1;

            if (timeBarData.Width == minimalTimeValue) {
                timeBar.AddAnimation (colorAnimToGray, "backgroundColor");
            } else {
                timeBar.AddAnimation (colorAnimToBlue, "backgroundColor");
            }

            var animation = CABasicAnimation.FromKeyPath ("transform.scale.x");
            animation.TimingFunction = CAMediaTimingFunction.FromName (CAMediaTimingFunction.EaseInEaseOut);
            animation.From = NSNumber.FromFloat (0f);
            animation.To = NSNumber.FromFloat (1f);
            animation.Duration = 1;

            var moveAnim = CABasicAnimation.FromKeyPath ("position");
            moveAnim.TimingFunction = CAMediaTimingFunction.FromName (CAMediaTimingFunction.EaseInEaseOut);
            moveAnim.From = NSValue.FromPointF ( new PointF ( timeTextData.X, timeTextData.Y));
            moveAnim.To =NSValue.FromPointF ( new PointF ( timeBarData.X + timeBarData.X + 20, timeTextData.Y));
            moveAnim.Duration = 0.9;

            var alphaAnim = CABasicAnimation.FromKeyPath ("opacity");
            alphaAnim.TimingFunction = CAMediaTimingFunction.FromName (CAMediaTimingFunction.Default);
            alphaAnim.From = NSNumber.FromFloat (0f);
            alphaAnim.To = NSNumber.FromFloat (1f);
            alphaAnim.Duration = 1.2;

            timeBar.AddAnimation (animation, "transform.scale.x");
            moneyBar.AddAnimation ( animation, "transform.scale.x");
            timeText.AddAnimation (moveAnim, "position");
            symbolText.AddAnimation (alphaAnim, "opacity");

            symbolText.Opacity = 1.0f;
            timeText.Position = new PointF (timeBarData.X + timeBarData.Width + 20, timeTextData.Y);
        }

    }
}
