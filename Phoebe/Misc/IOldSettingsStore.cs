using System;

namespace Toggl.Phoebe.Misc
{
    public interface IOldSettingsStore
    {
        // Common values
        Guid? UserId { get; }
        string ApiToken { get; }
        DateTime? SyncLastRun { get; }
        bool UseDefaultTag { get; }
        string LastAppVersion { get; }
        string ExperimentId { get; }
        int? LastReportZoomViewed { get; }
        bool GroupedTimeEntries { get; }
        bool ChooseProjectForNew { get; }
        string SortProjectsBy { get; }
        bool IsStagingMode { get; }
        bool ShowWelcome { get; }
        int ReportsCurrentItem { get; }
        // iOS Only
        bool RossReadDurOnlyNotice { get; }
        // Android only  values
        string GcmRegistrationId { get; }
        bool IdleNotification { get; }
        bool ShowNotification { get; }
    }
}
