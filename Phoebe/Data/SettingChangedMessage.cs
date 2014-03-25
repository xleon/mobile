using System;

namespace Toggl.Phoebe.Data
{
    public class SettingChangedMessage : Message
    {
        private readonly string name;

        public SettingChangedMessage (ISettingsStore sender, string name) : base (sender)
        {
            this.name = name;
        }

        public string Name {
            get { return name; }
        }
    }
}
