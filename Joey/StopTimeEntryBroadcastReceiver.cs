using System;
using System.Linq;
using System.Threading.Tasks;
using Android.Content;
using Toggl.Phoebe.Data;
using Toggl.Phoebe.Data.DataObjects;
using Toggl.Phoebe.Data.Models;
using Toggl.Phoebe.Net;
using XPlatUtils;

namespace Toggl.Joey
{
    [BroadcastReceiver (Exported = true)]
    class StopTimeEntryBroadcastReceiver: BroadcastReceiver
    {
        private const string LogTag = "StopTimeEntryBroadcastReceiver";

        public override void OnReceive (Context context, Intent intent)
        {
            // TODO: Should move this to an Android service to allow for async execution
            var userId = ServiceContainer.Resolve<AuthManager> ().GetUserId ();
            var dataStore = ServiceContainer.Resolve<IDataStore> ();
            var tasks = dataStore.Table<TimeEntryData> ()
                .QueryAsync (r => r.State == TimeEntryState.Running && r.DeletedAt == null && r.UserId == userId)
                .Result
                .Select (data => new TimeEntryModel (data).StopAsync ());
            Task.WhenAll (tasks).Wait ();

            // Try initialising components
            var app = context.ApplicationContext as AndroidApp;
            if (app != null) {
                app.InitializeComponents ();
            }
        }
    }
}
