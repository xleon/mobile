using System;

namespace TogglDoodle.Models
{
    [AttributeUsage (AttributeTargets.Property)]
    public class DontDirtyAttribute : Attribute
    {
    }
}
