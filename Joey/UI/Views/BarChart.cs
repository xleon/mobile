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
        public int CeilingValue;
        private List<BarItem> bars = new List<BarItem> ();
        private List<String> barTitles = new List<String> ();
        private List<String> lineTitles = new List<String> ();
        private Paint canvasPaint = new Paint ();
        private Path canvasPath = new Path ();
        private bool append = false;
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

        public BarChart (Context context, IAttributeSet attrs) : base (context, attrs)
        {
        }

        public BarChart (Context context, IAttributeSet attrs, int defStyle) : base (context, attrs, defStyle)
        {
        }

        public void Reset ()
        {
            bars.Clear ();
            barTitles.Clear ();
            lineTitles.Clear ();
        }

        public void AddBar (BarItem point)
        {
            bars.Add (point);
        }

        public void Refresh ()
        {
            StartAnimate ();
        }


        public List<string> BarTitles
        {
            get {
                return barTitles;
            } set {
                barTitles = value;
                PostInvalidate ();
            }
        }

        public List<string> LineTitles
        {
            get {
                return lineTitles;
            } set {
                lineTitles = value;
                PostInvalidate ();
            }
        }


        public bool Append
        {
            get {
                return append;
            } set {
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
            canvasPaint.Color = Color.White;
            canvas.DrawRect (backgroundPlate, canvasPaint);

            canvasPaint.Color = lineColor;
            canvasPaint.StrokeWidth = 1;
            canvasPaint.AntiAlias = true;

            int titleCount = 1;
            if (lineTitles.Count == 0) {
                lineTitles = EmptyStateLineTitles ();
            }
            foreach (var title in lineTitles) {
                canvas.DrawLine (
                    timeColumn + usableWidth / 5 * titleCount,
                    topPadding,
                    timeColumn + usableWidth / 5 * titleCount,
                    Height - bottomPadding + topPadding,
                    canvasPaint
                );

                canvasPaint.TextSize = 20;
                var bounds = new Rect ();
                canvasPaint.GetTextBounds (title, 0, title.Length, bounds);
                canvas.DrawText (
                    title,
                    timeColumn + usableWidth / 5 * titleCount - bounds.Width () / 2,
                    Height - bottomPadding / 2 + topPadding,
                    canvasPaint
                );

                titleCount++;
            }

            canvasPaint.Color = lineColor;
            canvasPaint.StrokeWidth = 8;
            canvas.DrawLine (
                timeColumn + 4,
                0,
                timeColumn + 4,
                Height,
                canvasPaint

            );
            return tempBitmap;
        }


        public override void Draw (Canvas canvas)
        {
            if (baseBitmap == null) {
                baseBitmap = BaseBitmap ();
            }
            canvas.DrawBitmap (baseBitmap, 0, 0, canvasPaint);

            float bottomPadding = 40;
            float usableWidth = Width - timeColumn;
            float ceilingSeconds = (float)CeilingValue * 3600F;
            float loadAnimation = animating ? (float) (animationProgress / 100F) : 1;
            canvasPath.Reset ();

            var rectangle = new Rect ();
            var notBillableRectangle = new Rect ();

            int count = 0;
            foreach (BarItem p in bars) {
                if ((int)p.Value == 0) {
                    rectangle.Set (
                        timeColumn,
                        (int) ((barPadding * 2) * count + barPadding + barHeight * count + topPadding),
                        timeColumn + 8,
                        (int) ((barPadding * 2) * count + barPadding + barHeight * (count + 1) + topPadding)
                    );
                    canvasPaint.Color = emptyBarColor;

                    canvas.DrawRect (rectangle, canvasPaint);

                } else {
                    if (p.Billable < p.Value) {
                        float notBillable = p.Value - p.Billable;
                        float totalWidth = (float) (usableWidth * (p.Value / ceilingSeconds));
                        float billableWidth = (float) (usableWidth * (p.Billable / ceilingSeconds));
                        float notBillableWidth = (float) (usableWidth * (notBillable / ceilingSeconds));
                        if ((loadAnimation * totalWidth) > billableWidth) {
                            rectangle.Set (
                                timeColumn,
                                (int) ((barPadding * 2) * count + barPadding + barHeight * count + topPadding),
                                timeColumn + (int) (billableWidth),
                                (int) ((barPadding * 2) * count + barPadding + barHeight * (count + 1) + topPadding)
                            );
                            notBillableRectangle.Set (
                                timeColumn,
                                (int) ((barPadding * 2) * count + barPadding + barHeight * count + topPadding),
                                timeColumn + (int) (notBillableWidth * loadAnimation + billableWidth),
                                (int) ((barPadding * 2) * count + barPadding + barHeight * (count + 1) + topPadding)
                            );

                            canvasPaint.Color = notBillableBarColor;
                            canvas.DrawRect (notBillableRectangle, canvasPaint);
                            canvasPaint.Color = billableBarColor;
                            canvas.DrawRect (rectangle, canvasPaint);
                        } else {
                            rectangle.Set (
                                timeColumn,
                                (int) ((barPadding * 2) * count + barPadding + barHeight * count + topPadding),
                                timeColumn + (int) (loadAnimation * totalWidth),
                                (int) ((barPadding * 2) * count + barPadding + barHeight * (count + 1) + topPadding)
                            );
                            canvasPaint.Color = billableBarColor;
                            canvas.DrawRect (rectangle, canvasPaint);
                        }
                    } else {
                        float totalWidth = (float) (usableWidth * (p.Value / ceilingSeconds));
                        rectangle.Set (
                            timeColumn,
                            (int) ((barPadding * 2) * count + barPadding + barHeight * count + topPadding),
                            timeColumn + (int) (loadAnimation * totalWidth),
                            (int) ((barPadding * 2) * count + barPadding + barHeight * (count + 1) + topPadding)
                        );
                        canvasPaint.Color = billableBarColor;
                        canvas.DrawRect (rectangle, canvasPaint);
                    }

                    if (animationProgress == 100) {
                        canvasPaint.TextSize = 20;
                        var bounds = new Rect ();
                        var barTitle = FormatSeconds (p.Value);
                        canvasPaint.GetTextBounds (barTitle, 0, barTitle.Length, bounds);
                        canvas.DrawText (
                            barTitle,
                            timeColumn + 10 + (int) ((usableWidth * (p.Value / ceilingSeconds))),
                            (int) ((barPadding * 2) * count + barPadding + barHeight * count + topPadding + barHeight / 2 + bounds.Height () / 2),
                            canvasPaint
                        );
                    }
                }
                canvasPaint.Color = emptyBarColor;
                canvasPaint.TextSize = 20;
                canvas.DrawText (
                    barTitles [count],
                    0,
                    (int) ((barPadding * 2) * count + barPadding + barHeight * count) + barHeight / 2 + barPadding + topPadding,
                    canvasPaint
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

        public int AnimationProgress
        {
            get {
                return animationProgress;
            } set {
                animationProgress = value;
                if (value == 100) {
                    animating = false;
                }
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

