using System;

namespace Toggl.Phoebe.Models
{
    [AttributeUsage (AttributeTargets.Property, Inherited = true)]
    public class DontDirtyAttribute : Attribute
    {
    }
}
