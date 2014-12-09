using System;

namespace Toggl.Phoebe.Data
{
    public interface ISettingsStore
    {
        Guid? UserId { get; set; }

        string ApiToken { get; set; }

        DateTime? SyncLastRun { get; set; }

        bool UseDefaultTag { get; set; }

        string LastAppVersion { get; set; }

        string ExperimentId { get; set; }

        int? LastReportPeriodViewed { get; set; }

        string LastReportZoomViewed { get; set; }
    }
}
