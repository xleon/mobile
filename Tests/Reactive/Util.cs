using System;
using System.Collections.Generic;
using SQLite.Net.Interop;
using SQLite.Net.Platform.Generic;
using Toggl.Phoebe._Data.Json;
using Toggl.Phoebe._Data.Models;
using Toggl.Phoebe._Net;
using Toggl.Phoebe._Reactive;
using System.Threading.Tasks;
using Toggl.Phoebe.Analytics;

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

    public class ToggleClientMock : ITogglClient
    {
        public Random rnd = new Random ();
        public IList<CommonJson> ReceivedItems = new List<CommonJson> ();

        public Task<T> Create<T> (T jsonObject) where T : CommonJson
        {
            return Task.Run (() => {
                ReceivedItems.Add (jsonObject);
                jsonObject.RemoteId = rnd.Next (100);
                return jsonObject;
            });
        }
        public Task<T> Get<T> (long id) where T : CommonJson
        {
            throw new NotImplementedException ();
        }
        public Task<List<T>> List<T> () where T : CommonJson
        {
            throw new NotImplementedException ();
        }
        public Task<T> Update<T> (T jsonObject) where T : CommonJson
        {
            return Task.Run (() => {
                ReceivedItems.Add (jsonObject);
                return jsonObject;
            });
        }
        public Task Delete<T> (T jsonObject) where T : CommonJson
        {
            return Task.Run (() => {
                ReceivedItems.Add (jsonObject);
            });
        }
        public Task Delete<T> (IEnumerable<T> jsonObjects) where T : CommonJson
        {
            throw new NotImplementedException ();
        }
        public Task<UserJson> GetUser (string username, string password)
        {
            throw new NotImplementedException ();
        }
        public Task<UserJson> GetUser (string googleAccessToken)
        {
            throw new NotImplementedException ();
        }
        public Task<List<ClientJson>> ListWorkspaceClients (long workspaceId)
        {
            throw new NotImplementedException ();
        }
        public Task<List<ProjectJson>> ListWorkspaceProjects (long workspaceId)
        {
            throw new NotImplementedException ();
        }
        public Task<List<WorkspaceUserJson>> ListWorkspaceUsers (long workspaceId)
        {
            throw new NotImplementedException ();
        }
        public Task<List<TaskJson>> ListWorkspaceTasks (long workspaceId)
        {
            throw new NotImplementedException ();
        }
        public Task<List<TaskJson>> ListProjectTasks (long projectId)
        {
            throw new NotImplementedException ();
        }
        public Task<List<ProjectUserJson>> ListProjectUsers (long projectId)
        {
            throw new NotImplementedException ();
        }
        public Task<List<TimeEntryJson>> ListTimeEntries (DateTime start, DateTime end, System.Threading.CancellationToken cancellationToken)
        {
            throw new NotImplementedException ();
        }
        public Task<List<TimeEntryJson>> ListTimeEntries (DateTime start, DateTime end)
        {
            throw new NotImplementedException ();
        }
        public Task<List<TimeEntryJson>> ListTimeEntries (DateTime end, int days, System.Threading.CancellationToken cancellationToken)
        {
            throw new NotImplementedException ();
        }
        public Task<List<TimeEntryJson>> ListTimeEntries (DateTime end, int days)
        {
            throw new NotImplementedException ();
        }
        public Task<UserRelatedJson> GetChanges (DateTime? since)
        {
            throw new NotImplementedException ();
        }
        public Task CreateFeedback (FeedbackJson jsonObject)
        {
            throw new NotImplementedException ();
        }
        public Task CreateExperimentAction (ActionJson jsonObject)
        {
            throw new NotImplementedException ();
        }
    }

    public class TrackerMock : ITracker
    {
        public string CurrentScreen
        {
            set { } // Do nothing
        }
        public void SendAccountCreateEvent (AccountCredentials credentialsType)
        {
            // Do nothing
        }
        public void SendAccountLoginEvent (AccountCredentials credentialsType)
        {
            // Do nothing
        }
        public void SendAccountLogoutEvent ()
        {
            // Do nothing
        }
        public void SendAppInitTime (TimeSpan duration)
        {
            // Do nothing
        }
        public void SendSettingsChangeEvent (SettingName settingName)
        {
            // Do nothing
        }
        public void SendTimerStartEvent (TimerStartSource startSource)
        {
            // Do nothing
        }
        public void SendTimerStopEvent (TimerStopSource stopSource)
        {
            // Do nothing
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
                    downloadInfo: new DownloadInfo (false, true, false, downloadFrom, downloadFrom),
                    user: userData,
                    workspaces: new Dictionary<Guid, WorkspaceData> { { WorkspaceId, workspaceData } },
                    projects: new Dictionary<Guid, ProjectData> (),
                    workspaceUsers: new Dictionary<Guid, WorkspaceUserData> (),
                    projectUsers: new Dictionary<Guid, ProjectUserData> (),
                    clients: new Dictionary<Guid, ClientData> (),
                    tasks: new Dictionary<Guid, TaskData> (),
                    tags: new Dictionary<Guid, TagData> (),
                    timeEntries: new Dictionary<Guid, RichTimeEntry> (),
                    activeTimeEntry: null);

            return new AppState (timerState);
        }
    }
}

