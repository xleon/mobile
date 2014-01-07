using System;

namespace Toggl.Phoebe
{
    [Flags]
    public enum AccessLevel
    {
        Regular = 1 << 0,
        Admin = 1 << 1,
        Any = Regular | Admin,
    }
}
