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

    public class NetWorkPresenceMock : Net.INetworkPresence
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
        public static string fakeUserEmail = "test@toggl.com";
        public static string fakeUserPassword = "123";
        public static string fakeGoogleId = "12345";

        public Random rnd = new Random ();
        public IList<CommonJson> ReceivedItems = new List<CommonJson> ();


        public Task<T> Create<T> (string authToken, T jsonObject) where T : CommonJson
        {
            ReceivedItems.Add (jsonObject);
            jsonObject.RemoteId = rnd.Next (100);
            return Task.FromResult (jsonObject);
        }
        public Task<T> Get<T> (string authToken, long id) where T : CommonJson
        {
            throw new NotImplementedException ();
        }
        public Task<List<T>> List<T> (string authToken) where T : CommonJson
        {
            throw new NotImplementedException ();
        }
        public Task<T> Update<T> (string authToken, T jsonObject) where T : CommonJson
        {
            return Task.Run (() => {
                ReceivedItems.Add (jsonObject);
                return jsonObject;
            });
        }
        public Task Delete<T> (string authToken, T jsonObject) where T : CommonJson
        {
            return Task.Run (() => {
                ReceivedItems.Add (jsonObject);
            });
        }
        public Task Delete<T> (string authToken, IEnumerable<T> jsonObjects) where T : CommonJson
        {
            throw new NotImplementedException ();
        }
        public Task<UserJson> GetUser (string username, string password)
        {
            Task.Delay (500);
            if (username == fakeUserEmail && password == fakeUserPassword) {
                var user = new UserJson () {
                    Email = fakeUserEmail,
                    Password = fakeUserPassword,
                    Name = "Test",
                    DefaultWorkspaceRemoteId = 123
                };
                return Task.FromResult (user);
            }
            return null;
        }

        public Task<UserJson> GetUser (string googleAccessToken)
        {
            if (googleAccessToken == fakeGoogleId) {
                var user = new UserJson () {
                    Email = fakeUserEmail,
                    Password = fakeUserPassword,
                    Name = "Test",
                    DefaultWorkspaceRemoteId = 123
                };
                return Task.FromResult (user);
            }
            return null;
        }

        public Task<List<ClientJson>> ListWorkspaceClients (string authToken, long workspaceId)
        {
            throw new NotImplementedException ();
        }
        public Task<List<ProjectJson>> ListWorkspaceProjects (string authToken, long workspaceId)
        {
            throw new NotImplementedException ();
        }
        public Task<List<WorkspaceUserJson>> ListWorkspaceUsers (string authToken, long workspaceId)
        {
            throw new NotImplementedException ();
        }
        public Task<List<TaskJson>> ListWorkspaceTasks (string authToken, long workspaceId)
        {
            throw new NotImplementedException ();
        }
        public Task<List<TaskJson>> ListProjectTasks (string authToken, long projectId)
        {
            throw new NotImplementedException ();
        }
        public Task<List<ProjectUserJson>> ListProjectUsers (string authToken, long projectId)
        {
            throw new NotImplementedException ();
        }
        public Task<List<TimeEntryJson>> ListTimeEntries (string authToken, DateTime start, DateTime end, System.Threading.CancellationToken cancellationToken)
        {
            throw new NotImplementedException ();
        }
        public Task<List<TimeEntryJson>> ListTimeEntries (string authToken, DateTime start, DateTime end)
        {
            throw new NotImplementedException ();
        }
        public Task<List<TimeEntryJson>> ListTimeEntries (string authToken, DateTime end, int days, System.Threading.CancellationToken cancellationToken)
        {
            throw new NotImplementedException ();
        }
        public Task<List<TimeEntryJson>> ListTimeEntries (string authToken, DateTime end, int days)
        {
            throw new NotImplementedException ();
        }
        public Task<UserRelatedJson> GetChanges (string authToken, DateTime? since)
        {
            throw new NotImplementedException ();
        }
        public Task CreateFeedback (string authToken, FeedbackJson jsonObject)
        {
            throw new NotImplementedException ();
        }
        public Task CreateExperimentAction (string authToken, ActionJson jsonObject)
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

            var timerState =
                new TimerState (
                authResult: Net.AuthResult.None,
                downloadResult: DownloadResult.Empty,
                user: userData,
                activeEntry: ActiveEntryInfo.Empty,
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
