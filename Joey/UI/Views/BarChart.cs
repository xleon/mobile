using System;
using System.Collections.Generic;
using Android.Content;
using Android.Util;
using Android.Views;
using Android.Graphics;
using Android.Graphics.Drawables;
using Android.Animation;

namespace Toggl.Joey.UI.Views
{
    public class BarChart : View
    {
        private List<BarItem> Bars = new List<BarItem> ();
        private List<String> BarTitles = new List<String> ();
        private List<String> LineTitles = new List<String> ();
        private Paint CanvasPaint = new Paint ();
        private Path CanvasPath = new Path ();
        private Boolean append = false;
        private int count = 7;
        private int barPadding = 5;
        private int barHeight = 60;
        private int bottomPadding = 30;
        private int timeColumn = 70;
        private int topPadding = 10;
        private int animationProgress;
        private bool animating;
        private Bitmap baseBitmap;
        private Color lineColor = Color.ParseColor ("#CCCCCC");
        private Color notBillableBarColor = Color.ParseColor ("#80D6FF");
        private Color billableBarColor = Color.ParseColor ("#00AEFF");
        private Color emptyBarColor = Color.ParseColor ("#666666");
        public double CeilingValue;


        public BarChart (Context context, IAttributeSet attrs) : base (context, attrs)
        {
        }

        public BarChart (Context context, IAttributeSet attrs, int defStyle) : base (context, attrs, defStyle)
        {
        }

        public void Reset ()
        {
            Bars.Clear ();
            BarTitles.Clear ();
            LineTitles.Clear ();
        }

        public void AddBar (BarItem point)
        {
            Bars.Add (point);
        }

        public void Refresh ()
        {
            StartAnimate ();
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
            return String.Format ("{0}:{1:mm}", (int)t.TotalHours, t);
        }

        private Bitmap BaseBitmap ()
        {
            float usableWidth = Width - timeColumn;

            Bitmap tempBitmap = Bitmap.CreateBitmap (Width, Height, Bitmap.Config.Argb8888);
            var canvas = new Canvas (tempBitmap);

            var backgroundPlate = new Rect ();
            backgroundPlate.Set (timeColumn, 0, Width, Height);
            CanvasPaint.Color = Color.White;
            canvas.DrawRect (backgroundPlate, CanvasPaint);

            CanvasPaint.Color = lineColor;
            CanvasPaint.StrokeWidth = 1;
            CanvasPaint.AntiAlias = true;

            int titleCount = 1;
            if (LineTitles.Count == 0) {
                LineTitles = EmptyStateLineTitles ();
            }
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

            CanvasPaint.Color = lineColor;
            CanvasPaint.StrokeWidth = 8;
            canvas.DrawLine (
                timeColumn + 4,
                0,
                timeColumn + 4,
                Height,
                CanvasPaint
            
            );
            return tempBitmap;
        }


        public override void Draw (Canvas canvas)
        {
            if (baseBitmap == null)
                baseBitmap = BaseBitmap ();
            canvas.DrawBitmap (baseBitmap, 0, 0, CanvasPaint);

            float bottomPadding = 40;
            float usableWidth = Width - timeColumn;
            float loadAnimation = animating ? (float)(animationProgress / 100F) : 1;
            CanvasPath.Reset ();

            var rectangle = new Rect ();
            var notBillableRectangle = new Rect ();

            int count = 0;
            foreach (BarItem p in Bars) {
                if ((int)p.Value == 0) {
                    rectangle.Set (
                        timeColumn,
                        (int)((barPadding * 2) * count + barPadding + barHeight * count + topPadding),
                        timeColumn + 8,
                        (int)((barPadding * 2) * count + barPadding + barHeight * (count + 1) + topPadding)
                    );
                    CanvasPaint.Color = emptyBarColor;

                    canvas.DrawRect (rectangle, CanvasPaint);

                } else {
                    if (p.Billable < p.Value) {
                        float notBillable = p.Value - p.Billable;
                        float totalWidth = (float)(usableWidth * (p.Value / CeilingValue));
                        float billableWidth = (float)(usableWidth * (p.Billable / CeilingValue));
                        float notBillableWidth = (float)(usableWidth * (notBillable / CeilingValue));
                        if ((loadAnimation * totalWidth) > billableWidth) {
                            rectangle.Set (
                                timeColumn,
                                (int)((barPadding * 2) * count + barPadding + barHeight * count + topPadding),
                                timeColumn + (int)(billableWidth),
                                (int)((barPadding * 2) * count + barPadding + barHeight * (count + 1) + topPadding)
                            );
                            notBillableRectangle.Set (
                                timeColumn,
                                (int)((barPadding * 2) * count + barPadding + barHeight * count + topPadding),
                                timeColumn + (int)(notBillableWidth * loadAnimation + billableWidth),
                                (int)((barPadding * 2) * count + barPadding + barHeight * (count + 1) + topPadding)
                            );

                            CanvasPaint.Color = notBillableBarColor;
                            canvas.DrawRect (notBillableRectangle, CanvasPaint);
                            CanvasPaint.Color = billableBarColor;
                            canvas.DrawRect (rectangle, CanvasPaint);
                        } else {
                            rectangle.Set (
                                timeColumn,
                                (int)((barPadding * 2) * count + barPadding + barHeight * count + topPadding),
                                timeColumn + (int)(loadAnimation * totalWidth),
                                (int)((barPadding * 2) * count + barPadding + barHeight * (count + 1) + topPadding)
                            );
                            CanvasPaint.Color = billableBarColor;
                            canvas.DrawRect (rectangle, CanvasPaint);
                        }
                    } else {
                        float totalWidth = (float)(usableWidth * (p.Value / CeilingValue));
                        rectangle.Set (
                            timeColumn,
                            (int)((barPadding * 2) * count + barPadding + barHeight * count + topPadding),
                            timeColumn + (int)(loadAnimation * totalWidth),
                            (int)((barPadding * 2) * count + barPadding + barHeight * (count + 1) + topPadding)
                        );
                        CanvasPaint.Color = billableBarColor;
                        canvas.DrawRect (rectangle, CanvasPaint);
                    }

                    if (animationProgress == 100) {
                        CanvasPaint.TextSize = 20;
                        var bounds = new Rect ();
                        var barTitle = FormatSeconds (p.Value); 
                        CanvasPaint.GetTextBounds (barTitle, 0, barTitle.Length, bounds);
                        canvas.DrawText (
                            barTitle,
                            timeColumn + 10 + (int)((usableWidth * (p.Value / CeilingValue))),
                            (int)((barPadding * 2) * count + barPadding + barHeight * count + topPadding + barHeight / 2 + bounds.Height () / 2),
                            CanvasPaint
                        );
                    }
                }
                CanvasPaint.Color = emptyBarColor;
                CanvasPaint.TextSize = 20;
                canvas.DrawText (
                    BarTitles [count],
                    0,
                    (int)((barPadding * 2) * count + barPadding + barHeight * count) + barHeight / 2 + barPadding + topPadding,
                    CanvasPaint
                );
                count++;
            }
        }

        protected override void OnMeasure (int widthMeasureSpec, int heightMeasureSpec)
        {
            int heightSize = (barPadding * 2 + barHeight) * count + bottomPadding;
            int widthSize = MeasureSpec.GetSize (widthMeasureSpec);

            SetMeasuredDimension (widthSize, heightSize);
        }

        List<string> EmptyStateLineTitles ()
        {
            var defaultList = new List<string> ();
            defaultList.Add ("2h");
            defaultList.Add ("4h");
            defaultList.Add ("6h");
            defaultList.Add ("8h");
            return defaultList;
        }

        public void StartAnimate ()
        {
            animating = true;
            var animator = ValueAnimator.OfInt (1, 100);
            animator.SetDuration (750);
            animator.Update += (sender, e) => AnimationProgress = (int)e.Animation.AnimatedValue;
            animator.Start ();
        }

        public int AnimationProgress {
            get {
                return animationProgress;
            }
            set {
                animationProgress = value;
                if (value == 100)
                    animating = false;
                PostInvalidate ();
            }
        }
    }

    public class BarItem
    {
        public Color Color { get; set; }

        public String Name { get; set; }

        public float Value { get; set; }

        public float Billable { get; set; }

        public Path Path { get; set; }

        public Region Region { get; set; }
    }
}

