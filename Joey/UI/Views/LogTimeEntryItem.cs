using System;
using Android.Content;
using Android.Util;
using Android.Views;
using Android.Widget;

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
            view = LayoutInflater.FromContext (context).Inflate (Resource.Layout.LogTimeEntryListItem, this, true);
            Initialize ();
        }

        public LogTimeEntryItem (Context context, IAttributeSet attrs, int defStyle) : base (context, attrs, defStyle)
        {
            view = LayoutInflater.FromContext (context).Inflate (Resource.Layout.LogTimeEntryListItem, this, true);
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
            int faderMargin = 10;

            layoutView (colorView, paddingLeft, currentTop, colorView.MeasuredWidth, colorView.MeasuredHeight);
            paddingLeft += getWidthWithMargins (colorView);

            int durationBar = r - continueImageButton.MeasuredWidth;
            layoutView (continueButtonSeparator, durationBar + 6, currentTop, continueButtonSeparator.MeasuredWidth, continueButtonSeparator.MeasuredHeight);
            layoutView (continueImageButton, durationBar, currentTop, continueImageButton.MeasuredWidth, continueImageButton.MeasuredHeight);
            int secondLineMark = durationBar - faderMargin;
            durationBar -= durationTextView.MeasuredWidth;
            layoutView (durationTextView, durationBar, currentTop, durationTextView.MeasuredWidth, durationTextView.MeasuredHeight);

            if (billableIcon.Visibility == ViewStates.Visible) {
                durationBar -= billableIcon.MeasuredWidth;
                layoutView (billableIcon, durationBar, currentTop, billableIcon.MeasuredWidth, billableIcon.MeasuredHeight);
            }
            if (tagsIcon.Visibility == ViewStates.Visible) {
                durationBar -= tagsIcon.MeasuredWidth;
                layoutView (tagsIcon, durationBar, currentTop, tagsIcon.MeasuredWidth, tagsIcon.MeasuredHeight);
            }

            durationBar -= faderMargin;
            int usableWidthFirstLine = durationBar - paddingLeft;
            int firstWidth = getFirstElementWidth (usableWidthFirstLine, projectTextView.MeasuredWidth);

            layoutView (projectTextView, paddingLeft, currentTop, firstWidth, projectTextView.MeasuredHeight);    
            if (clientTextView.Text != String.Empty) {
                layoutView (clientTextView, paddingLeft + firstWidth, currentTop, getSecondElementWidth (usableWidthFirstLine, projectTextView.MeasuredWidth, clientTextView.MeasuredWidth), clientTextView.MeasuredHeight);    
            }
            layoutView (faderFirstRow, usableWidthFirstLine + paddingLeft - faderFirstRow.MeasuredWidth, currentTop, faderFirstRow.MeasuredWidth, faderFirstRow.MeasuredHeight);

            int usableWidthSecondLine = secondLineMark - paddingLeft;

            if (taskTextView.Text != String.Empty) {
                layoutView (taskTextView, paddingLeft, currentTop, getFirstElementWidth (usableWidthSecondLine, taskTextView.MeasuredWidth), taskTextView.MeasuredHeight);    
                layoutView (descriptionTextView, paddingLeft + getFirstElementWidth (usableWidthSecondLine, taskTextView.MeasuredWidth), currentTop, getSecondElementWidth (usableWidthSecondLine, taskTextView.MeasuredWidth, descriptionTextView.MeasuredWidth), descriptionTextView.MeasuredHeight);    
            } else {
                layoutView (descriptionTextView, paddingLeft, currentTop, getSecondElementWidth (usableWidthSecondLine, taskTextView.MeasuredWidth, descriptionTextView.MeasuredWidth), descriptionTextView.MeasuredHeight);    
            }
            layoutView (faderSecondRow, secondLineMark - faderSecondRow.MeasuredWidth, currentTop, faderSecondRow.MeasuredWidth, faderSecondRow.MeasuredHeight);
        }

        private int getFirstElementWidth (int usable, int first)
        {
            return first > usable ? usable : first;
        }

        private int getSecondElementWidth (int usable, int first, int second)
        {
            int firstActual = getFirstElementWidth (usable, first);
            if (firstActual == usable) {
                return 0;
            } else if (usable < firstActual + second) {
                return usable - firstActual;
            } else {
                return second;
            }
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

