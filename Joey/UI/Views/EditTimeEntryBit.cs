
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
    public class EditTimeEntryBit : RelativeLayout
    {
        public EditTimeEntryBit (Context context) :
            base (context)
        {

            Initialize ();
        }

        public EditTimeEntryBit (Context context, IAttributeSet attrs) :
            base (context, attrs)
        {
            Initialize ();
        }

        public EditTimeEntryBit (Context context, IAttributeSet attrs, int defStyle) :
            base (context, attrs, defStyle)
        {
            Initialize ();
        }

        void Initialize ()
        {
            LayoutInflater inflater = (LayoutInflater)Context.GetSystemService (Context.LayoutInflaterService);
            inflater.Inflate (Resource.Layout.EditTimeEntryBit, this);
            TextView title = (TextView)FindViewById (Resource.Id.EditTimeEntryBitTitle);
            EditText text = (EditText)FindViewById (Resource.Id.EditTimeEntryBitText);

            text.FocusChange += (object sender, FocusChangeEventArgs e) => {
                title.Pressed = text.HasFocus;
            };
            title.Pressed = this.HasFocus;
           
        }

        public void SetTitle(string str)
        {
            TextView title = (TextView)FindViewById (Resource.Id.EditTimeEntryBitTitle);
            title.Text = str;
        }

        public void SetHint(string hint)
        {
            EditText text = (EditText)FindViewById (Resource.Id.EditTimeEntryBitText);
            text.Hint = hint;
        }

        public void DestroyAssistView() {
            TextView assistView = (TextView)FindViewById (Resource.Id.EditTimeEntryBitAssistView);
            assistView.Visibility = ViewStates.Gone;
        }

        public void DestroyArrow() {
            ImageView arrow = (ImageView)FindViewById (Resource.Id.EditTimeEntryBitArrow);
            arrow.Visibility = ViewStates.Gone;
        }
    }
}

