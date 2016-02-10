using System;
using SQLite.Net.Attributes;

namespace Toggl.Phoebe._Data.Models
{
    public interface ICommonData : IComparable<ICommonData>
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

        /// <summary>
        /// If only one of them has DeletedAt != null, favors that one
        /// Else favors the one with the most recent DeletedAt or ModifiedAt dates
        /// </summary>
        public int CompareTo (ICommonData other)
        {
            if (other == null) {
                throw new ArgumentNullException ("arguments cannot be null");
            }

            if (this.DeletedAt != null || other.DeletedAt != null) {
                if (other.DeletedAt == null) {
                    return 1;
                } else if (this.DeletedAt == null) {
                    return -1;
                } else {
                    return this.DeletedAt > other.DeletedAt ? 1 : -1;
                }
            } else {
                return this.ModifiedAt > other.ModifiedAt ? 1 : -1;
            }
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
