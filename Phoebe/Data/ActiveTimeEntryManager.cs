using System;
using System.Linq;
using System.Threading.Tasks;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Helpers;
using PropertyChanged;
using Toggl.Phoebe.Data.DataObjects;
using Toggl.Phoebe.Data.Models;
using Toggl.Phoebe.Net;
using XPlatUtils;

namespace Toggl.Phoebe.Data
{
    [ImplementPropertyChanged]
    public class ActiveTimeEntryManager : ObservableObject, IDisposable
    {
        public static readonly string PropertyActiveTimeEntry = "ActiveTimeEntry";

        private Subscription<StartStopMessage> subscriptionStateChange;
        private Subscription<SyncFinishedMessage> subscriptionSyncFinished;
        protected AuthManager AuthManager { get; set; }
        Binding<UserData, UserData> authBinding;

        public ActiveTimeEntryManager ()
        {
            var bus = ServiceContainer.Resolve<MessageBus> ();
            AuthManager = ServiceContainer.Resolve<AuthManager> ();
            authBinding = this.SetBinding (() => AuthManager.User).WhenSourceChanges (async () => await UpdateRunningTimeEntry ());
            subscriptionSyncFinished = bus.Subscribe<SyncFinishedMessage> (async msg => await UpdateRunningTimeEntry ());
            subscriptionStateChange = bus.Subscribe<StartStopMessage> (OnTimeEntryStateChanged);
        }

        ~ActiveTimeEntryManager ()
        {
            Dispose (false);
        }

        public void Dispose ()
        {
            Dispose (true);
            GC.SuppressFinalize (this);
        }

        private void Dispose (bool disposing)
        {
            if (disposing) {
                var bus = ServiceContainer.Resolve<MessageBus> ();

                if (subscriptionStateChange != null) {
                    bus.Unsubscribe (subscriptionStateChange);
                    subscriptionStateChange = null;
                }

                if (subscriptionSyncFinished != null) {
                    bus.Unsubscribe (subscriptionSyncFinished);
                    subscriptionSyncFinished = null;
                }
            }
        }

        private void OnTimeEntryStateChanged (StartStopMessage msg)
        {
            ActiveTimeEntry = msg.TimeEntry.State == TimeEntryState.Running ? msg.TimeEntry : TimeEntryModel.GetDraft ();
            IsRunning = msg.TimeEntry.State == TimeEntryState.Running;
        }

        private async Task UpdateRunningTimeEntry ()
        {
            var store = ServiceContainer.Resolve<IDataStore> ();
            var teList = await store.Table<TimeEntryData> ()
                         .Where (r => r.State == TimeEntryState.Running && r.DeletedAt == null)
                         .OrderByDescending (r => r.StartTime)
                         .ToListAsync ();

            ActiveTimeEntry = teList.Any () ? teList.FirstOrDefault () : TimeEntryModel.GetDraft ();
            IsRunning = ActiveTimeEntry.State == TimeEntryState.Running;
        }

        public bool IsRunning { get; private set; }

        public TimeEntryData ActiveTimeEntry { get; private set; }
    }
}
