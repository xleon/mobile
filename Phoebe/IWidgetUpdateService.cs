using System;
using System.Collections.Generic;
using Toggl.Phoebe.Data.Models;

namespace Toggl.Phoebe
{
    public interface IWidgetUpdateService
    {
        List<WidgetSyncManager.WidgetEntryData> LastEntries { get; set; }

        string RunningEntryDuration { get; set; }

        bool IsUserLogged { get; set; }

        Guid EntryIdStarted { get; set; }

        void ShowNewTimeEntryScreen (TimeEntryModel currentTimeEntry);
    }
}