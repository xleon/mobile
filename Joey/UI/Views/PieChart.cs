using System;
using System.Collections.Generic;
using System.Linq;
using Android.Animation;
using Android.Content;
using Android.Graphics;
using Android.Text;
using Android.Util;
using Android.Views;
using Android.Widget;
using Toggl.Phoebe.Data.Models;
using Toggl.Phoebe.Data.Reports;

namespace Toggl.Joey.UI.Views
{
    public class PieChart : ViewGroup
    {
        private const float ActiveSliceScale = 1.1f;
        private const float NonActiveSliceScale = 1f;
        private static Color emptyPieColor = Color.ParseColor ("#EDEDED");
        private readonly List<SliceView> slices = new List<SliceView> ();
        private SliceView backgroundView;
        private View loadingOverlayView;
        private View emptyOverlayView;
        private View statsOverlayView;
        private TextView statsTimeTextView;
        private TextView statsMoneyTextView;
        private int defaultRadius;
        private int overlayInset;
        private int activeSlice = -1;
        private SummaryReportView data;
        private Animator currentRevealAnimation;
        private Animator currentSelectAnimation;

        public PieChart (Context context, IAttributeSet attrs) : base (context, attrs)
        {
            Initialize (context);
        }

        public PieChart (Context context, IAttributeSet attrs, int defStyle) : base (context, attrs, defStyle)
        {
            Initialize (context);
        }

        private void Initialize (Context ctx)
        {
            var dm = ctx.Resources.DisplayMetrics;
            var inflater = LayoutInflater.FromContext (ctx);

            overlayInset = (int)TypedValue.ApplyDimension (ComplexUnitType.Dip, 45, dm);

            backgroundView = new SliceView (ctx) {
                StartAngle = 0,
                EndAngle = 360,
                Color = emptyPieColor,
            };
            AddView (backgroundView);

            loadingOverlayView = inflater.Inflate (Resource.Layout.PieChartLoading, this, false);
            AddView (loadingOverlayView);

            emptyOverlayView = inflater.Inflate (Resource.Layout.PieChartEmpty, this, false);
            emptyOverlayView.Visibility = ViewStates.Gone;
            AddView (emptyOverlayView);

            statsOverlayView = inflater.Inflate (Resource.Layout.PieChartStats, this, false);
            statsOverlayView.Visibility = ViewStates.Gone;
            AddView (statsOverlayView);

            statsTimeTextView = statsOverlayView.FindViewById<TextView> (Resource.Id.TimeTextView);
            statsMoneyTextView = statsOverlayView.FindViewById<TextView> (Resource.Id.MoneyTextView);

            Click += delegate {
                // Deselect slices on click. The Clickable property is set to true only when a slice is selected.
                ActiveSlice = -1;
            };
            Clickable = false;
        }

        protected override void OnMeasure (int widthMeasureSpec, int heightMeasureSpec)
        {
            var dm = Resources.DisplayMetrics;

            var width = (int)Math.Max (
                            TypedValue.ApplyDimension (ComplexUnitType.Dip, 270, dm),
                            MeasureSpec.GetSize (widthMeasureSpec)
                        );
            var height = (int)TypedValue.ApplyDimension (ComplexUnitType.Dip, 270, dm);

            // Determine default radius for slices
            var sliceSide = Math.Min (width, height);
            var oldRadius = defaultRadius;
            defaultRadius = (int) (sliceSide / 2 / ActiveSliceScale);

            // Readjust the radius if it has changed
            if (defaultRadius != oldRadius) {
                backgroundView.Radius = defaultRadius;
                foreach (var slice in slices) {
                    slice.Radius = defaultRadius;
                }
            }

            // Measure overlays
            var overlaySide = (defaultRadius - overlayInset) * 2;
            var overlaySizeSpec = MeasureSpec.MakeMeasureSpec (overlaySide, MeasureSpecMode.Exactly);
            loadingOverlayView.Measure (overlaySizeSpec, overlaySizeSpec);
            emptyOverlayView.Measure (overlaySizeSpec, overlaySizeSpec);
            statsOverlayView.Measure (overlaySizeSpec, overlaySizeSpec);

            SetMeasuredDimension (width, height);
        }

        protected override void OnLayout (bool changed, int l, int t, int r, int b)
        {
            var dm = Resources.DisplayMetrics;
            var width = r - l;
            var height = b - t;

            // Layout slices as rectangles in the center of this view
            var sliceSide = Math.Min (width, height);
            var sliceLeft = (width - sliceSide) / 2;
            var sliceTop = (height - sliceSide) / 2;

            backgroundView.Layout (sliceLeft, sliceTop, sliceLeft + sliceSide, sliceTop + sliceSide);

            // Position overlays
            var overlaySide = (int) (sliceSide / ActiveSliceScale) - overlayInset * 2;
            var overlayLeft = (width - overlaySide) / 2;
            var overlayTop = (height - overlaySide) / 2;
            loadingOverlayView.Layout (overlayLeft, overlayTop, overlayLeft + overlaySide, overlayTop + overlaySide);
            emptyOverlayView.Layout (overlayLeft, overlayTop, overlayLeft + overlaySide, overlayTop + overlaySide);
            statsOverlayView.Layout (overlayLeft, overlayTop, overlayLeft + overlaySide, overlayTop + overlaySide);

            foreach (var slice in slices) {
                slice.Layout (sliceLeft, sliceTop, sliceLeft + sliceSide, sliceTop + sliceSide);
            }
        }

        public void Reset (SummaryReportView data)
        {
            this.data = data;

            // Cancel old animation
            if (currentRevealAnimation != null) {
                currentRevealAnimation.Cancel ();
                currentRevealAnimation = null;
            }
            if (currentSelectAnimation != null) {
                currentSelectAnimation.Cancel ();
                currentSelectAnimation = null;
            }

            var totalSlices = data == null || data.CollapsedProjects == null ? 0 : data.CollapsedProjects.Count;

            SetActiveSlice (-1, updateStats: false);
            backgroundView.Visibility = ViewStates.Visible;
            backgroundView.Radius = defaultRadius;

            ResetSlices (totalSlices);
            if (totalSlices > 0) {
                var totalTime = data.CollapsedProjects.Sum (x => x.TotalTime);
                var startAngle = 0f;

                for (var i = 0; i < totalSlices; i++) {
                    var slice = slices [i];
                    var project = data.CollapsedProjects [i];
                    var percentOfAll = (float)project.TotalTime / totalTime;

                    slice.Visibility = ViewStates.Gone;
                    slice.Radius = defaultRadius;
                    if (project.Color == ProjectModel.GroupedProjectColorIndex) {
                        slice.Color = Color.ParseColor (ProjectModel.GroupedProjectColor);
                    } else {
                        slice.Color = Color.ParseColor (ProjectModel.HexColors [project.Color % ProjectModel.HexColors.Length]);
                    }
                    slice.StartAngle = startAngle;
                    startAngle += percentOfAll * 360;
                }
            }

            // Detect state
            var isLoading = data == null || data.IsLoading;
            var isEmpty = !isLoading && totalSlices == 0;

            if (isLoading) {
                // Loading state
                loadingOverlayView.Visibility = ViewStates.Visible;
                loadingOverlayView.Alpha = 1f;

                emptyOverlayView.Visibility = ViewStates.Gone;
                statsOverlayView.Visibility = ViewStates.Gone;
            } else if (isEmpty) {
                // Error state
                loadingOverlayView.Visibility = ViewStates.Visible;
                loadingOverlayView.Alpha = 1f;

                emptyOverlayView.Visibility = ViewStates.Visible;
                emptyOverlayView.Alpha = 0f;

                statsOverlayView.Visibility = ViewStates.Gone;

                // Animate overlay in
                var scene = new AnimatorSet ();

                var fadeIn = ObjectAnimator.OfFloat (emptyOverlayView, "alpha", 0f, 1f).SetDuration (500);
                var fadeOut = ObjectAnimator.OfFloat (loadingOverlayView, "alpha", 1f, 0f).SetDuration (500);
                fadeOut.AnimationEnd += delegate {
                    loadingOverlayView.Visibility = ViewStates.Gone;
                };

                scene.Play (fadeOut);
                scene.Play (fadeIn).After (3 * fadeOut.Duration / 4);

                currentRevealAnimation = scene;
                scene.Start();
            } else {
                // Normal state
                var scene = new AnimatorSet ();

                // Fade loading message out
                statsOverlayView.Visibility = ViewStates.Visible;
                statsOverlayView.Alpha = 0f;

                var fadeOverlayOut = ObjectAnimator.OfFloat (loadingOverlayView, "alpha", 1f, 0f).SetDuration (500);
                fadeOverlayOut.AnimationEnd += delegate {
                    loadingOverlayView.Visibility = ViewStates.Gone;
                };
                scene.Play (fadeOverlayOut);

                var fadeOverlayIn = ObjectAnimator.OfFloat (statsOverlayView, "alpha", 0f, 1f).SetDuration (500);
                scene.Play (fadeOverlayIn).After (3 * fadeOverlayOut.Duration / 4);

                var donutReveal = ValueAnimator.OfFloat (0, 360);
                donutReveal.SetDuration (750);
                donutReveal.Update += (sender, e) => ShowSlices ((float)e.Animation.AnimatedValue);
                scene.Play (donutReveal).After (fadeOverlayOut.Duration / 2);

                currentRevealAnimation = scene;
                scene.Start();
            }

            UpdateStats ();
            RequestLayout ();
        }

        private void ShowSlices (float endAngle)
        {
            for (var i = 0; i < slices.Count; i++) {
                var slice = slices [i];
                var sliceEndAngle = i + 1 < slices.Count ? slices [i + 1].StartAngle : 360;

                if (slice.StartAngle <= endAngle) {
                    slice.EndAngle = Math.Min (sliceEndAngle, endAngle);
                    slice.Visibility = ViewStates.Visible;
                } else {
                    slice.Visibility = ViewStates.Gone;
                }
            }

            backgroundView.Visibility = endAngle >= 360 ? ViewStates.Gone : ViewStates.Visible;
        }

        private void ResetSlices (int neededSlices)
        {
            var existingSlices = slices.Count;
            var totalRows = (int)Math.Max (existingSlices, neededSlices);
            var expandSlices = neededSlices > existingSlices;
            var contractSlices = existingSlices > neededSlices;

            if (expandSlices) {
                for (var i = existingSlices; i < totalRows; i++) {
                    // Create new row
                    var slice = new SliceView (Context);
                    slice.Click += OnSliceClicked;
                    slices.Add (slice);

                    // Add new slice views
                    AddView (slice, 1 + i);
                }
            } else if (contractSlices) {
                // Remove unused rows and views
                var sliceCount = existingSlices - neededSlices;
                for (var i = neededSlices; i < existingSlices; i++) {
                    var slice = slices [0];
                    slice.Click -= OnSliceClicked;
                }
                slices.RemoveRange (neededSlices, sliceCount);

                var startIndex = 1 + neededSlices;
                var viewCount = sliceCount;
                RemoveViews (startIndex, viewCount);
            }
        }

        private void OnSliceClicked (object sender, EventArgs e)
        {
            var pos = slices.IndexOf ((SliceView)sender);
            ActiveSlice = pos != ActiveSlice ? pos : -1;
        }

        private void UpdateStats()
        {
            if (data == null) {
                return;
            }

            if (ActiveSlice >= 0 && ActiveSlice < data.Projects.Count) {
                var proj = data.Projects [ActiveSlice];
                statsTimeTextView.Text = FormatMilliseconds (proj.TotalTime);
                statsMoneyTextView.Text = String.Join (", ", proj.Currencies.Select (c => String.Format ("{0} {1}", c.Amount, c.Currency)));
            } else {
                statsTimeTextView.Text = FormatMilliseconds (data.Projects.Sum (x => x.TotalTime));
                statsMoneyTextView.Text = String.Join (", ", data.TotalCost);
            }
        }

        public event EventHandler ActiveSliceChanged;

        public int ActiveSlice
        {
            get { return activeSlice; }
            set { SetActiveSlice (value, animate: true); }
        }

        private void SetActiveSlice (int value, bool animate = false, bool updateStats = true)
        {
            if (slices.Count == 1 || value >= slices.Count) {
                value = -1;
            }

            if (value == activeSlice) {
                return;
            }

            activeSlice = value;
            Clickable = activeSlice >= 0;

            if (updateStats) {
                UpdateStats ();
            }

            if (animate) {
                // Finish currently running animations
                if (currentSelectAnimation != null) {
                    currentSelectAnimation.Cancel ();
                }

                // Animate changes
                var scene = new AnimatorSet ();
                for (var i = 0; i < slices.Count; i++) {
                    var slice = slices [i];

                    if (i == activeSlice) {
                        // Slice activating animations
                        if (slice.Alpha < 1) {
                            var fadeIn = ObjectAnimator.OfFloat (slice, "alpha", slice.Alpha, 1).SetDuration (500);
                            scene.Play (fadeIn);
                        }
                        if (slice.ScaleX != ActiveSliceScale) {
                            var scaleXUp = ObjectAnimator.OfFloat (slice, "scaleX", slice.ScaleX, ActiveSliceScale).SetDuration (500);
                            scene.Play (scaleXUp);
                        }
                        if (slice.ScaleY != ActiveSliceScale) {
                            var scaleYUp = ObjectAnimator.OfFloat (slice, "scaleY", slice.ScaleY, ActiveSliceScale).SetDuration (500);
                            scene.Play (scaleYUp);
                        }
                    } else if (activeSlice >= 0) {
                        // Slice deactivating animations
                        if (slice.Alpha > 0.5f) {
                            var fadeOut = ObjectAnimator.OfFloat (slice, "alpha", slice.Alpha, 0.5f).SetDuration (300);
                            scene.Play (fadeOut);
                        }
                        if (slice.ScaleX != NonActiveSliceScale) {
                            var scaleXDown = ObjectAnimator.OfFloat (slice, "scaleX", slice.ScaleX, NonActiveSliceScale).SetDuration (300);
                            scene.Play (scaleXDown);
                        }
                        if (slice.ScaleY != NonActiveSliceScale) {
                            var scaleYDown = ObjectAnimator.OfFloat (slice, "scaleY", slice.ScaleY, NonActiveSliceScale).SetDuration (300);
                            scene.Play (scaleYDown);
                        }
                    } else {
                        // No slice selected animations
                        if (slice.Alpha < 1) {
                            var fadeIn = ObjectAnimator.OfFloat (slice, "alpha", slice.Alpha, 1).SetDuration (300);
                            scene.Play (fadeIn);
                        }
                        if (slice.ScaleX != 1) {
                            var scaleXDown = ObjectAnimator.OfFloat (slice, "scaleX", slice.ScaleX, 1f).SetDuration (300);
                            scene.Play (scaleXDown);
                        }
                        if (slice.ScaleY != 1) {
                            var scaleYDown = ObjectAnimator.OfFloat (slice, "scaleY", slice.ScaleY, 1f).SetDuration (300);
                            scene.Play (scaleYDown);
                        }
                    }
                }

                currentSelectAnimation = scene;
                scene.Start ();
            }

            // Notify listeners
            if (ActiveSliceChanged != null) {
                ActiveSliceChanged (this, EventArgs.Empty);
            }
        }

        private static string FormatMilliseconds (long ms)
        {
            var timeSpan = TimeSpan.FromMilliseconds (ms);
            return String.Format ("{0}:{1:mm\\:ss}", Math.Floor (timeSpan.TotalHours).ToString ("00"), timeSpan);
        }

        private class SliceView : View
        {
            private readonly Paint slicePaint;
            private readonly Path slicePath = new Path ();
            private readonly RectF rect = new RectF();
            private readonly int thickness;
            private float startAngle;
            private float endAngle;
            private float radius;

            public SliceView (Context context) : base (context)
            {
                var dm = context.Resources.DisplayMetrics;

                thickness = (int)TypedValue.ApplyDimension (ComplexUnitType.Dip, 45, dm);

                slicePaint = new Paint() {
                    AntiAlias = true,
                };

                Clickable = true;
            }

            public override bool OnTouchEvent (MotionEvent e)
            {
                // Verify that the touch is in the drawn slice
                var x = e.GetX() - Width / 2;
                var y = e.GetY() - Height / 2;

                // Convert to polar coordinates and verify location of the touch event
                var distance = Math.Sqrt (Math.Pow (x, 2) + Math.Pow (y, 2));
                if (distance == 0 || distance < Radius - thickness || distance > Radius) {
                    return false;
                }
                var angle = ((Math.Atan2 (y, x) + 5 * Math.PI / 2) % (2 * Math.PI)) * 180 / Math.PI;
                if (angle < StartAngle || angle > EndAngle) {
                    return false;
                }

                return base.OnTouchEvent (e);
            }

            protected override void OnDraw (Canvas canvas)
            {
                base.OnDraw (canvas);

                var width = canvas.Width;
                var height = canvas.Height;

                // Center the canvas
                canvas.Translate (width / 2f, height / 2f);

                if (slicePath.IsEmpty) {
                    if (startAngle == 0 && endAngle == 360) {
                        slicePath.AddCircle (0, 0, radius - thickness, Path.Direction.Cw);
                        slicePath.AddCircle (0, 0, radius, Path.Direction.Ccw);
                        slicePath.Close ();
                    } else {
                        // Inner arc
                        rect.Set (-radius + thickness, -radius + thickness, radius - thickness, radius - thickness);
                        slicePath.ArcTo (rect, -90 + startAngle, endAngle - startAngle);

                        // Outer arc
                        rect.Set (-radius, -radius, radius, radius);
                        slicePath.ArcTo (rect, -90 + endAngle, startAngle - endAngle);

                        slicePath.Close ();
                    }
                }

                canvas.DrawPath (slicePath, slicePaint);
            }

            public float StartAngle
            {
                get { return startAngle; }
                set {
                    if (value == startAngle) {
                        return;
                    }

                    startAngle = value;
                    slicePath.Reset ();
                    Invalidate ();
                }
            }

            public float EndAngle
            {
                get { return endAngle; }
                set {
                    if (value == endAngle) {
                        return;
                    }

                    endAngle = value;
                    slicePath.Reset ();
                    Invalidate ();
                }
            }

            public float Radius
            {
                get { return radius; }
                set {
                    if (value == radius) {
                        return;
                    }

                    radius = value;
                    slicePath.Reset ();
                    Invalidate ();
                }
            }

            public Color Color
            {
                get { return slicePaint.Color; }
                set {
                    if (value == slicePaint.Color) {
                        return;
                    }

                    slicePaint.Color = value;
                    Invalidate ();
                }
            }
        }
    }
}
