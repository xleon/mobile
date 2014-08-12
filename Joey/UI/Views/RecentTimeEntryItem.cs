using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
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

        public RecentTimeEntryItem (Context context, IAttributeSet attrs) : base (context, attrs)
        {
            view = LayoutInflater.FromContext (context).Inflate (Resource.Layout.RecentTimeEntryItem, this, true);
            Initialize ();
        }

        public RecentTimeEntryItem (Context context, IAttributeSet attrs, int defStyle) : base (context, attrs, defStyle)
        {
            view = LayoutInflater.FromContext (context).Inflate (Resource.Layout.RecentTimeEntryItem, this, true);
            Initialize ();
        }

        private void Initialize () 
        {
            view.SetBackgroundDrawable (Resources.GetDrawable(Resource.Drawable.MainListButton));
            colorView = FindViewById (Resource.Id.ColorView);
            projectTextView = view.FindViewById<TextView> (Resource.Id.ProjectTextView);
            clientTextView = view.FindViewById<TextView> (Resource.Id.ClientTextView);
            taskTextView = view.FindViewById<TextView> (Resource.Id.TaskTextView);
            descriptionTextView = view.FindViewById<TextView> (Resource.Id.DescriptionTextView);
        }

        protected override void OnMeasure (int widthMeasureSpec, int heightMeasureSpec)
        {
            int widthUsed = 0;
            int heightUsed = 0;
            int widthSize = MeasureSpec.GetSize(widthMeasureSpec);

            MeasureChildWithMargins (colorView, widthMeasureSpec, widthUsed, heightMeasureSpec, heightUsed);
            heightUsed += getMeasuredHeightWithMargins (colorView); // should be 64dp in xml (Make it the height defining element of the layout)

            MeasureChildWithMargins (projectTextView, widthMeasureSpec, widthUsed, heightMeasureSpec, heightUsed);
            MeasureChildWithMargins (descriptionTextView, widthMeasureSpec, widthUsed, heightMeasureSpec, heightUsed);
            MeasureChildWithMargins (clientTextView, widthMeasureSpec, widthUsed, heightMeasureSpec, heightUsed);
            MeasureChildWithMargins (taskTextView, widthMeasureSpec, widthUsed, heightMeasureSpec, heightUsed);

            int heightSize = heightUsed + PaddingTop + PaddingBottom;
            SetMeasuredDimension (widthSize, heightSize);
        }

        protected override void OnLayout (bool changed, int l, int t, int r, int b)
        {
            int paddingLeft = PaddingLeft;
            int paddingTop = PaddingTop;
            int firstRow = 17;
            int secondRow = 68;
            int currentTop = paddingTop;

            layoutView (colorView, paddingLeft, currentTop, colorView.MeasuredWidth, colorView.MeasuredHeight);
            int contentLeft = getWidthWithMargins (colorView) + paddingLeft + 20;

            layoutView (projectTextView, contentLeft, firstRow, projectTextView.MeasuredWidth, projectTextView.MeasuredHeight);
            int widthUsedFirstRow = getWidthWithMargins (projectTextView)  + contentLeft;
            layoutView (clientTextView, widthUsedFirstRow, firstRow + 8, clientTextView.MeasuredWidth, clientTextView.MeasuredHeight);
            if (taskTextView.Text.Length > 0) {
                layoutView (taskTextView, contentLeft, secondRow, taskTextView.MeasuredWidth, taskTextView.MeasuredHeight);
                int widthUsedSecondRow = getWidthWithMargins (taskTextView) + contentLeft;
                layoutView (descriptionTextView, widthUsedSecondRow, secondRow, descriptionTextView.MeasuredWidth, descriptionTextView.MeasuredHeight);
            } else {
                layoutView (descriptionTextView, contentLeft, secondRow, descriptionTextView.MeasuredWidth, descriptionTextView.MeasuredHeight);
            }

        }

        private void layoutView(View view, int left, int top, int width, int height)
        {
            var margins = (MarginLayoutParams) view.LayoutParameters;
            int leftWithMargins = left + margins.LeftMargin;
            int topWithMargins = top + margins.TopMargin;

            view.Layout(leftWithMargins, topWithMargins,
                leftWithMargins + width, topWithMargins + height);
        }

        private int getWidthWithMargins (View child)
        {
            MarginLayoutParams lp = (MarginLayoutParams) child.LayoutParameters;
            return child.Width + lp.LeftMargin + lp.RightMargin;
        }

        private int getHeightWithMargins (View child)
        {
            MarginLayoutParams lp = (MarginLayoutParams) child.LayoutParameters;
            return child.MeasuredHeight + lp.TopMargin + lp.BottomMargin;
        }

        private int getMeasuredWidthWithMargins(View child)
        {
            MarginLayoutParams lp = (MarginLayoutParams) child.LayoutParameters;
            return child.MeasuredWidth + lp.LeftMargin + lp.RightMargin;
        }

        private int getMeasuredHeightWithMargins(View child)
        {
            MarginLayoutParams lp = (MarginLayoutParams) child.LayoutParameters;
            return child.MeasuredHeight + lp.TopMargin + lp.BottomMargin;
        }

        public override LayoutParams GenerateLayoutParams(IAttributeSet attrs) {
            return new MarginLayoutParams(Context, attrs);
        }

        protected override LayoutParams GenerateDefaultLayoutParams() {
            return new MarginLayoutParams(LayoutParams.WrapContent, LayoutParams.WrapContent);
        }

    }
}

