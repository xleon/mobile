using System;
using SQLite.Net.Attributes;

namespace Toggl.Phoebe._Data.Models
{
    public interface ICommonData
    {
        Guid Id { get; }
        DateTime ModifiedAt { get; }
        DateTime? DeletedAt { get; }
        bool IsDirty { get; }
        long? RemoteId { get; }
        bool RemoteRejected { get; }
    }

    public abstract class CommonData : ICommonData
    {
        protected CommonData ()
        {
            ModifiedAt = Time.UtcNow;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Toggl.Phoebe.Data.DataObjects.CommonData"/> class copying
        /// the data from the other object.
        /// </summary>
        /// <param name="other">Instance to copy data from.</param>
        protected CommonData (ICommonData other)
        {
            Id = other.Id;
            ModifiedAt = other.ModifiedAt;
            DeletedAt = other.DeletedAt;
            IsDirty = other.IsDirty;
            RemoteId = other.RemoteId;
            RemoteRejected = other.RemoteRejected;
        }

        [PrimaryKey, AutoIncrement]
        public Guid Id { get; set; }

        public DateTime ModifiedAt { get; set; }

        public DateTime? DeletedAt { get; set; }

        public bool IsDirty { get; set; }

        [Unique]
        public long? RemoteId { get; set; }

        public bool RemoteRejected { get; set; }
    }
}
