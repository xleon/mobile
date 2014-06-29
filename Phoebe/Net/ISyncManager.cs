using System;

namespace Toggl.Phoebe.Net
{
    public interface ISyncManager
    {
        void Run (SyncMode mode = SyncMode.Full);

        bool IsRunning { get; }
    }
}
