using Android.Content;
using Android.Graphics;
using Android.Util;
using Android.Views;
using Android.Widget;
using System;
using System.Collections.Generic;
using XPlatUtils;
using Android.Animation;

namespace Toggl.Joey.UI.Views
{
    public class BarChartView : LinearLayout
    {
        private List<String> BarTitles = new List<String> ();
        private List<String> LineTitles = new List<String> ();
        private Paint CanvasPaint = new Paint ();
        private Path CanvasPath = new Path ();
        private Bitmap FullImage;
        private Boolean shouldUpdate = false;
        private Boolean append = false;
        private int count = 7;
        private int barPadding = 5;
        private int barHeight = 60;
        private int bottomPadding = 30;
        private int timeColumn = 70;
        private int topPadding = 10;
        public double CeilingValue;
        private Bar TestBar;
        private Color LineColor = Color.ParseColor("#CCCCCC");


        public BarChartView (Context context) : base (context)
        {
        }

        public BarChartView (Context context, IAttributeSet attrs) : base (context, attrs)
        {
        }

        public BarChartView (Context context, IAttributeSet attrs, int defStyle) : base (context, attrs, defStyle)
        {
        }

        protected override void DispatchDraw (Canvas canvas)
        {
            if (FullImage == null || shouldUpdate) {
                FullImage = Bitmap.CreateBitmap (Width, Height, Bitmap.Config.Argb8888);
                Canvas paintCanvas = new Canvas (FullImage);
                paintCanvas.DrawColor (Color.Transparent);

                int bottomPadding = 40;
                int usableWidth = Width - timeColumn;

                var backgroundPlate = new Rect ();
                backgroundPlate.Set (timeColumn, 0, Width, Height);
                CanvasPaint.Color = Color.White;
                canvas.DrawRect (backgroundPlate, CanvasPaint);

                CanvasPaint.Color = LineColor;
                CanvasPaint.StrokeWidth = 1;
                CanvasPaint.AntiAlias = true;

                int titleCount = 1;
                foreach (var title in LineTitles) {

                    canvas.DrawLine (
                        timeColumn + usableWidth / 5 * titleCount,
                        topPadding,
                        timeColumn + usableWidth / 5 * titleCount,
                        Height - bottomPadding + topPadding,
                        CanvasPaint
                    );

                    CanvasPaint.TextSize = 20;
                    var bounds = new Rect ();
                    CanvasPaint.GetTextBounds (title, 0, title.Length, bounds);
                    canvas.DrawText (
                        title,
                        timeColumn + usableWidth / 5 * titleCount - bounds.Width () / 2,
                        Height - bottomPadding / 2 + topPadding,
                        CanvasPaint
                    );

                    titleCount++;
                }

                CanvasPaint.Color = LineColor;
                CanvasPaint.StrokeWidth = 8;
                canvas.DrawLine (
                    timeColumn + 4,
                    0,
                    timeColumn + 4,
                    Height,
                    CanvasPaint
                );

                CanvasPath.Reset ();

                canvas.DrawBitmap (FullImage, 0, 0, null);
            }
//            var ctx = ServiceContainer.Resolve<Context> ();
//            var test = new Bar (ctx);
//            test.Click += (sender, e) => test.StartAnimate ();
//            test.Value = 200;
//            var parameters = new BarChartView.LayoutParams(BarChartView.LayoutParams.MatchParent, BarChartView.LayoutParams.WrapContent);
//            test.LayoutParameters = new BarChartView.LayoutParams(300, 50);
////            AddView (test, parameters);
//            test.Draw (canvas);
//            Console.WriteLine ("DispatchDraw, childCount: {0}", ChildCount);
            base.DispatchDraw (canvas);
        }

        public void AddTestBar()
        {

        }
        public override void AddView(View v)
        {
            Console.WriteLine ("Addview test:");
            Console.WriteLine ("Addview {0}", v.LayoutParameters);

            base.AddView (v);

        }
        public void Reset ()
        {
            BarTitles.Clear ();
            LineTitles.Clear ();
        }

        public void Refresh ()
        {
            shouldUpdate = true;
            PostInvalidate ();
        }

        public void SetBarTitles (List<String> titles)
        {
            BarTitles = titles;
        }

        public void SetLineTitles (List<String> titles)
        {
            LineTitles = titles;
        }

        public Boolean Append {
            get {
                return append;
            }
            set {
                append = value;
            }
        }

        private string FormatSeconds (double seconds)
        {
            var t = TimeSpan.FromSeconds (seconds);
            return String.Format ("{0:hh\\:mm}", t);
        }

        protected override void OnMeasure (int widthMeasureSpec, int heightMeasureSpec)
        {
            int heightSize = (barPadding * 2 + barHeight) * count + bottomPadding;
            Console.WriteLine ("height: {0}", heightSize);
            int widthSize = MeasureSpec.GetSize (widthMeasureSpec);

            SetMeasuredDimension (widthSize, heightSize);
        }


    }
}

