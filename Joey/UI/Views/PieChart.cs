using System;
using System.Collections.Generic;
using Android.Content;
using Android.Views;
using Android.Graphics;
using Android.Util;
using Android.Widget;

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
        private IOnSliceClickedListener listener;

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
        }

        public override void Draw (Canvas canvas)
        {
            if (IsLoading)
                return;
            canvas.DrawColor (Color.Transparent);
            paint.Reset ();
            paint.AntiAlias = true;
            path.Reset ();

            long totalValue = 0;
            float currentAngle = 270;
            float currentSweep;
            float centerX = Width / 2;
            float centerY = Height / 2;
            float radius;
            long selectedValue = 0;
            int slicePadding = 10;

            if (centerX < centerY) {
                radius = centerX;
            } else {
                radius = centerY;
            }
            float innerRadius = radius - thickness;

            foreach (PieSlice slice in slices) {
                totalValue += slice.Value;
            }
            int count = 0;
            foreach (PieSlice slice in slices) {
                var slicePath = new Path ();
                paint.Color = slice.Color;
                if (indexSelected != count && listener != null && indexSelected != -1) {
                    paint.Alpha = 100;
                }

                currentSweep = ((float)slice.Value / (float)totalValue) * (360);

                if ((int)currentSweep == 360) {
                    slicePath.AddCircle (centerX, centerY, radius, Path.Direction.Cw);
                    slicePath.AddCircle (centerX, centerY, innerRadius, Path.Direction.Ccw);
                } else {
                    slicePath.ArcTo (
                        new RectF (
                            centerX - radius + slicePadding,
                            centerY - radius + slicePadding,
                            centerX + radius - slicePadding,
                            centerY + radius - slicePadding
                        ),
                        currentAngle,
                        currentSweep
                    );
                    slicePath.ArcTo (
                        new RectF (
                            centerX - innerRadius + slicePadding,
                            centerY - innerRadius + slicePadding,
                            centerX + innerRadius - slicePadding,
                            centerY + innerRadius - slicePadding
                        ),
                        currentAngle + currentSweep,
                        -(currentSweep)
                    );
                }

                if (indexSelected == count && listener != null && (int)currentSweep != 360) {
                    var sliceSector = currentAngle + (currentSweep / 2) - 270;
                    var angleToRadian = sliceSector / (180 / Math.PI);
                    var dx = (float)Math.Sin (angleToRadian) * slicePadding;
                    var dy = (float)Math.Cos (angleToRadian) * slicePadding * -1;
                    slicePath.Offset (dx, dy);
                    selectedValue = slice.Value;
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
                currentAngle = currentAngle + currentSweep;
                count++;
            }

            Paint text = new Paint ();
            text.Color = Color.Black;
            text.TextAlign = Paint.Align.Center;
            text.TextSize = 30;
            canvas.DrawText (FormatMilliseconds (selectedValue > 0 ? selectedValue : totalValue), centerX, centerY, text);
        }

        public override bool OnTouchEvent (MotionEvent ev)
        {
            Point point = new Point ();
            point.X = (int)ev.GetX ();
            point.Y = (int)ev.GetY ();
            indexSelected = -1;
            int count = 0;
            foreach (PieSlice slice in slices) {
                Region r = new Region ();
                r.SetPath (slice.Path, slice.Region);
                if (r.Contains (point.X, point.Y) && ev.Action == MotionEventActions.Up) {
                    indexSelected = count;
                }
                count++;
            }
            if (ev.Action == MotionEventActions.Up) {
                PostInvalidate ();
                OnSliceSelected ();
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
            PostInvalidate ();
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


