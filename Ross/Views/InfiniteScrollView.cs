using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using MonoTouch.UIKit;

namespace Toggl.Ross.Views
{
    public class InfiniteScrollView : UIScrollView
    {
        public event EventHandler OnChangeReport;

        private int _pageIndex;

        public int PageIndex
        {
            get {
                return _pageIndex + (Convert.ToInt32 ( ContentOffset.X / PageWidth) - tmpOffset);
            }
        }

        public ReportView VisibleReportView
        {
            get {
                var pos = ConvertPointToView ( ContentOffset, _containerView).X;
                return visibleViews.Count > 0 ? visibleViews.First (v => Math.Abs (pos - v.Frame.X) <= PageWidth / 2) : null;
            }
        }

        public InfiniteScrollView ( RectangleF frame ) : base ( frame)
        {
            ContentSize = new SizeF ( PageWidth * 20, Frame.Height);
            visibleViews = new List<ReportView> ();
            cachedViews = new List<ReportView> ();
            _containerView = new UIView ();
            _containerView.Frame = new RectangleF (0, 0, ContentSize.Width, ContentSize.Height);
            AddSubview (_containerView);
            ShowsHorizontalScrollIndicator = false;
            PagingEnabled = true;
            Delegate = new InfiniteScrollDelegate ();
        }

        List<ReportView> visibleViews;
        List<ReportView> cachedViews;
        UIView _containerView;

        public const float PageWidth = 320;
        private int tmpOffset;
        private int prevPageIndex = 5000;

        private void RecenterIfNeeded()
        {
            PointF currentOffset = ContentOffset;
            float contentWidth = ContentSize.Width;
            float centerOffsetX = (contentWidth - Bounds.Width) / 2.0f;
            float distanceFromCenter = Math.Abs ( currentOffset.X - centerOffsetX);

            if (distanceFromCenter > (contentWidth / 4.0f) && (distanceFromCenter - PageWidth/2) % PageWidth == 0) {
                _pageIndex += Convert.ToInt32 ( ContentOffset.X / PageWidth) - tmpOffset;
                ContentOffset = new PointF (centerOffsetX - PageWidth/2, currentOffset.Y);
                foreach (var item in visibleViews) {
                    PointF center = _containerView.ConvertPointToView (item.Center, this);
                    center.X += centerOffsetX - currentOffset.X - PageWidth/2;
                    item.Center = ConvertPointToView (center, _containerView);
                }
            }
        }

        public override bool GestureRecognizerShouldBegin (UIGestureRecognizer gestureRecognizer)
        {
            if (!VisibleReportView.Dragging) {
                VisibleReportView.ScrollEnabled = false;
                foreach (var item in visibleViews) {
                    item.Position = VisibleReportView.Position;
                }
            }
            return !VisibleReportView.Dragging;
        }

        public override void LayoutSubviews ()
        {
            base.LayoutSubviews ();
            RecenterIfNeeded ();

            // tile content in visible bounds
            RectangleF visibleBounds = ConvertRectToView (Bounds, _containerView);
            float minimumVisibleX = CGRectGetMinX ( visibleBounds);
            float maximumVisibleX = CGRectGetMaxX ( visibleBounds);
            TileViews ( minimumVisibleX, maximumVisibleX);

            if (prevPageIndex != PageIndex) {
                prevPageIndex = PageIndex;
                if (OnChangeReport != null) {
                    OnChangeReport.Invoke (this, new EventArgs ());
                }
            }
        }

        public void SetPageIndex ( int offSet, bool animated)
        {
            var currentCOffset = ContentOffset;
            if (currentCOffset.X % PageWidth == 0) {
                currentCOffset.X += PageWidth * offSet;
                SetContentOffset (currentCOffset, animated);
            }
        }

        public void RefreshVisibleReportView()
        {
            if (Dragging) {
                return;
            }

            var currentView = visibleViews.Find (v => v.Frame.X.CompareTo ( ContentOffset.X) == 0);
            var newReportView = InsertView ();
            var offSetY = ContentSize.Height;
            var frame = currentView.Frame;
            frame.Y += offSetY;
            newReportView.Frame = frame;

            UIView.Animate (0.5, 0, UIViewAnimationOptions.CurveEaseOut,
            () => {
                newReportView.Center = new PointF ( currentView.Center.X, currentView.Center.Y );
                currentView.Center = new PointF ( currentView.Center.X, currentView.Center.Y - offSetY );
            },() => {
                var index = visibleViews.IndexOf (currentView);
                visibleViews[index] = newReportView;
                currentView.RemoveFromSuperview ();
                if (currentView.IsClean) {
                    currentView.StopReloadData ();
                }
                if (OnChangeReport != null) {
                    OnChangeReport.Invoke (this, new EventArgs ());
                }
            });

        }

        private ReportView InsertView()
        {
            ReportView view;
            if (cachedViews.Count == 0) {
                view = new ReportView (new RectangleF (0, 0, PageWidth, Frame.Height));
            } else {
                view = cachedViews[0];
                cachedViews.RemoveAt (0);
            }
            view.Frame = new RectangleF (0, 0, PageWidth, Frame.Height);
            if ( visibleViews.Count > 0) {
                view.Position = VisibleReportView.Position;
            }
            _containerView.Add (view);
            return view;
        }

        private float PlaceNewViewOnRight ( float rightEdge)
        {
            ReportView view = InsertView ();
            visibleViews.Add (view); // add rightmost label at the end of the array

            RectangleF labelFrame = view.Frame;
            labelFrame.X = rightEdge;
            labelFrame.Y = _containerView.Bounds.Height - labelFrame.Height;
            view.Frame = labelFrame;

            return CGRectGetMaxX ( labelFrame);
        }

        private float PlaceNewViewOnLeft ( float leftEdge)
        {
            ReportView view = InsertView ();
            visibleViews.Insert ( 0, view); // add leftmost label at the beginning of the array

            RectangleF labelFrame = view.Frame;
            labelFrame.X = leftEdge - labelFrame.Width;
            labelFrame.Y = _containerView.Bounds.Height - labelFrame.Height;
            view.Frame = labelFrame;

            return CGRectGetMinX ( labelFrame);
        }

        private void TileViews ( float minX, float maxX)
        {
            // the upcoming tiling logic depends on there already being at least one label in the visibleLabels array, so
            // to kick off the tiling we need to make sure there's at least one label
            if (visibleViews.Count == 0) {
                tmpOffset = Convert.ToInt32 (ContentOffset.X / PageWidth);
                PlaceNewViewOnRight (minX);
            }

            // add views that are missing on right side
            ReportView lastView = visibleViews [visibleViews.Count - 1];
            float rightEdge = CGRectGetMaxX ( lastView.Frame);
            while ( rightEdge < maxX) {
                rightEdge = PlaceNewViewOnRight (rightEdge);
            }

            // add views that are missing on left side
            ReportView firstView = visibleViews [0];
            float leftEdge = CGRectGetMinX ( firstView.Frame);
            while ( leftEdge > minX) {
                leftEdge = PlaceNewViewOnLeft (leftEdge);
            }

            // remove views that have fallen off right edge
            lastView = visibleViews.Last();
            while (lastView.Frame.X > maxX) {
                lastView.RemoveFromSuperview ();
                if (lastView.IsClean) {
                    //cachedViews.Add (lastView);
                    lastView.StopReloadData ();
                }
                visibleViews.Remove (lastView);
                lastView = visibleViews.Last();
            }

            // remove views that have fallen off left edge
            firstView = visibleViews.First();
            while ( CGRectGetMaxX ( firstView.Frame) < minX) {
                firstView.RemoveFromSuperview ();
                if (firstView.IsClean) {
                    //cachedViews.Add (firstView);
                    firstView.StopReloadData ();
                }
                visibleViews.Remove (firstView);
                firstView = visibleViews.First();
            }
        }

        private float CGRectGetMinX ( RectangleF rect)
        {
            return rect.X;
        }

        private float CGRectGetMaxX ( RectangleF rect)
        {
            return rect.X + rect.Width;
        }

    }

    internal class InfiniteScrollDelegate : UIScrollViewDelegate
    {
        public override void DecelerationEnded (UIScrollView scrollView)
        {
            var infiniteScroll = (InfiniteScrollView)scrollView;
            infiniteScroll.VisibleReportView.ScrollEnabled = true;
        }

    }
}