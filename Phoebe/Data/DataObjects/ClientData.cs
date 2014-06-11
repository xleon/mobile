using System;
using SQLite;

namespace Toggl.Phoebe.Data.DataObjects
{
    [Table ("Client")]
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

        public Guid WorkspaceId { get; set; }
    }
}
