using Toggl.Phoebe.Data.DataObjects;
using System;


namespace Toggl.Phoebe.Data
{
    public enum DataAction {
        Put,
        Delete
    }

    public abstract class TimeEntryAction
    {
        #region Cases
        public sealed class Put : TimeEntryAction
        {
            public readonly TimeEntryData Data;
            Put (TimeEntryData data)
            {
                Data = data;
            }
            public static TimeEntryAction New (TimeEntryData data)
            {
                return new TimeEntryAction.Put (data);
            }
        }

        public sealed class Delete : TimeEntryAction
        {
            public readonly TimeEntryData Data;
            protected Delete (TimeEntryData data)
            {
                Data = data;
            }
            public static TimeEntryAction New (TimeEntryData data)
            {
                return new TimeEntryAction.Delete (data);
            }
        }

        public sealed class Load : TimeEntryAction
        {
            public readonly bool IsInitial;
            Load (bool isInitial)
            {
                IsInitial = isInitial;
            }
            public static TimeEntryAction New (bool isInitial = false)
            {
                return new Load (isInitial);
            }
        }
        #endregion

        public T Match<T> (Func<Put, T> put = null, Func<Delete, T> delete = null,
                           Func<Load, T> batch = null, Func<T> @default = null)
        {
            if (put != null && this is Put) {
                return put ((Put)this);
            } else if (delete != null && this is Delete) {
                return delete ((Delete)this);
            } else if (batch != null && this is Load) {
                return batch ((Load)this);
            } else if (@default != null) {
                return @default ();
            }
            throw new NotSupportedException ();
        }
    }
}
