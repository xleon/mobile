using System;
using SQLite.Net.Attributes;

namespace Toggl.Phoebe.Data.Models
{
    public enum SyncState {
        /// <summary>
        /// ILLEGAL: The default state must be changed immediately after creation
        /// </summary>
        None,
        CreatePending,
        UpdatePending,
		Synced,
    }

    public interface ICommonData : IComparable<ICommonData>, ICloneable
    {
        Guid Id { get; }
        DateTime ModifiedAt { get; }
        DateTime? DeletedAt { get; }
        SyncState SyncState { get; }
        long? RemoteId { get; }
    }

    public abstract class CommonData : ICommonData
    {
        protected static T Create<T> (Action<T> transform = null)
        where T : CommonData, new ()
        {
            var x = new T ();
            x.Id = Guid.NewGuid ();
            x.SyncState = SyncState.CreatePending;
            if (transform != null)
                transform (x);
            return x;
        }

        protected CommonData ()
        {
            ModifiedAt = Time.UtcNow;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Data.DataObjects.CommonData"/> class copying
        /// the data from the other object.
        /// </summary>
        /// <param name="other">Instance to copy data from.</param>
        protected CommonData (ICommonData other)
        {
            Id = other.Id;
            ModifiedAt = other.ModifiedAt;
            DeletedAt = other.DeletedAt;
            SyncState = other.SyncState;
            RemoteId = other.RemoteId;
        }

        public abstract object Clone ();

        protected T With<T> (Action<T> transform)
        where T : CommonData
        {
            var newItem = (T)Clone ();
            newItem.ModifiedAt = Time.UtcNow;
            newItem.SyncState = SyncState.UpdatePending;
            transform (newItem);
            return newItem;
        }

        /// <summary>
        /// If only one of them has DeletedAt != null, favors that one
        /// Else favors the one with the most recent DeletedAt or ModifiedAt dates
        /// </summary>
        public int CompareTo (ICommonData other)
        {
            if (other == null) {
                throw new ArgumentNullException (nameof (other));
            }

            if (DeletedAt != null || other.DeletedAt != null) {
                if (other.DeletedAt == null) {
                    return 1;
                } else if (DeletedAt == null) {
                    return -1;
                } else {
                    return DeletedAt.Value.CompareTo (other.DeletedAt);
                }
            } else {
                return ModifiedAt.CompareTo (other.ModifiedAt);
            }
        }

        [PrimaryKey, AutoIncrement]
        public Guid Id { get; set; }

        public DateTime ModifiedAt { get; set; }

        public DateTime? DeletedAt { get; set; }

        public SyncState SyncState { get; set; }

        [Unique]
        public long? RemoteId { get; set; }
    }
}
