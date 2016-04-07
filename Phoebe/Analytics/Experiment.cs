using System;

namespace Toggl.Phoebe.Analytics
{
    public sealed class Experiment
    {
        public Experiment()
        {
            Enabled = true;
        }

        public string Id { get; set; }
        public DateTime? StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public bool FreshInstallOnly { get; set; }
        public bool Enabled { get; set; }
        public Action SetUp { get; set; }

        public override string ToString()
        {
            return Id;
        }
    }
}
