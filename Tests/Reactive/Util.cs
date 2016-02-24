using System;
using System.Collections.Generic;
using SQLite.Net.Interop;
using SQLite.Net.Platform.Generic;
using Toggl.Phoebe._Data.Models;
using Toggl.Phoebe._Reactive;
using System.Threading.Tasks;

namespace Toggl.Phoebe.Tests.Reactive
{
    public class PlatformUtils : IPlatformUtils
    {
        public string AppIdentifier { get; set; }

        public string AppVersion { get; set; }

        public bool IsWidgetAvailable { get; set; }

        public ISQLitePlatform SQLiteInfo
        {
            get {
                return new SQLitePlatformGeneric ();
            }
        }

        public void DispatchOnUIThread (Action action)
        {
            action ();
        }
    }

    public class NetWorkPresenceMock : Toggl.Phoebe.Net.INetworkPresence
    {
        public bool IsNetworkPresent { get { return false; } }

        public void RegisterSyncWhenNetworkPresent ()
        {
            throw new NotImplementedException ();
        }

        public void UnregisterSyncWhenNetworkPresent ()
        {
            throw new NotImplementedException ();
        }
    }

    public static class Util
    {
        public static readonly Guid UserId = Guid.NewGuid ();
        public static readonly Guid WorkspaceId = Guid.NewGuid ();

        public static TimeEntryData CreateTimeEntryData (
            DateTime startTime, long userRemoteId = 0, long workspaceRemoteId = 0)
        {
            var id = Guid.NewGuid ();
            return new TimeEntryData {
                Id = id,
                Description = id.ToString (),
                IsBillable = true,
                DurationOnly = true,
                StartTime = startTime,
                StopTime = startTime.AddMinutes (1),
                Tags = new List<string> (),
                TaskRemoteId = null,
                UserRemoteId = userRemoteId,
                WorkspaceRemoteId = workspaceRemoteId,
                UserId = UserId,
                WorkspaceId = WorkspaceId,
                State = TimeEntryState.Finished,
            };
        }

        public static TaskCompletionSource<T> CreateTask<T> (int timeout = 10000)
        {
            var tcs = new TaskCompletionSource<T> ();
            var timer = new System.Timers.Timer (timeout);
            timer.Elapsed += (s, e) => {
                timer.Stop ();
                tcs.SetException (new TimeoutException ());
            };
            timer.Start ();
            return tcs;
        }

        public static AppState GetInitAppState ()
        {
            var userData = new UserData { Id = UserId };
            var workspaceData = new WorkspaceData { Id = WorkspaceId };

            // Set initial pagination Date to the beginning of the next day.
            // So, we can include all entries created -Today-.
            var downloadFrom = Time.UtcNow.Date.AddDays (1);

            var timerState =
                new TimerState (
                    downloadInfo: new DownloadInfo (true, false, downloadFrom, downloadFrom),
                    user: userData,
                    workspaces: new Dictionary<Guid, WorkspaceData> { { WorkspaceId, workspaceData } },
                    projects: new Dictionary<Guid, ProjectData> (),
                    workspaceUsers: new Dictionary<Guid, WorkspaceUserData> (),
                    projectUsers: new Dictionary<Guid, ProjectUserData> (),
                    clients: new Dictionary<Guid, ClientData> (),
                    tasks: new Dictionary<Guid, TaskData> (),
                    tags: new Dictionary<Guid, TagData> (),
                    timeEntries: new Dictionary<Guid, RichTimeEntry> ());

            return new AppState (timerState);
        }
    }
}

