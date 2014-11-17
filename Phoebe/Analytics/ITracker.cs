using System;

namespace Toggl.Phoebe.Analytics
{
    public interface ITracker
    {
        string CurrentScreen { set; }
        void SendAppInitTime (TimeSpan duration);
    }
}
