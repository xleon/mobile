using System;

namespace Toggl.Phoebe.Data
{
    public interface ISettingsStore
    {
        Guid? UserId { get; set; }

        string ApiToken { get; set; }

        DateTime? SyncLastRun { get; set; }
    }
}
