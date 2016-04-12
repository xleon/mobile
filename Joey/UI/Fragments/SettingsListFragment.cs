using Android.App;
using Android.Content;
using Android.OS;
using Android.Views;
using Android.Widget;
using Toggl.Joey.Data;
using Toggl.Joey.UI.Activities;
using Toggl.Joey.UI.Adapters;
using Toggl.Phoebe.Analytics;
using Toggl.Phoebe.Net;
using XPlatUtils;
using DialogFragment = Android.Support.V4.App.DialogFragment;
using ListFragment = Android.Support.V4.App.ListFragment;

namespace Toggl.Joey.UI.Fragments
{
    // Need to use ordinary ListFragment here as PreferenceFragment isn't available in support library.
    public class SettingsListFragment : ListFragment
    {
        public override View OnCreateView (LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            var view = inflater.Inflate (Resource.Layout.SettingsListFragment, container, false);
            return view;
        }

        public override void OnViewCreated (View view, Bundle savedInstanceState)
        {
            base.OnViewCreated (view, savedInstanceState);

            ListView.SetClipToPadding (false);
            ListAdapter = new SettingsAdapter ();
        }

        public override void OnListItemClick (ListView l, View v, int position, long id)
        {
            ((SettingsAdapter)ListAdapter).OnItemClicked (position);
        }

        public override void OnStart ()
        {
            base.OnStart ();
            ServiceContainer.Resolve<ITracker> ().CurrentScreen = "Settings";
        }
    }
}
