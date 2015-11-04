using Android.App;
using Android.OS;
using Android.Views;

namespace Toggl.Chandler.Resources.UI.Fragments
{
    public class RecentsListFragment : Fragment
    {
        public override View OnCreateView (LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            var view = inflater.Inflate (Resource.Layout.ListFragment, container, false);
            return view;
        }
    }
}

