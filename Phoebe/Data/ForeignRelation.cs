using System;

namespace Toggl.Phoebe.Data
{
    public class ForeignRelation
    {
        public string Name { get; set; }

        public Type Type { get; set; }

        public bool Required { get; set; }

        public Guid? Id { get; set; }
    }
}
