using System;

namespace TogglDoodle.Models
{
    [AttributeUsage (AttributeTargets.Property, Inherited = true)]
    public class DontDirtyAttribute : Attribute
    {
    }
}
