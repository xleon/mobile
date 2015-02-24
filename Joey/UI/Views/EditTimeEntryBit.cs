using Android.Content;
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

        public EditText TextField
        {
            get { return (EditText)FindViewById (Resource.Id.EditTimeEntryBitText); }
        }

        public EditTimeEntryBit SetName (string name)
        {
            TextView title = (TextView)FindViewById (Resource.Id.EditTimeEntryBitTitle);
            title.Text = name;
            EditText text = (EditText)FindViewById (Resource.Id.EditTimeEntryBitText);
            text.Hint = name;
            return this;
        }

        public EditTimeEntryBit DestroyAssistView()
        {
            TextView assistView = (TextView)FindViewById (Resource.Id.EditTimeEntryBitAssistView);
            assistView.Visibility = ViewStates.Gone;
            return this;
        }

        public EditTimeEntryBit DestroyArrow()
        {
            ImageView arrow = (ImageView)FindViewById (Resource.Id.EditTimeEntryBitArrow);
            arrow.Visibility = ViewStates.Gone;
            return this;
        }
    }
}

