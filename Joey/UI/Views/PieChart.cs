using System;
using System.Collections.Generic;
using Android.Content;
using Android.Views;
using Android.Graphics;
using Android.Util;
using Android.Widget;
using Android.Animation;
using Android.Text;

namespace Toggl.Joey.UI.Views
{
    public delegate void SliceClickedEventHandler (int position);

    public class PieChart : View
    {
        private List<PieSlice> slices = new List<PieSlice> ();
        private Paint paint = new Paint ();
        private Path path = new Path ();
        private int thickness = 65;
        private bool isLoading = true;
        private int indexSelected = -1;
        const float angleCorrection = 270;
        const int slicePadding = 20;
        private IOnSliceClickedListener listener;
        private int animationProgress;
        private float slideAnimationProgress;
        private Color emptyStateColor = Color.ParseColor ("#808080");

        public event SliceClickedEventHandler SliceClicked;

        public PieChart (Context context, IAttributeSet attrs) : base (context, attrs)
        {
        }

        public PieChart (Context context, IAttributeSet attrs, int defStyle) : base (context, attrs, defStyle)
        {
        }

        public void Reset ()
        {
            slices.Clear ();
            indexSelected = -1;
        }

        public override void Draw (Canvas canvas)
        {
            if (IsLoading)
                return;

            long totalValue = 0;
            long selectedSliceValue = 0;
            float currentAngle = 0;
            float currentSweep;
            float centerX = Width / 2;
            float centerY = Height / 2;
            float radius = centerX < centerY ? centerX : centerY;
            float innerRadius = radius - thickness;
            float loadAnimation = (float)animationProgress / 360F;
            float sliceSlideOutAnimation = slideAnimationProgress / (float)slicePadding;

            foreach (PieSlice slice in slices) {
                totalValue += slice.Value;
            }

            canvas.DrawColor (Color.Transparent);
            paint.Reset ();
            paint.AntiAlias = true;
            path.Reset ();

            var basePath = new Path ();
            basePath.AddCircle (centerX, centerY, radius - slicePadding, Path.Direction.Cw);
            basePath.AddCircle (centerX, centerY, innerRadius - slicePadding, Path.Direction.Ccw);
            var basePaint = new Paint ();
            basePaint.Color = Color.ParseColor ("#EDEDED");
            canvas.DrawPath (basePath, basePaint);

            if (slices.Count == 0) {
                Paint emptyText = new Paint ();
                emptyText.Color = emptyStateColor;
                emptyText.TextAlign = Paint.Align.Center;
                emptyText.AntiAlias = true;
                emptyText.TextSize = 30;
                canvas.DrawText (Resources.GetText (Resource.String.ReportsPieChartEmptyHeader), centerX, centerY, emptyText);

                var textPaint = new TextPaint ();
                textPaint.Color = emptyStateColor;
                textPaint.TextAlign = Paint.Align.Center;
                textPaint.AntiAlias = true;
                textPaint.TextSize = 20;

                StaticLayout emptyStateText = new StaticLayout (Resources.GetText (Resource.String.ReportsPieChartEmptyText), textPaint, 300, StaticLayout.Alignment.AlignNormal, 1, 0, false);
                canvas.Translate (centerX, centerY + 10);
                emptyStateText.Draw (canvas);
                return;
            }
            int count = 0;

            foreach (PieSlice slice in slices) {
                var slicePath = new Path ();
                paint.Color = slice.Color;
                if (indexSelected != count && listener != null && indexSelected != -1) {
                    paint.Alpha = (int)(255 - sliceSlideOutAnimation * 127F);
                }

                currentSweep = ((float)slice.Value / (float)totalValue) * (360);

                if ((int)currentSweep == 360 && animationProgress == 360) {
                    slicePath.AddCircle (centerX, centerY, radius - slicePadding, Path.Direction.Cw);
                    slicePath.AddCircle (centerX, centerY, innerRadius - slicePadding, Path.Direction.Ccw);
                } else {
                    slicePath.ArcTo (
                        new RectF (
                            centerX - radius + slicePadding,
                            centerY - radius + slicePadding,
                            centerX + radius - slicePadding,
                            centerY + radius - slicePadding
                        ),
                        loadAnimation * currentAngle + angleCorrection,
                        loadAnimation * currentSweep
                    );
                    slicePath.ArcTo (
                        new RectF (
                            centerX - innerRadius + slicePadding,
                            centerY - innerRadius + slicePadding,
                            centerX + innerRadius - slicePadding,
                            centerY + innerRadius - slicePadding
                        ),
                        loadAnimation * (currentAngle + currentSweep) + angleCorrection,
                        loadAnimation * -(currentSweep)
                    );
                }

                if (indexSelected == count && listener != null && (int)currentSweep != 360) {
                    var sliceSector = currentAngle + (currentSweep / 2);
                    var angleToRadian = sliceSector / (180 / Math.PI);
                    var dx = (float)Math.Sin (angleToRadian) * slicePadding;
                    var dy = (float)Math.Cos (angleToRadian) * slicePadding * -1;
                    slicePath.Offset (dx * sliceSlideOutAnimation, dy * sliceSlideOutAnimation);
                    selectedSliceValue = slice.Value;
                }

                slicePath.Close ();

                slice.Path = slicePath;
                slice.Region = new Region (
                    (int)(centerX - radius),
                    (int)(centerY - radius),
                    (int)(centerX + radius),
                    (int)(centerY + radius)
                );
                canvas.DrawPath (slicePath, paint);
                currentAngle += currentSweep;
                count++;
            }


            Paint text = new Paint ();
            text.Color = Color.Black;
            text.TextAlign = Paint.Align.Center;
            text.AntiAlias = true;
            text.TextSize = 30;
            canvas.DrawText (FormatMilliseconds (selectedSliceValue > 0 ? selectedSliceValue : totalValue), centerX, centerY, text);
        }

        public override bool OnTouchEvent (MotionEvent ev)
        {
            Point point = new Point ();
            point.X = (int)ev.GetX ();
            point.Y = (int)ev.GetY ();
            int clickedSlice = -1;
            int count = 0;
            foreach (PieSlice slice in slices) {
                Region r = new Region ();
                r.SetPath (slice.Path, slice.Region);
                if (r.Contains (point.X, point.Y) && ev.Action == MotionEventActions.Up) {
                    clickedSlice = count;
                }
                count++;
            }
            if (clickedSlice == -1 && ev.Action == MotionEventActions.Up) {
                indexSelected = -1;
            } else if (clickedSlice == indexSelected) {
                return true;
            } else {
                indexSelected = clickedSlice;
            }
            if (ev.Action == MotionEventActions.Up) {
                OnSliceSelected ();
                StartSliceSlideAnimation ();
            }
            return true;
        }

        private string FormatMilliseconds (long ms)
        {
            var timeSpan = TimeSpan.FromMilliseconds (ms);
            return String.Format ("{0}:{1:mm\\:ss}", Math.Floor (timeSpan.TotalHours).ToString ("00"), timeSpan);
        }

        protected virtual void OnSliceSelected ()
        {
            var sliceClicked = SliceClicked;
            if (sliceClicked != null) {
                sliceClicked (indexSelected);
            }
        }

        public List<PieSlice> Slices {
            get {
                return slices;
            }
            set {
                slices = value;
                PostInvalidate ();
            }
        }

        public bool IsLoading {
            get {
                return isLoading;
            }
            set {
                if (isLoading == value)
                    return;
                isLoading = value;
                PostInvalidate ();
            }
        }

        public PieSlice GetSlice (int index)
        {
            return slices [index];
        }

        public void AddSlice (PieSlice slice)
        {
            this.slices.Add (slice);
            PostInvalidate ();
        }

        public void SetOnSliceClickedListener (IOnSliceClickedListener listener)
        {
            this.listener = listener;
        }

        public int Thickness {
            get {
                return thickness;
            }
            set {
                thickness = value;
                PostInvalidate ();
            }
        }

        public void RemoveSlices ()
        {
            for (int i = slices.Count - 1; i >= 0; i--) {
                slices.Remove (slices [i]);
            }
            PostInvalidate ();
        }

        public interface IOnSliceClickedListener
        {
            void OnClick (int index);
        }

        public void SelectSlice (int position)
        {
            indexSelected = position;
            StartSliceSlideAnimation ();
//            PostInvalidate ();
        }

        public void StartDrawAnimation ()
        {
            var animator = ValueAnimator.OfInt (1, 360);
            animator.SetDuration (750);
            animator.Update += (sender, e) => AnimationProgress = (int)e.Animation.AnimatedValue;
            animator.Start ();
        }

        public void StartSliceSlideAnimation ()
        {
            var animator = ValueAnimator.OfInt (1, slicePadding);
            animator.SetDuration (300);
            animator.Update += (sender, e) => SlideAnimationProgress = (float)e.Animation.AnimatedValue;
            animator.Start ();
        }

        public float SlideAnimationProgress {
            get {
                return slideAnimationProgress;
            }
            set {
                slideAnimationProgress = value;
                PostInvalidate ();
            }
        }

        public int AnimationProgress {
            get {
                return animationProgress;
            }
            set {
                animationProgress = value;
                PostInvalidate ();
            }
        }

    }

    public class PieSlice
    {
        public string Title { get; set; }

        public Color Color { get; set; }

        public long Value { get; set; }

        public Path Path { get; set; }

        public Region Region { get; set; }
    }
}


