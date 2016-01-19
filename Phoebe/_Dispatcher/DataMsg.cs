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
        TimeEntryLoad,
        TimeEntryLoadFromServer,
        TimeEntryStop,
        TimeEntryRemove,
        TimeEntryRemoveWithUndo,
        TimeEntryRestoreFromUndo,
    }

    public enum DataDir {
        Incoming,
        Outcoming,
        None
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
        DataDir Dir { get; }
        Type DataType { get; }
    }

    public class DataMsg<T> : IDataMsg
    {
        public DataTag Tag { get; private set; }
        public DataDir Dir { get; private set; }
        public Type DataType { get { return typeof (T); } }
        public Either<T, Exception> Data { get; private set; }

        internal DataMsg (Either<T, Exception> data, DataTag tag, DataDir dir)
        {
            Data = data;
            Tag = tag;
            Dir = dir;
        }
    }

    public static class DataMsg
    {
        public static U MatchData<T,U> (this IDataMsg msg, Func<T,U> left, Func<Exception,U> right)
        {
            var typedMsg = msg as DataMsg<T>;
            if (typedMsg == null) {
                right (new InvalidCastException (typeof (T).FullName));
            }
            return typedMsg.Data.Match (left, right);
        }

        public static Task<U> MatchDataAsync<T,U> (this IDataMsg msg, Func<T,Task<U>> left, Func<Exception,U> right)
        {
            var typedMsg = msg as DataMsg<T>;
            if (typedMsg == null) {
                right (new InvalidCastException (typeof (T).FullName));
            }
            return typedMsg.Data.Match (left, ex => Task.Run (() => right (ex)));
        }


        public static T ForceGetData<T> (this IDataMsg msg)
        {
            var typedMsg = msg as DataMsg<T>;
            if (typedMsg == null) {
                throw new InvalidCastException (typeof (T).FullName);
            }

            return typedMsg.Data.Match (x => x, e => { throw e; });
        }

        public static DataMsg<T> Success<T> (T data, DataTag tag, DataDir dir)
        {
            return new DataMsg<T> (Either<T, Exception>.Left (data), tag, dir);
        }

        public static DataMsg<T> Error<T> (Exception ex, DataTag tag, DataDir dir)
        {
            ServiceContainer.Resolve<ILogger> ().Error (Util.GetName (tag), ex, ex.Message);
            return new DataMsg<T> (Either<T, Exception>.Right (ex), tag, dir);
        }
    }
}

