using Android.App;
using Android.OS;
using Android.Views;
using Android.Widget;
using Toggl.Chandler.UI.Activities;

namespace Toggl.Chandler.UI.Fragments
{
    public class OpenAppFragment : Fragment
    {
        private TextView openAppTextView;

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            var view = inflater.Inflate(Resource.Layout.OpenAppLayout, container, false);
            openAppTextView = view.FindViewById<TextView> (Resource.Id.OpenApp);
            openAppTextView.Click += OpenAppClick;
            return view;
        }

        private void OpenAppClick(object sender, System.EventArgs e)
        {
            ((MainActivity)Activity).RequestHandheldOpen();
        }
    }
}

