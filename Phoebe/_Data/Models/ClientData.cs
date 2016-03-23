using System;
using SQLite.Net.Attributes;

namespace Toggl.Phoebe._Data.Models
{
    public interface IClientData : ICommonData
    {
        string Name { get; }
        long WorkspaceRemoteId { get; }
        Guid WorkspaceId { get; }
        IClientData With (Action<ClientData> transform);
    }

    [Table ("ClientModel")]
    public class ClientData : CommonData, IClientData
    {
        public ClientData ()
        {
        }

        protected ClientData (ClientData other) : base (other)
        {
            Name = other.Name;
            WorkspaceId = other.WorkspaceId;
            WorkspaceRemoteId = other.WorkspaceRemoteId;
        }

        public override object Clone ()
        {
            return new ClientData (this);
        }

        public IClientData With (Action<ClientData> transform)
        {
            return base.With (transform);
        }

        public string Name { get; set; }

        public long WorkspaceRemoteId { get; set; }

        public Guid WorkspaceId { get; set; }
    }
}
