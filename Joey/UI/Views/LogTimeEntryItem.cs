using Android.Content;
using Android.Util;
using Android.Views;
using Android.Widget;
using System;

namespace Toggl.Joey.UI.Views
{
    public class LogTimeEntryItem : ViewGroup
    {
        private View colorView;
        private TextView projectTextView;
        private TextView clientTextView;
        private TextView taskTextView;
        private TextView descriptionTextView;
        private ImageButton continueImageButton;
        private View continueButtonSeparator;
        private TextView durationTextView;
        private ImageView billableIcon;
        private ImageView tagsIcon;
        private ImageView faderFirstRow;
        private ImageView faderSecondRow;

        private View view;

        public LogTimeEntryItem (Context context, IAttributeSet attrs) : base (context, attrs)
        {
            view = LayoutInflater.FromContext (context).Inflate (Resource.Layout.LogTimeEntryItem, this, true);
            Initialize ();
        }

        public LogTimeEntryItem (Context context, IAttributeSet attrs, int defStyle) : base (context, attrs, defStyle)
        {
            view = LayoutInflater.FromContext (context).Inflate (Resource.Layout.LogTimeEntryItem, this, true);
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
            continueImageButton = view.FindViewById<ImageButton> (Resource.Id.ContinueImageButton);
            continueButtonSeparator = view.FindViewById<View> (Resource.Id.ContinueButtonSeparator);
            durationTextView = view.FindViewById<TextView> (Resource.Id.DurationTextView);
            billableIcon = view.FindViewById<ImageView> (Resource.Id.BillableIcon);
            tagsIcon = view.FindViewById<ImageView> (Resource.Id.TagsIcon);
            faderFirstRow = view.FindViewById<ImageView> (Resource.Id.FaderFirstLine);
            faderSecondRow = view.FindViewById<ImageView> (Resource.Id.FaderSecondLine);

        }

        protected override void OnMeasure (int widthMeasureSpec, int heightMeasureSpec)
        {
            int widthUsed = 0;
            int heightUsed = 0;
            int widthSize = MeasureSpec.GetSize (widthMeasureSpec);

            MeasureChildWithMargins (colorView, widthMeasureSpec, widthUsed, heightMeasureSpec, heightUsed);
            heightUsed += getMeasuredHeightWithMargins (colorView);

            MeasureChildWithMargins (projectTextView, widthMeasureSpec, widthUsed, heightMeasureSpec, heightUsed);
            MeasureChildWithMargins (descriptionTextView, widthMeasureSpec, widthUsed, heightMeasureSpec, heightUsed);
            MeasureChildWithMargins (clientTextView, widthMeasureSpec, widthUsed, heightMeasureSpec, heightUsed);
            MeasureChildWithMargins (taskTextView, widthMeasureSpec, widthUsed, heightMeasureSpec, heightUsed);
            MeasureChildWithMargins (continueButtonSeparator, widthMeasureSpec, widthUsed, heightMeasureSpec, heightUsed);
            MeasureChildWithMargins (continueImageButton, widthMeasureSpec, widthUsed, heightMeasureSpec, heightUsed);
            MeasureChildWithMargins (durationTextView, widthMeasureSpec, widthUsed, heightMeasureSpec, heightUsed);
            MeasureChildWithMargins (billableIcon, widthMeasureSpec, widthUsed, heightMeasureSpec, heightUsed);
            MeasureChildWithMargins (tagsIcon, widthMeasureSpec, widthUsed, heightMeasureSpec, heightUsed);
            MeasureChildWithMargins (faderFirstRow, widthMeasureSpec, widthUsed, heightMeasureSpec, heightUsed);
            MeasureChildWithMargins (faderSecondRow, widthMeasureSpec, widthUsed, heightMeasureSpec, heightUsed);

            int heightSize = heightUsed + PaddingTop + PaddingBottom;
            SetMeasuredDimension (widthSize, heightSize);
        }

        protected override void OnLayout (bool changed, int l, int t, int r, int b)
        {
            int paddingLeft = PaddingLeft;
            int currentTop = PaddingTop;

            layoutView (colorView, paddingLeft, currentTop, colorView.MeasuredWidth, colorView.MeasuredHeight);
            paddingLeft += getWidthWithMargins (colorView);

            int durationBar = r - getMeasuredWidthWithMargins (continueImageButton);
            layoutView (continueButtonSeparator, durationBar, currentTop, continueButtonSeparator.MeasuredWidth, continueButtonSeparator.MeasuredHeight);
            layoutView (continueImageButton, durationBar, currentTop, continueImageButton.MeasuredWidth, continueImageButton.MeasuredHeight);
            int secondLineMark = durationBar;
            durationBar -= getMeasuredWidthWithMargins (durationTextView);
            layoutView (durationTextView, durationBar, currentTop, durationTextView.MeasuredWidth, durationTextView.MeasuredHeight);

            if (billableIcon.Visibility == ViewStates.Visible) {
                durationBar -= getMeasuredWidthWithMargins (billableIcon);
                layoutView (billableIcon, durationBar, currentTop, billableIcon.MeasuredWidth, billableIcon.MeasuredHeight);
            }
            if (tagsIcon.Visibility == ViewStates.Visible) {
                durationBar -= getMeasuredWidthWithMargins (tagsIcon);
                layoutView (tagsIcon, durationBar, currentTop, tagsIcon.MeasuredWidth, tagsIcon.MeasuredHeight);
            }
            durationBar -= 15;
            int usableWidthFirstLine = durationBar - paddingLeft;
            Boolean clientVisible = clientTextView.Text != String.Empty;
            int widthNeededFirstLine = clientVisible ? clientTextView.MeasuredWidth + projectTextView.MeasuredWidth : projectTextView.MeasuredWidth;

            if (widthNeededFirstLine > usableWidthFirstLine) {
                if (clientVisible) {
                    if (projectTextView.MeasuredWidth > usableWidthFirstLine) {
                        layoutView (projectTextView, paddingLeft, currentTop, usableWidthFirstLine, projectTextView.MeasuredHeight);    
                    } else {
                        layoutView (projectTextView, paddingLeft, currentTop, projectTextView.MeasuredWidth, projectTextView.MeasuredHeight);
                        layoutView (clientTextView, paddingLeft + projectTextView.MeasuredWidth, currentTop, usableWidthFirstLine - projectTextView.MeasuredWidth, clientTextView.MeasuredHeight);
                    }
                } else {
                    layoutView (projectTextView, paddingLeft, currentTop, usableWidthFirstLine, projectTextView.MeasuredHeight);
                }
            } else {
                layoutView (projectTextView, paddingLeft, currentTop, projectTextView.MeasuredWidth, projectTextView.MeasuredHeight);
                layoutView (clientTextView, paddingLeft + projectTextView.MeasuredWidth, currentTop, clientTextView.MeasuredWidth, clientTextView.MeasuredHeight);
            }
            layoutView (faderFirstRow, durationBar - faderFirstRow.MeasuredWidth, currentTop, faderFirstRow.MeasuredWidth, faderFirstRow.MeasuredHeight);


            secondLineMark -= 15;
            int usableWidthSecondLine = secondLineMark - paddingLeft;
            Boolean taskVisible = taskTextView.Text != String.Empty;
            int widthNeededSecondLine = taskVisible ? descriptionTextView.MeasuredWidth + taskTextView.MeasuredWidth : descriptionTextView.MeasuredWidth;

            if (widthNeededSecondLine > usableWidthSecondLine) {
                if (taskVisible) {
                    if (taskTextView.MeasuredWidth > usableWidthSecondLine) {
                        layoutView (taskTextView, paddingLeft, currentTop, usableWidthSecondLine, taskTextView.MeasuredHeight);
                    } else {
                        layoutView (taskTextView, paddingLeft, currentTop, taskTextView.MeasuredWidth, taskTextView.MeasuredHeight);
                        usableWidthSecondLine -= taskTextView.MeasuredWidth;
                        layoutView (descriptionTextView, paddingLeft + taskTextView.MeasuredWidth, currentTop, usableWidthSecondLine, descriptionTextView.MeasuredHeight);
                    }
                } else {
                    layoutView (descriptionTextView, paddingLeft, currentTop, usableWidthSecondLine, descriptionTextView.MeasuredHeight);
                }
            } else {
                layoutView (taskTextView, paddingLeft, currentTop, taskTextView.MeasuredWidth, taskTextView.MeasuredHeight);
                layoutView (descriptionTextView, paddingLeft + taskTextView.MeasuredWidth, currentTop, descriptionTextView.MeasuredWidth, descriptionTextView.MeasuredHeight);
            }
            layoutView (faderSecondRow, secondLineMark - faderSecondRow.MeasuredWidth, currentTop, faderSecondRow.MeasuredWidth, faderSecondRow.MeasuredHeight);
        }

        private void layoutView (View view, int left, int top, int width, int height)
        {
            var margins = (MarginLayoutParams)view.LayoutParameters;
            int leftWithMargins = left + margins.LeftMargin;
            int topWithMargins = top + margins.TopMargin;

            view.Layout (leftWithMargins, topWithMargins,
                leftWithMargins + width, topWithMargins + height);
        }

        private int getWidthWithMargins (View child)
        {
            MarginLayoutParams lp = (MarginLayoutParams)child.LayoutParameters;
            return child.Width + lp.LeftMargin + lp.RightMargin;
        }

        private int getHeightWithMargins (View child)
        {
            MarginLayoutParams lp = (MarginLayoutParams)child.LayoutParameters;
            return child.MeasuredHeight + lp.TopMargin + lp.BottomMargin;
        }

        private int getMeasuredWidthWithMargins (View child)
        {
            MarginLayoutParams lp = (MarginLayoutParams)child.LayoutParameters;
            return child.MeasuredWidth + lp.LeftMargin + lp.RightMargin;
        }

        private int getMeasuredHeightWithMargins (View child)
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

