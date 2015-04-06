using Android.Content;
using Android.Util;
using Android.Views;
using Android.Widget;

namespace Toggl.Joey.UI.Views
{
    public class TogglField : RelativeLayout
    {
        public TogglField (Context context) :
        base (context)
        {
            Initialize ();
        }

        public TogglField (Context context, IAttributeSet attrs) :
        base (context, attrs)
        {
            Initialize ();
        }

        void Initialize ()
        {
            LayoutInflater inflater = (LayoutInflater)Context.GetSystemService (Context.LayoutInflaterService);
            inflater.Inflate (Resource.Layout.TogglField, this);

            TextView title = (TextView)FindViewById (Resource.Id.EditTimeEntryBitTitle);
            EditText text = (EditText)FindViewById (Resource.Id.EditTimeEntryBitText);

            text.FocusChange += (object sender, FocusChangeEventArgs e) => {
                title.Selected = text.HasFocus;
            };
        }

        public EditText TextField
        {
            get { return (EditText)FindViewById (Resource.Id.EditTimeEntryBitText); }
        }

        public TogglField SetName (string name)
        {
            TextView title = (TextView)FindViewById (Resource.Id.EditTimeEntryBitTitle);
            title.Text = name;
            EditText text = (EditText)FindViewById (Resource.Id.EditTimeEntryBitText);
            text.Hint = name;
            return this;
        }

        public TogglField SetName (int resourceId)
        {
            TextView title = (TextView)FindViewById (Resource.Id.EditTimeEntryBitTitle);
            title.SetText (resourceId);
            EditText text = (EditText)FindViewById (Resource.Id.EditTimeEntryBitText);
            text.SetText (resourceId);
            return this;
        }

        public TogglField DestroyAssistView()
        {
            TextView assistView = (TextView)FindViewById (Resource.Id.EditTimeEntryBitAssistView);
            assistView.Visibility = ViewStates.Gone;
            return this;
        }

        public TogglField DestroyArrow()
        {
            ImageView arrow = (ImageView)FindViewById (Resource.Id.EditTimeEntryBitArrow);
            arrow.Visibility = ViewStates.Gone;
            return this;
        }

        public TogglField SimulateButton()
        {
            TextView title = (TextView)FindViewById (Resource.Id.EditTimeEntryBitTitle);
            EditText text = (EditText)FindViewById (Resource.Id.EditTimeEntryBitText);
            text.Touch += (object sender, TouchEventArgs e) => {
                e.Handled = false;
                title.Pressed = e.Event.Action != MotionEventActions.Up;
            };
            text.Focusable = text.Clickable = false;
            return this;
        }
    }
}

