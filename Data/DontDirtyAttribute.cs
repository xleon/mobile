using System;

namespace Toggl.Phoebe.Data
{
    [AttributeUsage (AttributeTargets.Property, Inherited = true)]
    public class DontDirtyAttribute : Attribute
    {
    }
}
