using System;
using System.Collections.Generic;
using System.Linq;
using Android.Animation;
using Android.Content;
using Android.Graphics;
using Android.Graphics.Drawables;
using Android.Text;
using Android.Util;
using Android.Views;
using Android.Widget;
using Toggl.Phoebe.Data;
using Toggl.Phoebe.Data.Reports;

namespace Toggl.Joey.UI.Views
{
    public class BarChart : ViewGroup
    {
        private static Color LightGrayColor = Color.ParseColor ("#CCCCCC");
        private static Color DarkGrayColor = Color.ParseColor ("#666666");
        private static Color DarkBlueColor = Color.ParseColor ("#00AEFF");
        private static Color LightBlueColor = Color.ParseColor ("#80D6FF");

        private readonly List<Row> rows = new List<Row> (31);
        private int leftMargin;
        private int leftPadding;
        private int topPadding;
        private int bottomPadding;
        private int rightPadding;
        private int yAxisSpacing;
        private int barZeroSize;
        private int barLabelSpacing;
        private BackgroundView backgroundView;
        private View loadingOverlayView;
        private View emptyOverlayView;
        private Animator currentAnimation;

        public BarChart (Context context, IAttributeSet attrs) : base (context, attrs)
        {
            Initialize (context);
        }

        public BarChart (Context context, IAttributeSet attrs, int defStyle) : base (context, attrs, defStyle)
        {
            Initialize (context);
        }

        private void Initialize (Context ctx)
        {
            var dm = ctx.Resources.DisplayMetrics;
            var inflater = LayoutInflater.FromContext (ctx);

            leftMargin = (int)TypedValue.ApplyDimension (ComplexUnitType.Dip, 45, dm);
            leftPadding = (int)TypedValue.ApplyDimension (ComplexUnitType.Dip, 14, dm);
            topPadding = (int)TypedValue.ApplyDimension (ComplexUnitType.Dip, 10, dm);
            bottomPadding = (int)TypedValue.ApplyDimension (ComplexUnitType.Dip, 23, dm);
            rightPadding = (int)TypedValue.ApplyDimension (ComplexUnitType.Dip, 45, dm);
            yAxisSpacing = (int)TypedValue.ApplyDimension (ComplexUnitType.Dip, 5, dm);
            barZeroSize = (int)TypedValue.ApplyDimension (ComplexUnitType.Dip, 3, dm);
            barLabelSpacing = (int)TypedValue.ApplyDimension (ComplexUnitType.Dip, 5, dm);

            backgroundView = new BackgroundView (ctx);
            AddView (backgroundView);

            loadingOverlayView = inflater.Inflate (Resource.Layout.BarChartLoading, this, false);
            AddView (loadingOverlayView);

            emptyOverlayView = inflater.Inflate (Resource.Layout.BarChartEmpty, this, false);
            emptyOverlayView.Visibility = ViewStates.Gone;
            AddView (emptyOverlayView);
        }

        private void ResetRows (int neededRows)
        {
            var existingRows = rows.Count;
            var totalRows = (int)Math.Max (existingRows, neededRows);
            var expandRows = neededRows > existingRows;
            var contractRows = existingRows > neededRows;

            if (expandRows) {
                for (var i = existingRows; i < totalRows; i++) {
                    // Create new row
                    var row = new Row (Context);
                    rows.Add (row);

                    // Add new row views
                    var startIndex = 1 + 3 * i;
                    AddView (row.YAxisTextView, startIndex++);
                    AddView (row.BarView, startIndex++);
                    AddView (row.ValueTextView, startIndex++);
                }
            } else if (contractRows) {
                // Remove unused rows and views
                var rowCount = existingRows - neededRows;
                rows.RemoveRange (neededRows, rowCount);

                var startIndex = 1 + 3 * neededRows;
                var viewCount = 3 * rowCount;
                RemoveViews (startIndex, viewCount);
            }

            foreach (var row in rows) {
                row.Reset ();
            }
        }

        public void Reset (SummaryReportView data)
        {
            // Cancel old animation
            if (currentAnimation != null) {
                currentAnimation.Cancel ();
                currentAnimation = null;
            }

            var totalRows = 0;
            var hasTime = false;

            if (data == null) {
                backgroundView.XAxisLabels = null;
                ResetRows (totalRows);
            } else {
                var showEveryYLabel = 1;
                var showValueLabels = true;

                backgroundView.XAxisLabels = data.ChartTimeLabels.ToArray ();

                totalRows = (int)Math.Min (data.Activity.Count, data.ChartRowLabels.Count);
                ResetRows (totalRows);

                if (totalRows > 25) {
                    showEveryYLabel = 3;
                    showValueLabels = false;
                }

                for (var i = 0; i < totalRows; i++) {
                    var activity = data.Activity [i];
                    var yLabel = data.ChartRowLabels [i];

                    if (activity.TotalTime > 0) {
                        hasTime = true;
                    }

                    var barWidth = (float)activity.TotalTime / (float) (data.MaxTotal * 3600);
                    var showYAxis = i % showEveryYLabel == 0;

                    // Bind the data to row
                    var row = rows [i];
                    row.RelativeWidth = barWidth;
                    row.BarView.BillableTime = activity.BillableTime;
                    row.BarView.TotalTime = activity.TotalTime;
                    row.ValueTextView.Text = FormatTime (activity.TotalTime);
                    row.ValueTextView.Visibility = showValueLabels ? ViewStates.Visible : ViewStates.Gone;
                    row.YAxisTextView.Text = yLabel;
                    row.YAxisTextView.Visibility = showYAxis ? ViewStates.Visible : ViewStates.Gone;
                }
            }

            // Detect state
            var isLoading = totalRows == 0 || (data != null && data.IsLoading);
            var isEmpty = !isLoading && !hasTime;

            if (isLoading) {
                // Loading state
                loadingOverlayView.Visibility = ViewStates.Visible;
                loadingOverlayView.Alpha = 1f;

                emptyOverlayView.Visibility = ViewStates.Gone;
            } else if (isEmpty) {
                // Error state
                loadingOverlayView.Visibility = ViewStates.Visible;
                loadingOverlayView.Alpha = 1f;

                emptyOverlayView.Visibility = ViewStates.Visible;
                emptyOverlayView.Alpha = 0f;

                // Animate overlay in
                var scene = new AnimatorSet ();

                var fadeIn = ObjectAnimator.OfFloat (emptyOverlayView, "alpha", 0f, 1f).SetDuration (500);
                var fadeOut = ObjectAnimator.OfFloat (loadingOverlayView, "alpha", 1f, 0f).SetDuration (500);
                fadeOut.AnimationEnd += delegate {
                    loadingOverlayView.Visibility = ViewStates.Gone;
                };

                scene.Play (fadeOut);
                scene.Play (fadeIn).After (3 * fadeOut.Duration / 4);

                currentAnimation = scene;
                scene.Start();
            } else {
                // Normal state
                var scene = new AnimatorSet ();

                // Fade loading message out
                var fadeOverlayOut = ObjectAnimator.OfFloat (loadingOverlayView, "alpha", 1f, 0f).SetDuration (500);
                fadeOverlayOut.AnimationEnd += delegate {
                    loadingOverlayView.Visibility = ViewStates.Gone;
                };

                scene.Play (fadeOverlayOut);

                foreach (var row in rows) {
                    var axisFadeIn = ObjectAnimator.OfFloat (row.YAxisTextView, "alpha", 0f, 1f).SetDuration (500);
                    var barScaleUp = ObjectAnimator.OfFloat (row.BarView, "scaleX", 0f, 1f).SetDuration (750);
                    var valueFadeIn = ObjectAnimator.OfFloat (row.ValueTextView, "alpha", 0f, 1f).SetDuration (400);

                    scene.Play (axisFadeIn);
                    scene.Play (barScaleUp).After (axisFadeIn.Duration / 2);
                    scene.Play (valueFadeIn).After (barScaleUp);
                }

                currentAnimation = scene;
                scene.Start();
            }

            RequestLayout ();
        }

        protected override void OnMeasure (int widthMeasureSpec, int heightMeasureSpec)
        {
            var dm = Resources.DisplayMetrics;

            // Let various labels measure themselves
            var unspecifiedMeasureSpec = MeasureSpec.MakeMeasureSpec (0, MeasureSpecMode.Unspecified);
            foreach (var row in rows) {
                row.YAxisTextView.Measure (unspecifiedMeasureSpec, unspecifiedMeasureSpec);
                row.ValueTextView.Measure (unspecifiedMeasureSpec, unspecifiedMeasureSpec);
            }

            var width = (int)Math.Max (
                            TypedValue.ApplyDimension (ComplexUnitType.Dip, 300, dm),
                            MeasureSpec.GetSize (widthMeasureSpec)
                        );
            var height = (int)Math.Max (
                             TypedValue.ApplyDimension (ComplexUnitType.Dip, 250, dm),
                             MeasureSpec.GetSize (heightMeasureSpec)
                         );

            // Measure overlays
            var overlayWidthSpec = MeasureSpec.MakeMeasureSpec (width - leftMargin, MeasureSpecMode.Exactly);
            var overlayHeightSpec = MeasureSpec.MakeMeasureSpec (height, MeasureSpecMode.Exactly);
            loadingOverlayView.Measure (overlayWidthSpec, overlayHeightSpec);
            emptyOverlayView.Measure (overlayWidthSpec, overlayHeightSpec);

            SetMeasuredDimension (width, height);
        }

        protected override void OnLayout (bool changed, int l, int t, int r, int b)
        {
            var dm = Resources.DisplayMetrics;
            var width = r - l;
            var height = b - t;

            // Assign positions to children
            var backgroundWidth = width - leftMargin;
            backgroundView.Layout (leftMargin, 0, leftMargin + backgroundWidth, height);

            // Position overlays
            loadingOverlayView.Layout (leftMargin, 0, leftMargin + backgroundWidth, height);
            emptyOverlayView.Layout (leftMargin, 0, leftMargin + backgroundWidth, height);

            if (rows.Count > 0) {
                var rowHeight = (height - topPadding - bottomPadding) / rows.Count;
                var rowMargin = (int)Math.Max (
                                    TypedValue.ApplyDimension (ComplexUnitType.Dip, 1f, dm), // Minimum
                                    Math.Min (
                                        rowHeight * 0.05f,
                                        TypedValue.ApplyDimension (ComplexUnitType.Dip, 5f, dm) // Maximum
                                    )
                                );
                rowHeight -= rowMargin * 2;
                var effBgWidth = backgroundWidth - barZeroSize - rightPadding;

                // Determine Y-axis left margin (by respecting yAxisSpacing)
                var yAxisLeftMargin = leftPadding;
                var maxYAxisWidth = rows.Max (x => x.YAxisTextView.MeasuredWidth);
                yAxisLeftMargin += (int)Math.Min (0, leftMargin - yAxisLeftMargin - yAxisSpacing - maxYAxisWidth);

                // Layout rows
                for (var i = 0; i < rows.Count; i++) {
                    var row = rows [i];

                    var rowTop = topPadding + rowMargin + (2 * rowMargin + rowHeight) * i;
                    var rowWidth = barZeroSize + (int) (row.RelativeWidth * effBgWidth);
                    row.BarView.Layout (leftMargin, rowTop, leftMargin + rowWidth, rowTop + rowHeight);

                    // Position value label
                    var tv = row.ValueTextView;
                    var valueX = leftMargin + rowWidth + barLabelSpacing;
                    var valueY = rowTop + (rowHeight - tv.MeasuredHeight - (tv.MeasuredHeight - tv.Baseline)) / 2;
                    if (rowHeight < tv.MeasuredHeight) {
                        // If the bar is smaller than text, we baseline algin the text to bar bottom
                        valueY = rowTop + rowHeight - tv.Baseline;
                    }
                    tv.Layout (valueX, valueY, valueX + tv.MeasuredWidth, valueY + tv.MeasuredHeight);

                    // Position y-axis label
                    tv = row.YAxisTextView;
                    var axisX = yAxisLeftMargin;
                    var axisY = rowTop + (rowHeight - tv.MeasuredHeight - (tv.MeasuredHeight - tv.Baseline)) / 2;
                    if (rowHeight < tv.MeasuredHeight) {
                        // If the bar is smaller than text, we baseline algin the text to bar bottom
                        axisY = rowTop + rowHeight - tv.Baseline;
                    }
                    tv.Layout (axisX, axisY, axisX + tv.MeasuredWidth, axisY + tv.MeasuredHeight);
                }
            }
        }

        private static string FormatTime (long seconds)
        {
            if (seconds == 0) {
                return String.Empty;
            }
            var t = TimeSpan.FromSeconds (seconds);
            return String.Format ("{0}:{1:mm}", (int)t.TotalHours, t);
        }

        private class BackgroundView : View
        {
            // Contains the background with X-axis
            private int leftBorderWidth;
            private int topPadding;
            private int bottomPadding;
            private int rightPadding;
            private Paint backgroundPaint;
            private Paint borderPaint;
            private Paint linePaint;
            private Paint xLabelPaint;
            private Rect rect = new Rect();
            private string[] xLabels;
            private string[] defaultXLabels;

            public BackgroundView (Context ctx) : base (ctx)
            {
                var dm = ctx.Resources.DisplayMetrics;

                leftBorderWidth = (int)TypedValue.ApplyDimension (ComplexUnitType.Dip, 3, dm);
                topPadding = (int)TypedValue.ApplyDimension (ComplexUnitType.Dip, 5, dm);
                bottomPadding = (int)TypedValue.ApplyDimension (ComplexUnitType.Dip, 18, dm);
                rightPadding = (int)TypedValue.ApplyDimension (ComplexUnitType.Dip, 45, dm);
                var lineWidth = (int)Math.Max (1, TypedValue.ApplyDimension (ComplexUnitType.Dip, 0.5f, dm));
                var xLabelFontSize = TypedValue.ApplyDimension (ComplexUnitType.Sp, 10, dm);

                backgroundPaint = new Paint() {
                    Color = Color.White,
                };
                borderPaint = new Paint() {
                    Color = LightGrayColor,
                };
                linePaint = new Paint() {
                    Color = borderPaint.Color,
                    StrokeWidth = lineWidth,
                };
                xLabelPaint = new Paint() {
                    Color = linePaint.Color,
                    TextSize = xLabelFontSize,
                    AntiAlias = true,
                };

                var zeroLabel = "0h"; // TODO: Localize
                xLabels = defaultXLabels = new[] { zeroLabel, zeroLabel, zeroLabel, zeroLabel, zeroLabel };
            }

            protected override void OnDraw (Canvas canvas)
            {
                base.OnDraw (canvas);

                var width = canvas.Width;
                var height = canvas.Height;

                // Background
                int backgroundWidth = width - leftBorderWidth;
                rect.Set (leftBorderWidth, 0, width, height);
                canvas.DrawRect (rect, backgroundPaint);

                // Left border
                rect.Set (0, 0, leftBorderWidth, height);
                canvas.DrawRect (rect, borderPaint);

                for (int i = 0; i < xLabels.Length; i++) {
                    // X-axis line
                    var lineRight = (int) ((backgroundWidth - rightPadding) / (float)xLabels.Length * (i + 1));
                    canvas.DrawLine (
                        leftBorderWidth + lineRight,
                        topPadding,
                        leftBorderWidth + lineRight,
                        height - bottomPadding,
                        linePaint
                    );

                    // X-axis label
                    var label = xLabels [i];
                    xLabelPaint.GetTextBounds (label, 0, label.Length, rect);
                    canvas.DrawText (
                        label,
                        leftBorderWidth + lineRight - rect.Width () / 2f,
                        height - bottomPadding / 2f + rect.Height () / 2f,
                        xLabelPaint
                    );
                }
            }

            public string[] XAxisLabels
            {
                get { return xLabels; }
                set {
                    if (value == null) {
                        value = defaultXLabels;
                    }
                    xLabels = value;
                    Invalidate ();
                }
            }
        }

        private class BarView : View
        {
            // A single barchat layer which is animated using scaleX
            private long totalTime;
            private long billableTime;
            private readonly Paint emptyPaint;
            private readonly Paint billablePaint;
            private readonly Paint charityPaint;

            public BarView (Context ctx) : base (ctx)
            {
                emptyPaint = new Paint() {
                    Color = DarkGrayColor,
                };
                billablePaint = new Paint() {
                    Color = DarkBlueColor,
                };
                charityPaint = new Paint() {
                    Color = LightBlueColor,
                };
            }

            protected override void OnDraw (Canvas canvas)
            {
                base.OnDraw (canvas);

                if (totalTime == 0) {
                    canvas.DrawRect (0, 0, canvas.Width, canvas.Height, emptyPaint);
                } else {
                    if (billableTime == 0) {
                        canvas.DrawRect (0, 0, canvas.Width, canvas.Height, charityPaint);
                    } else if (billableTime == totalTime) {
                        canvas.DrawRect (0, 0, canvas.Width, canvas.Height, billablePaint);
                    } else {
                        var billableWidth = canvas.Width * (float)billableTime / totalTime;
                        canvas.DrawRect (0, 0, billableWidth, canvas.Height, billablePaint);
                        canvas.DrawRect (billableWidth, 0, canvas.Width, canvas.Height, charityPaint);
                    }
                }
            }

            public long TotalTime
            {
                get { return totalTime; }
                set {
                    if (totalTime == value) {
                        return;
                    }
                    totalTime = value;
                    Invalidate ();
                }
            }

            public long BillableTime
            {
                get { return billableTime; }
                set {
                    if (billableTime == value) {
                        return;
                    }
                    billableTime = value;
                    Invalidate ();
                }
            }
        }

        private class Row
        {
            public Row (Context ctx)
            {
                YAxisTextView = new TextView (ctx) {
                    TextSize = 10,
                };
                YAxisTextView.SetTextColor (DarkGrayColor);

                BarView = new BarView (ctx) {
                    PivotX = 0f,
                };

                ValueTextView = new TextView (ctx) {
                    TextSize = 10,
                };
                ValueTextView.SetTextColor (DarkBlueColor);
            }

            public void Reset()
            {
                YAxisTextView.Visibility = ViewStates.Visible;
                YAxisTextView.Alpha = 0f;
                BarView.ScaleX = 0f;
                ValueTextView.Visibility = ViewStates.Visible;
                ValueTextView.Alpha = 0f;
            }

            public float RelativeWidth { get; set; }
            public TextView YAxisTextView { get; set; }
            public BarView BarView { get; set; }
            public TextView ValueTextView { get; set; }
        }
    }
}
