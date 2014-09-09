using System;
using Android.Content;
using Android.Util;
using Android.Views;
using Android.Widget;

namespace Toggl.Joey.UI.Views
{
    public class RecentTimeEntryItem : ViewGroup
    {
        private View colorView;
        private TextView projectTextView;
        private TextView clientTextView;
        private TextView taskTextView;
        private TextView descriptionTextView;
        private View view;
        private ImageView fader;

        public RecentTimeEntryItem (Context context, IAttributeSet attrs) : base (context, attrs)
        {
            view = LayoutInflater.FromContext (context).Inflate (Resource.Layout.RecentTimeEntryListItem, this, true);
            Initialize ();
        }

        public RecentTimeEntryItem (Context context, IAttributeSet attrs, int defStyle) : base (context, attrs, defStyle)
        {
            view = LayoutInflater.FromContext (context).Inflate (Resource.Layout.RecentTimeEntryListItem, this, true);
            Initialize ();
        }

        private void Initialize ()
        {
            view.SetBackgroundDrawable (Resources.GetDrawable (Resource.Drawable.MainListButton));
            colorView = FindViewById (Resource.Id.ColorView);
            projectTextView = view.FindViewById<TextView> (Resource.Id.ProjectTextView);
            clientTextView = view.FindViewById<TextView> (Resource.Id.ClientTextView);
            taskTextView = view.FindViewById<TextView> (Resource.Id.TaskTextView);
            descriptionTextView = view.FindViewById<TextView> (Resource.Id.DescriptionTextView);
            fader = view.FindViewById<ImageView> (Resource.Id.FadeOut);
        }

        protected override void OnMeasure (int widthMeasureSpec, int heightMeasureSpec)
        {
            int widthUsed = 0;
            int heightUsed = 0;
            int widthSize = MeasureSpec.GetSize (widthMeasureSpec);

            MeasureChildWithMargins (colorView, widthMeasureSpec, widthUsed, heightMeasureSpec, heightUsed);
            heightUsed += GetMeasuredHeightWithMargins (colorView); // should be 64dp in xml (Make it the height defining element of the layout)

            MeasureChildWithMargins (projectTextView, widthMeasureSpec, widthUsed, heightMeasureSpec, heightUsed);
            MeasureChildWithMargins (descriptionTextView, widthMeasureSpec, widthUsed, heightMeasureSpec, heightUsed);
            MeasureChildWithMargins (clientTextView, widthMeasureSpec, widthUsed, heightMeasureSpec, heightUsed);
            MeasureChildWithMargins (taskTextView, widthMeasureSpec, widthUsed, heightMeasureSpec, heightUsed);
            MeasureChildWithMargins (fader, widthMeasureSpec, widthUsed, heightMeasureSpec, heightUsed);

            int heightSize = heightUsed + PaddingTop + PaddingBottom;
            SetMeasuredDimension (widthSize, heightSize);
        }

        protected override void OnLayout (bool changed, int l, int t, int r, int b)
        {
            int paddingLeft = PaddingLeft;
            int paddingTop = PaddingTop;
            int currentTop = paddingTop;

            LayoutView (colorView, paddingLeft, currentTop, colorView.MeasuredWidth, colorView.MeasuredHeight);
            int contentLeft = GetWidthWithMargins (colorView) + paddingLeft + 20;

            int fadeFrom = r - 15;
            int usableWidth = fadeFrom - contentLeft;

            LayoutView (projectTextView, contentLeft, currentTop, GetFirstElementWidth (usableWidth, projectTextView.MeasuredWidth), projectTextView.MeasuredHeight);
            if (clientTextView.Text != String.Empty) {
                LayoutView (clientTextView, contentLeft + GetFirstElementWidth (usableWidth, projectTextView.MeasuredWidth), currentTop, GetSecondElementWidth (usableWidth, projectTextView.MeasuredWidth, clientTextView.MeasuredWidth), clientTextView.MeasuredHeight);
            }

            if (taskTextView.Text != String.Empty) {
                LayoutView (taskTextView, contentLeft, currentTop, GetFirstElementWidth (usableWidth, taskTextView.MeasuredWidth), taskTextView.MeasuredHeight);
                LayoutView (descriptionTextView, contentLeft + GetFirstElementWidth (usableWidth, taskTextView.MeasuredWidth), currentTop, GetSecondElementWidth (usableWidth, taskTextView.MeasuredWidth, descriptionTextView.MeasuredWidth), descriptionTextView.MeasuredHeight);
            } else {
                LayoutView (descriptionTextView, contentLeft, currentTop, GetFirstElementWidth (usableWidth, descriptionTextView.MeasuredWidth), descriptionTextView.MeasuredHeight);
            }
            LayoutView (fader, fadeFrom - fader.MeasuredWidth, currentTop, fader.MeasuredWidth, fader.MeasuredHeight);

        }

        private int GetFirstElementWidth (int usable, int first)
        {
            return first > usable ? usable : first;
        }

        private int GetSecondElementWidth (int usable, int first, int second)
        {
            int firstActual = GetFirstElementWidth (usable, first);
            if (firstActual == usable) {
                return 0;
            } else if (usable < firstActual + second) {
                return usable - firstActual;
            } else {
                return second;
            }
        }

        private void LayoutView (View view, int left, int top, int width, int height)
        {
            var margins = (MarginLayoutParams)view.LayoutParameters;
            int leftWithMargins = left + margins.LeftMargin;
            int topWithMargins = top + margins.TopMargin;

            view.Layout (leftWithMargins, topWithMargins,
                leftWithMargins + width, topWithMargins + height);
        }

        private int GetWidthWithMargins (View child)
        {
            MarginLayoutParams lp = (MarginLayoutParams)child.LayoutParameters;
            return child.Width + lp.LeftMargin + lp.RightMargin;
        }

        private int GetHeightWithMargins (View child)
        {
            MarginLayoutParams lp = (MarginLayoutParams)child.LayoutParameters;
            return child.MeasuredHeight + lp.TopMargin + lp.BottomMargin;
        }

        private int GetMeasuredWidthWithMargins (View child)
        {
            MarginLayoutParams lp = (MarginLayoutParams)child.LayoutParameters;
            return child.MeasuredWidth + lp.LeftMargin + lp.RightMargin;
        }

        private int GetMeasuredHeightWithMargins (View child)
        {
            MarginLayoutParams lp = (MarginLayoutParams)child.LayoutParameters;
            return child.MeasuredHeight + lp.TopMargin + lp.BottomMargin;
        }

        public override LayoutParams GenerateLayoutParams (IAttributeSet attrs)
        {
            return new MarginLayoutParams (Context, attrs);
        }

        protected override LayoutParams GenerateDefaultLayoutParams ()
        {
            return new MarginLayoutParams (LayoutParams.WrapContent, LayoutParams.WrapContent);
        }
    }
}

