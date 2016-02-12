using System;
using System.Collections.Generic;
using SQLite.Net.Interop;
using SQLite.Net.Platform.Generic;
using Toggl.Phoebe._Data.Models;
using Toggl.Phoebe._Reactive;

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

    public static class Util
    {
        static Guid userId = Guid.NewGuid ();
        static Guid workspaceId = Guid.NewGuid ();

        public static TimeEntryData CreateTimeEntryData (DateTime startTime)
        {
            return new TimeEntryData {
                Id = Guid.NewGuid (),
                StartTime = startTime,
                StopTime = startTime.AddMinutes (1),
                UserId = userId,
                WorkspaceId = workspaceId,
                Description = "Test Entry",
                State = TimeEntryState.Finished
            };
        }

        public static AppState GetInitAppState ()
        {
            var userData = new UserData { Id = userId };
            var workspaceData = new WorkspaceData { Id = workspaceId };

            // Set initial pagination Date to the beginning of the next day.
            // So, we can include all entries created -Today-.
            var downloadFrom = Time.UtcNow.Date.AddDays (1);

            var timerState =
                new TimerState (
                    downloadInfo: new DownloadInfo (true, false, downloadFrom, downloadFrom),
                    user: userData,
                    workspaces: new Dictionary<Guid, WorkspaceData> { { workspaceId, workspaceData } },
                    projects: new Dictionary<Guid, ProjectData> (),
                    clients: new Dictionary<Guid, ClientData> (),
                    tasks: new Dictionary<Guid, TaskData> (),
                    tags: new Dictionary<Guid, TagData> (),
                    timeEntries: new Dictionary<Guid, RichTimeEntry> ());

            return new AppState (timerState);
        }
    }
}

