using System;
using System.Linq;
using Android.App;
using Android.OS;
using Android.Views;
using Toggl.Joey.UI.Adapters;

namespace Toggl.Joey.UI.Fragments
{
    public class RecentTimeEntriesListFragment : ListFragment
    {
        public override void OnViewCreated (View view, Bundle savedInstanceState)
        {
            base.OnViewCreated (view, savedInstanceState);

            ListAdapter = new RecentTimeEntriesAdapter ();
        }
    }
}
