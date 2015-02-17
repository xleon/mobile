using System;
using System.Collections.Generic;
using Toggl.Phoebe.Data.Models;

namespace Toggl.Phoebe
{
    public interface IWidgetUpdateService
    {
        void SetLastEntries (List<WidgetSyncManager.WidgetEntryData> lastEntries);

        void SetRunningEntryDuration (string duration);

        void SetUserLogged (bool isLogged);

        void SetAppActivated (bool isActivated);

        void SetAppOnBackground (bool isBackground);

        void ShowNewTimeEntryScreen (TimeEntryModel currentTimeEntry);

        Guid GetEntryIdStarted();
    }
}