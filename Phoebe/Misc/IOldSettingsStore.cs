using System;

namespace Toggl.Phoebe.Misc
{
    public interface IOldSettingsStore
    {
        // Common values
        Guid? UserId { get; }
        DateTime? SyncLastRun { get; }
        bool UseDefaultTag { get; }
        string LastAppVersion { get; }
        string ExperimentId { get; }
        int? LastReportZoomViewed { get; }
        bool GroupedTimeEntries { get; }
        string SortProjectsBy { get; }
        bool IsStagingMode { get; }
        bool ShowWelcome { get; }
        bool ChooseProjectForNew { get; }
        int ReportsCurrentItem { get; }
        // iOS Only
        bool RossReadDurOnlyNotice { get; }
        // Android only  values
        string GcmRegistrationId { get; }
        string GcmAppVersion { get; }
        bool IdleNotification { get; }
        bool ShowNotification { get; }
    }
}
