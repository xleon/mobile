using System;
using System.Collections.Generic;
using SQLite.Net.Interop;
using SQLite.Net.Platform.Generic;
using Toggl.Phoebe._Data.Models;

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
    }
}

