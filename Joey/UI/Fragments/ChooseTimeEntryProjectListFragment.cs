using System;
using Android.OS;
using Android.Views;
using Android.Widget;
using Toggl.Phoebe.Data;
using Toggl.Phoebe.Data.Models;
using Toggl.Phoebe.Net;
using XPlatUtils;
using Toggl.Joey.UI.Adapters;
using ListFragment = Android.Support.V4.App.ListFragment;

namespace Toggl.Joey.UI.Fragments
{
    public class ChooseTimeEntryProjectListFragment : ListFragment
    {
        private static readonly string TimeEntryIdArgument = "com.toggl.android.time_entry_id";
        private TimeEntryModel timeEntry;

        public ChooseTimeEntryProjectListFragment (Guid timeEntryId)
        {
            var args = new Bundle ();
            args.PutString (TimeEntryIdArgument, timeEntryId.ToString ());
            Arguments = args;
        }

        public ChooseTimeEntryProjectListFragment () : base ()
        {
        }

        public ChooseTimeEntryProjectListFragment (IntPtr javaRef, Android.Runtime.JniHandleOwnership transfrer) : base (javaRef, transfrer)
        {
        }

        public override void OnCreate (Bundle state)
        {
            base.OnCreate (state);

            timeEntry = Model.ById<TimeEntryModel> (TimeEntryId);
        }

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
            if (model != null) {
                if (timeEntry == null) {
                    TimeEntryModel.StartNew (model);
                } else {
                    timeEntry.Project = model;
                }
            }
            Activity.Finish ();
        }

        private Guid TimeEntryId {
            get {
                try {
                    return Guid.Parse (Arguments.GetString (TimeEntryIdArgument));
                } catch (NullReferenceException) {
                    return Guid.Empty;
                } catch (FormatException) {
                    return Guid.Empty;
                }
            }
        }
    }
}

