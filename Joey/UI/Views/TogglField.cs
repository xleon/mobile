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
            var inflater = (LayoutInflater)Context.GetSystemService (Context.LayoutInflaterService);
            inflater.Inflate (Resource.Layout.TogglField, this);

            var title = (TextView)FindViewById (Resource.Id.EditTimeEntryBitTitle);
            var text = (EditText)FindViewById (Resource.Id.EditTimeEntryBitText);

            text.FocusChange += (sender, e) => {
                title.Selected = text.HasFocus;
            };
        }

        public EditText TextField
        {
            get { return (EditText)FindViewById (Resource.Id.EditTimeEntryBitText); }
        }

        public TogglField SetName (string name)
        {
            var title = (TextView)FindViewById (Resource.Id.EditTimeEntryBitTitle);
            title.Text = name;
            var text = (EditText)FindViewById (Resource.Id.EditTimeEntryBitText);
            text.Hint = name;
            return this;
        }

        public TogglField SetName (int resourceId)
        {
            var title = (TextView)FindViewById (Resource.Id.EditTimeEntryBitTitle);
            title.SetText (resourceId);
            var text = (EditText)FindViewById (Resource.Id.EditTimeEntryBitText);
            text.SetHint (resourceId);
            return this;
        }

        public TogglField SetAssistViewTitle (string title)
        {
            var assistView = (TextView)FindViewById (Resource.Id.EditTimeEntryBitAssistView);
            assistView.Text = title;
            assistView.Visibility = ViewStates.Visible;
            return this;
        }

        public TogglField DestroyAssistView()
        {
            var assistView = (TextView)FindViewById (Resource.Id.EditTimeEntryBitAssistView);
            assistView.Visibility = ViewStates.Gone;
            return this;
        }

        public TogglField RestoreAssistView()
        {
            var assistView = (TextView)FindViewById (Resource.Id.EditTimeEntryBitAssistView);
            assistView.Visibility = ViewStates.Visible;
            return this;
        }

        public TogglField DestroyArrow()
        {
            var arrow = (ImageView)FindViewById (Resource.Id.EditTimeEntryBitArrow);
            arrow.Visibility = ViewStates.Gone;
            return this;
        }

        public TogglField SimulateButton()
        {
            var title = (TextView)FindViewById (Resource.Id.EditTimeEntryBitTitle);
            var text = (EditText)FindViewById (Resource.Id.EditTimeEntryBitText);
            text.Touch += (sender, e) => {
                e.Handled = false;
                title.Pressed = e.Event.Action != MotionEventActions.Up;
            };
            text.Focusable = text.Clickable = false;
            return this;
        }
    }
}

