using System;

namespace Toggl.Phoebe
{
    public interface ITimeProvider
    {
        DateTime Now { get; }

        DateTime UtcNow { get; }

        string TimeZoneId { get; }
    }
}
