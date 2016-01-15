using System;
using System.Threading.Tasks;
using Toggl.Phoebe.Helpers;
using Toggl.Phoebe.Logging;
using XPlatUtils;
using System.Reactive.Linq;

namespace Toggl.Phoebe
{
    public enum DataTag {
        UncaughtError,
        LoadMoreTimeEntries,
        RunTimeEntriesUpdate,
        StopTimeEntry,
        RemoveTimeEntryPermanently,
        RemoveTimeEntryWithUndo,
        RestoreTimeEntryFromUndo,
    }

    public class ActionNotFoundException : Exception
    {
        public DataTag Tag { get; private set; }
        public Type Register { get; private set; }

        public ActionNotFoundException (DataTag tag, Type register)
        : base (Enum.GetName (typeof (DataTag), tag) + " not found in " + register.FullName)
        {
            Tag = tag;
            Register = register;
        }
    }

    public interface IDataMsg
    {
        DataTag Tag { get; }
        Type DataType { get; }
    }

    public class DataMsg<T> : IDataMsg
    {
        public DataTag Tag { get; private set; }
        public Type DataType { get { return typeof (T); } }
        public Either<T, Exception> Data { get; private set; }

        internal DataMsg (DataTag tag, Either<T, Exception> data)
        {
            Tag = tag;
            Data = data;
        }
    }

    public static class DataMsg
    {
        public static DataMsg<T> Success<T> (DataTag tag, T data)
        {
            return new DataMsg<T> (tag, Either<T, Exception>.Left (data));
        }

        public static DataMsg<T> Error<T> (DataTag tag, Exception ex)
        {
            ServiceContainer.Resolve<ILogger> ().Error (Util.GetName (tag), ex, ex.Message);
            return new DataMsg<T> (tag, Either<T, Exception>.Right (ex));
        }
    }
}

