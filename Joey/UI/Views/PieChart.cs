using System;
using System.Collections.Generic;
using Android.Content;
using Android.Views;
using Android.Graphics;
using Android.Util;

namespace Toggl.Joey.UI.Views
{
    public class PieChart : View
    {
        private List<PieSlice> slices = new List<PieSlice> ();
        private Paint paint = new Paint ();
        private Path path = new Path ();

        private int indexSelected = -1;
        private int thickness = 75;
        private IOnSliceClickedListener listener;

        public PieChart (Context context, IAttributeSet attrs) : base (context, attrs)
        {
        }

        public PieChart (Context context, IAttributeSet attrs, int defStyle) : base (context, attrs, defStyle)
        {
        }

        public override void Draw (Canvas canvas)
        {
            canvas.DrawColor (Color.Transparent);
            paint.Reset ();
            paint.AntiAlias = true;
            path.Reset ();

            int totalValue = 0;
            float currentAngle = 270;
            float currentSweep;
            float padding = 0;
            float centerX = Width / 2;
            float centerY = Height / 2;
            float radius;

            if (centerX < centerY) {
                radius = centerX;
            } else {
                radius = centerY;
            }
            radius -= padding;
            float innerRadius = radius - thickness;

            foreach (PieSlice slice in slices) {
                totalValue += (int)slice.Value;
            }

            int count = 0;
            foreach (PieSlice slice in slices) {
                var slicePath = new Path ();

                paint.Color = slice.Color;
                currentSweep = (slice.Value / totalValue) * (360);
                slicePath.ArcTo (
                    new RectF (
                        centerX - radius,
                        centerY - radius,
                        centerX + radius,
                        centerY + radius
                    ),
                    currentAngle + padding,
                    currentSweep - padding
                );
                slicePath.ArcTo (
                    new RectF (
                        centerX - innerRadius,
                        centerY - innerRadius,
                        centerX + innerRadius,
                        centerY + innerRadius
                    ),
                    (currentAngle + padding) + (currentSweep - padding),
                    -(currentSweep - padding)
                );
                slicePath.Close ();

                slice.Path = slicePath;
                slice.Region = new Region (
                    (int)(centerX - radius),
                    (int)(centerY - radius),
                    (int)(centerX + radius),
                    (int)(centerY + radius)
                );
                canvas.DrawPath (slicePath, paint);

                int selectedPadding = 3;

                if (indexSelected == count && listener != null) {
                    path.Reset ();
                    paint.Color = slice.Color;
                    paint.Color = Color.ParseColor ("#33B5E5");
                    paint.Alpha = 100;

                    if (slices.Count > 1) {
                        path.ArcTo (
                            new RectF (
                                centerX - radius - (selectedPadding * 2),
                                centerY - radius - (selectedPadding * 2),
                                centerX + radius + (padding * 2),
                                centerY + radius + (padding * 2)
                            ),
                            currentAngle,
                            currentSweep + padding
                        );
                        path.ArcTo (
                            new RectF (
                                centerX - innerRadius + (selectedPadding * 2),
                                centerY - innerRadius + (selectedPadding * 2),
                                centerX + innerRadius - (selectedPadding * 2),
                                centerY + innerRadius - (selectedPadding * 2)
                            ),
                            currentAngle + currentSweep + selectedPadding,
                            -(currentSweep + selectedPadding)
                        );
                        path.Close ();
                    } else {
                        path.AddCircle (centerX, centerY, radius + selectedPadding, Path.Direction.Cw);
                    }

                    canvas.DrawPath (path, paint);
                    paint.Alpha = 255;
                }

                currentAngle = currentAngle + currentSweep;

                count++;
            }
        }

        public bool OnTouchEvent(MotionEvent ev) {
            Console.WriteLine ("touched");

            Point point = new Point();
            point.X = (int)ev.GetX();
            point.Y = (int) ev.GetY();

            int count = 0;
            foreach (PieSlice slice in slices){
                Region r = new Region();
                r.SetPath(slice.Path, slice.Region);
                if (r.Contains(point.X, point.Y) && ev.Action == MotionEventActions.Down) {
                    indexSelected = count;
                } else if (ev.Action == MotionEventActions.Up){
                    if (r.Contains(point.X, point.Y) && listener != null) {
                        if (indexSelected > -1){
                            listener.OnClick(indexSelected);
                        }
                        indexSelected = -1;
                    }
                }
                count++;
            }

            if (ev.Action == MotionEventActions.Down || ev.Action == MotionEventActions.Up){
                PostInvalidate ();
            }

            return true;
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
    }



    public class PieSlice
    {
        private float value;
        private String title;
        private Path path;
        private Region region;

        public string Title {
            get;
            set;
        }

        public Color Color {
            get;
            set;
        }

        public float Value {
            get;
            set;
        }

        public Path Path {
            get;
            set;
        }

        public Region Region {
            get;
            set;
        }
    }
}


