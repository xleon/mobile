using Android.App;
using Android.OS;
using Android.Views;

namespace Toggl.Chandler.UI.Fragments
{
    public class OpenAppFragment : Fragment
    {
        public override View OnCreateView (LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            var view = inflater.Inflate (Resource.Layout.OpenAppLayout, container, false);
            return view;
        }
    }
}

