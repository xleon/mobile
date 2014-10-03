using System;
using Android.OS;
using Android.Views;
using Toggl.Joey.UI.Views;
using Fragment = Android.Support.V4.App.Fragment;
using FragmentManager = Android.Support.V4.App.FragmentManager;
using Toggl.Phoebe.Data.Reports;
using Toggl.Phoebe.Data;
using System.Threading.Tasks;
using XPlatUtils;
using Toggl.Phoebe.Net;

namespace Toggl.Joey.UI.Fragments
{
    public class ReportsFragment : Fragment
    {
        public override void OnCreate (Bundle savedInstanceState)
        {
            base.OnCreate (savedInstanceState);
        }

        public override View OnCreateView (LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            var view = inflater.Inflate (Resource.Layout.ReportsFragment, container, false);
            LoadData ();
            return view;
        }

        private async Task LoadData ()
        {
            var summary = new SummaryReportView ();
            summary.Period = ZoomLevel.Month;
            await summary.Load (1);
            var user = ServiceContainer.Resolve<AuthManager> ().User;
            Console.WriteLine ("defaultWorkspaceId: {0}", user.DefaultWorkspaceId);
        }
    }
}
