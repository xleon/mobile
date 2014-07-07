using System;
using SQLite;

namespace Toggl.Phoebe.Data.DataObjects
{
    [Table ("ClientModel")]
    public class ClientData : CommonData
    {
        public ClientData ()
        {
        }

        public ClientData (ClientData other) : base (other)
        {
            Name = other.Name;
            WorkspaceId = other.WorkspaceId;
        }

        public string Name { get; set; }

        [ForeignRelation (typeof(WorkspaceData))]
        public Guid WorkspaceId { get; set; }
    }
}
