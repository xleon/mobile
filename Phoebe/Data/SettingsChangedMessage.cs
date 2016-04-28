using System;
using Toggl.Phoebe.Misc;

namespace Toggl.Phoebe.Data
{
    public class SettingChangedMessage : Message
    {
        private readonly string name;

        public SettingChangedMessage(IOldSettingsStore sender, string name) : base(sender)
        {
            this.name = name;
        }

        public string Name
        {
            get { return name; }
        }
    }
}

