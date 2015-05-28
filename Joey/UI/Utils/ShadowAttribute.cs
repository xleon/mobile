using System;

namespace Toggl.Joey.UI.Utils
{
    [AttributeUsage (AttributeTargets.Class)]
    public class ShadowAttribute : Attribute
    {
        [Flags]
        public enum Mode { Top = 1, Bottom = 2 };
        public readonly Mode Modes;

        public ShadowAttribute (Mode modes)
        {
            Modes = modes;
        }
    }
}

