using System;
using Android.App;
using Android.OS;
using Android.Views;
using Android.Widget;
using Toggl.Phoebe.Data;
using Toggl.Phoebe.Data.Models;
using Toggl.Phoebe.Net;
using XPlatUtils;
using Toggl.Joey.UI.Adapters;

namespace Toggl.Joey.UI.Fragments
{
    public class NewTimeEntryProjectListFragment : ListFragment
    {
        public override void OnViewCreated (View view, Bundle savedInstanceState)
        {
            base.OnViewCreated (view, savedInstanceState);

            var user = ServiceContainer.Resolve<AuthManager> ().User;
            ListAdapter = new ProjectsAdapter (user.GetAvailableProjects ().ToView ());
        }

        public override void OnListItemClick (ListView l, View v, int position, long id)
        {
            var adapter = l.Adapter as ProjectsAdapter;
            if (adapter == null)
                return;

            var model = adapter.GetModel (position);
            if (model == null) {
                TimeEntryModel.StartNew ();
            } else {
                TimeEntryModel.StartNew (model);
            }
            Activity.Finish ();
        }
    }
}

