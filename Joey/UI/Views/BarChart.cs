using System;
using System.Collections.Generic;
using Android.Content;
using Android.Util;
using Android.Views;
using Android.Graphics;
using Android.Graphics.Drawables;

namespace Toggl.Joey.UI.Views
{
    public class BarChart : View
    {
        private List<BarItem> Bars = new List<BarItem> ();
        private List<String> BarTitles = new List<String> ();
        private List<String> LineTitles = new List<String> ();
        private Paint CanvasPaint = new Paint ();
        private Path CanvasPath = new Path ();
        //private OnBarClickedListener listener;
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

        public BarChart (Context context, IAttributeSet attrs) : base (context, attrs)
        {
        }

        public BarChart (Context context, IAttributeSet attrs, int defStyle) : base (context, attrs, defStyle)
        {
        }

        public void AddBar (BarItem point)
        {
            Bars.Add (point);
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

        public void SetBars (List<BarItem> points)
        {
            Bars = points;
            PostInvalidate ();
        }

        public List<BarItem> GetBars ()
        {
            return Bars;
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

        public override void Draw (Canvas canvas)
        {
            if (FullImage == null || shouldUpdate) {
                FullImage = Bitmap.CreateBitmap (Width, Height, Bitmap.Config.Argb8888);
                Canvas paintCanvas = new Canvas (FullImage);
                paintCanvas.DrawColor (Color.Transparent);

                int selectPadding = 4;
                float bottomPadding = 40;
                float usableWidth = Width - timeColumn;

                var backgroundPlate = new Rect ();
                backgroundPlate.Set (
                    timeColumn,
                    0,
                    Width, 
                    Height
                );
                CanvasPaint.Color = Color.White;
                canvas.DrawRect (backgroundPlate, CanvasPaint);

                CanvasPaint.Color = Color.ParseColor ("#CCCCCC");
                CanvasPaint.StrokeWidth = 1;
                CanvasPaint.AntiAlias = true;

                int titleCount = 1;
                foreach (var title in LineTitles) {

                    canvas.DrawLine (
                        timeColumn + usableWidth / 5 * titleCount,
                        0,
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

                CanvasPaint.Color = Color.ParseColor ("#CCCCCC");
                CanvasPaint.StrokeWidth = 8;
                canvas.DrawLine (
                    timeColumn + 4,
                    0,
                    timeColumn + 4,
                    Height,
                    CanvasPaint
                );
                    
                var rectangle = new Rect ();

                CanvasPath.Reset ();

                int count = 0;
                foreach (BarItem p in Bars) {
                    if ((int)p.Value == 0) {
                        p.Color = Color.ParseColor ("#666666");
                        rectangle.Set (
                            timeColumn,
                            (int)((barPadding * 2) * count + barPadding + barHeight * count + topPadding),
                            timeColumn + 8,
                            (int)((barPadding * 2) * count + barPadding + barHeight * (count + 1) + topPadding)
                        );
                    } else {
                        rectangle.Set (
                            timeColumn,
                            (int)((barPadding * 2) * count + barPadding + barHeight * count + topPadding),
                            timeColumn + (int)((usableWidth * (p.Value / CeilingValue))),
                            (int)((barPadding * 2) * count + barPadding + barHeight * (count + 1) + topPadding)
                        );
                        CanvasPaint.Color = Color.ParseColor ("#00AEFF");
                        CanvasPaint.TextSize = 20;
                        canvas.DrawText (
                            FormatSeconds (p.Value),
                            timeColumn + (int)((usableWidth * (p.Value / CeilingValue))),
                            (int)((barPadding * 2) * count + barPadding + barHeight * count + topPadding),
                            CanvasPaint
                        );
                    }

                    CanvasPath.AddRect (
                        new RectF (
                            rectangle.Left - selectPadding,
                            rectangle.Top - selectPadding,
                            rectangle.Right + selectPadding,
                            rectangle.Bottom + selectPadding
                        ),
                        Path.Direction.Cw
                    );
                    p.Path = CanvasPath;
                    p.Region = new Region (
                        rectangle.Left - selectPadding,
                        rectangle.Top - selectPadding,
                        rectangle.Right + selectPadding,
                        rectangle.Bottom + selectPadding
                    );

                    CanvasPaint.Color = p.Color;
                    canvas.DrawRect (rectangle, CanvasPaint);

                    CanvasPaint.Color = Color.ParseColor ("#666666");
                    CanvasPaint.TextSize = 20;
                    canvas.DrawText (
                        BarTitles [count],
                        0,
                        (int)((barPadding * 2) * count + barPadding + barHeight * count) + barHeight / 2 + barPadding + topPadding,
                        CanvasPaint
                    );

                    count++;
                }
                shouldUpdate = false;
            }
            canvas.DrawBitmap (FullImage, 0, 0, null);
        }

        protected override void OnMeasure (int widthMeasureSpec, int heightMeasureSpec)
        {
            int heightSize = (barPadding * 2 + barHeight) * count + bottomPadding;
            int widthSize = MeasureSpec.GetSize (widthMeasureSpec);

            SetMeasuredDimension (widthSize, heightSize);
        }

    }

    public class BarItem
    {
        public Color Color { get; set; }

        public String Name { get; set; }

        public float Value { get; set; }

        public Path Path { get; set; }

        public Region Region { get; set; }
    }
}

