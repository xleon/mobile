using System;
using System.Linq;
using System.Threading.Tasks;
using Toggl.Phoebe.Helpers;
using Toggl.Phoebe.Logging;
using XPlatUtils;
using System.Reactive.Linq;
using System.Collections.Generic;
using Toggl.Phoebe.Data.DataObjects;
using Toggl.Phoebe.Data;

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

    public enum DataVerb {
        Post,
        Put,
        Delete
    }

    public enum DataDir {
        Incoming,
        Outcoming,
        None
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
            Data = data;
            Tag = tag;
        }
    }

    public interface IDataSyncMsg
    {
        DataDir Dir { get; }
        DataVerb Verb { get; }
        Type DataType { get; }
        CommonData RawData { get; }
    }

    public class DataSyncMsg<T> : IDataSyncMsg where T : CommonData
    {
        public DataDir Dir { get; private set; }
        public DataVerb Verb { get; private set; }
        public T Data { get; private set; }
        public Type DataType { get { return typeof(T); } }
        public CommonData RawData { get { return Data; } }

        public DataSyncMsg (DataDir dir, DataVerb verb, T data)
        {
            Dir = dir;
            Verb = verb;
            Data = data;
        }
    }

    public interface IDataSyncGroupMsg
    {
        Type DataType { get; }
        IEnumerable<IDataSyncMsg> RawMessages { get; }
    }

    public interface IDataSyncGroupMsg<T> : IDataSyncGroupMsg where T : CommonData
    {
        IEnumerable<DataSyncMsg<T>> Messages { get; }
    }

    public static class DataMsg
    {
        public static DataVerb ToVerb (this DataAction action)
        {
            return action == DataAction.Put ? DataVerb.Put : DataVerb.Delete;
        }

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

        public static IDataMsg Success<T> (DataTag tag, T data)
        {
            return new DataMsg<T> (tag, Either<T, Exception>.Left (data));
        }

        public static IDataMsg Error<T> (DataTag tag, Exception ex)
        {
            ServiceContainer.Resolve<ILogger> ().Error (Util.GetName (tag), ex, ex.Message);
            return new DataMsg<T> (tag, Either<T, Exception>.Right (ex));
        }
    }
}

