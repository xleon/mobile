using System;

namespace Toggl.Phoebe.Net
{
    [Flags]
    public enum SyncMode {
        Auto = 0,
        Push = 1 << 0,
        Pull = 1 << 1,
        Full = Push | Pull,
    }
}
