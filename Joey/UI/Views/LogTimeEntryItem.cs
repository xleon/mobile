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
    public class LogTimeEntryItem : ViewGroup
    {
        private View colorView;
        private TextView projectTextView;
        private TextView clientTextView;
        private TextView taskTextView;
        private TextView descriptionTextView;
        private ImageButton continueImageButton;
        private View continueButtonSeparator;
        private ImageView icTagsMiniGray;
        private ImageView icBillableMiniGray;
        private TextView durationTextView;
        private ImageView billableIcon;
        private ImageView tagsIcon;

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
        }

        protected override void OnMeasure (int widthMeasureSpec, int heightMeasureSpec)
        {
            int widthUsed = 0;
            int heightUsed = 0;
            int widthSize = MeasureSpec.GetSize (widthMeasureSpec);

            MeasureChildWithMargins (colorView, widthMeasureSpec, widthUsed, heightMeasureSpec, heightUsed);
            heightUsed += getMeasuredHeightWithMargins (colorView); // should be 64dp in xml (Make it the height defining element of the layout)

            MeasureChildWithMargins (projectTextView, widthMeasureSpec, widthUsed, heightMeasureSpec, heightUsed);
            MeasureChildWithMargins (descriptionTextView, widthMeasureSpec, widthUsed, heightMeasureSpec, heightUsed);
            MeasureChildWithMargins (clientTextView, widthMeasureSpec, widthUsed, heightMeasureSpec, heightUsed);
            MeasureChildWithMargins (taskTextView, widthMeasureSpec, widthUsed, heightMeasureSpec, heightUsed);

            MeasureChildWithMargins (continueButtonSeparator, widthMeasureSpec, widthUsed, heightMeasureSpec, heightUsed);
            MeasureChildWithMargins (continueImageButton, widthMeasureSpec, widthUsed, heightMeasureSpec, heightUsed);
            MeasureChildWithMargins (durationTextView, widthMeasureSpec, widthUsed, heightMeasureSpec, heightUsed);
            MeasureChildWithMargins (billableIcon, widthMeasureSpec, widthUsed, heightMeasureSpec, heightUsed);
            MeasureChildWithMargins (tagsIcon, widthMeasureSpec, widthUsed, heightMeasureSpec, heightUsed);

            int heightSize = heightUsed + PaddingTop + PaddingBottom;
            SetMeasuredDimension (widthSize, heightSize);
        }

        protected override void OnLayout (bool changed, int l, int t, int r, int b)
        {
            int paddingLeft = PaddingLeft;
            int paddingTop = PaddingTop;
            int currentTop = paddingTop;

            layoutView (colorView, paddingLeft, currentTop, colorView.MeasuredWidth, colorView.MeasuredHeight);
            int contentLeft = getWidthWithMargins (colorView) + paddingLeft + 20;

            layoutView (projectTextView, contentLeft, currentTop, projectTextView.MeasuredWidth, projectTextView.MeasuredHeight);
            int widthUsedFirstRow = getWidthWithMargins (projectTextView) + contentLeft;
            layoutView (clientTextView, widthUsedFirstRow, currentTop + 8, clientTextView.MeasuredWidth, clientTextView.MeasuredHeight);
            if (taskTextView.Text.Length > 0) {
                layoutView (taskTextView, contentLeft, currentTop, taskTextView.MeasuredWidth, taskTextView.MeasuredHeight);
                int widthUsedSecondRow = getWidthWithMargins (taskTextView) + contentLeft;
                layoutView (descriptionTextView, widthUsedSecondRow, currentTop, descriptionTextView.MeasuredWidth, descriptionTextView.MeasuredHeight);
            } else {
                layoutView (descriptionTextView, contentLeft, currentTop, descriptionTextView.MeasuredWidth, descriptionTextView.MeasuredHeight);
            }

            int continueButtonLeft = r - getMeasuredWidthWithMargins (continueImageButton); // use getMeasuredWidthWithMargins instead getWidthWithMargins, otherwise car will break down.
            int durationLeft = continueButtonLeft - getMeasuredWidthWithMargins (durationTextView);
            int billableLeft = durationLeft - getMeasuredWidthWithMargins (billableIcon);
            int tagsLeft = billableLeft - getMeasuredWidthWithMargins (tagsIcon);
            layoutView (continueButtonSeparator, continueButtonLeft, currentTop, continueButtonSeparator.MeasuredWidth, continueButtonSeparator.MeasuredHeight);
            layoutView (continueImageButton, continueButtonLeft, currentTop, continueImageButton.MeasuredWidth, continueImageButton.MeasuredHeight);
            layoutView (durationTextView, durationLeft, currentTop, durationTextView.MeasuredWidth, durationTextView.MeasuredHeight);
            layoutView (billableIcon, billableLeft, currentTop, billableIcon.MeasuredWidth, billableIcon.MeasuredHeight);
            layoutView (tagsIcon, tagsLeft, currentTop, tagsIcon.MeasuredWidth, tagsIcon.MeasuredHeight);

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

