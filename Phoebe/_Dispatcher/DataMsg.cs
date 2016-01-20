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

    public class CommonDataWrapper<T> : CommonData
    {
        public T Data { get; private set; }

        public CommonDataWrapper (T data)
        {
            Data = data;
        }
    }

    public class DataActionMsg<T>
    {
        public T Data { get; private set; }
        public DataAction Action { get; private set; }

        public DataActionMsg (T data, DataAction action)
        {
            Data = data;
            Action = action;
        }

        public DataActionMsg (DataChangeMessage msg)
        {
            Data = (T)msg.Data;
            Action = msg.Action;
        }
    }

    public interface IDataMsg
    {
        DataTag Tag { get; }
        DataDir Dir { get; }
        Type DataType { get; }
    }

    public class DataMsg<T> : IDataMsg where T : CommonData
    {
        public DataTag Tag { get; private set; }
        public DataDir Dir { get; private set; }
        public Type DataType { get { return typeof (T); } }
        public Either<IList<DataActionMsg<T>>, Exception> Data { get; private set; }

        internal DataMsg (Either<IList<DataActionMsg<T>>, Exception> data, DataTag tag, DataDir dir)
        {
            Data = data;
            Tag = tag;
            Dir = dir;
        }
    }

    public static class DataMsg
    {
        public static U MatchData<T,U> (
            this IDataMsg msg, Func<IList<DataActionMsg<T>>,U> left, Func<Exception,U> right)
            where T : CommonData
        {
            var typedMsg = msg as DataMsg<T>;
            if (typedMsg == null) {
                right (new InvalidCastException (typeof (T).FullName));
            }
            return typedMsg.Data.Match (left, right);
        }

        public static Task<U> MatchDataAsync<T,U> (
            this IDataMsg msg, Func<IList<DataActionMsg<T>>,Task<U>> left, Func<Exception,U> right)
            where T : CommonData
        {
            var typedMsg = msg as DataMsg<T>;
            if (typedMsg == null) {
                right (new InvalidCastException (typeof (T).FullName));
            }
            return typedMsg.Data.Match (left, ex => Task.Run (() => right (ex)));
        }

        public static IList<DataActionMsg<T>> ForceGetData<T> (this IDataMsg msg)
            where T : CommonData
        {
            var typedMsg = msg as DataMsg<T>;
            if (typedMsg == null) {
                throw new InvalidCastException (typeof (T).FullName);
            }

            return typedMsg.Data.Match (x => x, e => { throw e; });
        }

        public static T ForceGetWrappedData<T> (this IDataMsg msg)
        {
            var typedMsg = msg as DataMsg<CommonDataWrapper<T>>;
            if (typedMsg == null) {
                throw new InvalidCastException (typeof (T).FullName);
            }

            return typedMsg.Data.Match (x => x.Single ().Data.Data, e => { throw e; });
        }


        public static IDataMsg Success<T> (IList<DataActionMsg<T>> items, DataTag tag, DataDir dir)
            where T : CommonData
        {
            return new DataMsg<T> (Either<IList<DataActionMsg<T>>, Exception>.Left (items), tag, dir);
        }

        public static IDataMsg Success<T> (T data, DataAction action, DataTag tag, DataDir dir)
            where T : CommonData
        {
            var li = new List<DataActionMsg<T>> { new DataActionMsg<T> (data, action) };
            return Success (li, tag, dir);
        }

        public static IDataMsg Error<T> (Exception ex, DataTag tag, DataDir dir)
            where T : CommonData
        {
            ServiceContainer.Resolve<ILogger> ().Error (Util.GetName (tag), ex, ex.Message);
            return new DataMsg<T> (Either<IList<DataActionMsg<T>>, Exception>.Right (ex), tag, dir);
        }
    }
}

