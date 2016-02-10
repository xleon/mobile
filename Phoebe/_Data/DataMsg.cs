using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Toggl.Phoebe._Data;
using Toggl.Phoebe._Data.Json;
using Toggl.Phoebe._Data.Models;
using Toggl.Phoebe._Helpers;
using Toggl.Phoebe.Logging;
using XPlatUtils;

namespace Toggl.Phoebe._Data
{
    public enum DataTag {
        UncaughtError,

        TimeEntryLoad,
        TimeEntryReceivedFromServer,

        TimeEntryStop,
        TimeEntryContinue,

        TimeEntriesRemoveWithUndo,
        TimeEntriesRestoreFromUndo,
        TimeEntriesRemovePermanently,

        EmptyQueue,
        EmptyQueueAndSync
    }

    public interface IDataMsg
    {
        DataTag Tag { get; }
        Type DataType { get; }
        Either<object, Exception> RawData { get; }
    }

    public class DataMsg<T> : IDataMsg
    {
        public DataTag Tag { get; private set; }
        public Type DataType { get { return typeof (T); } }
        public Either<T, Exception> Data { get; private set; }

        public Either<object, Exception> RawData
        {
            get {
                return Data.Match (
                           x => Either<object, Exception>.Left (x),
                           e => Either<object, Exception>.Right (e)
                       );
            }
        }

        internal DataMsg (DataTag tag, Either<T, Exception> data)
        {
            Data = data;
            Tag = tag;
        }
    }

    public class DataSyncMsg<T>
    {
        public T State { get; private set; }
        public IReadOnlyList<ICommonData> SyncData { get; private set; }

        public DataSyncMsg (T state, IEnumerable<ICommonData> syncData = null)
        {
            State = state;
            SyncData = syncData != null ? syncData.ToList () : new List<ICommonData> ();
        }
    }

    public class DataJsonMsg
    {
        static readonly IDictionary<string, Type> typeCache = new Dictionary<string, Type> ();

        public string TypeName { get; set; }
        public string RawData { get; set; }

        [JsonIgnore]
        public CommonJson Data
        {
            get {
                Type type = null;
                if (!typeCache.TryGetValue (TypeName, out type)) {
                    type = Assembly.GetExecutingAssembly ().GetType (TypeName);
                    typeCache.Add (TypeName, type);
                }
                return (CommonJson)JsonConvert.DeserializeObject (RawData, type);
            } set {
                RawData = JsonConvert.SerializeObject (value);
            }
        }

        public DataJsonMsg ()
        {
        }

        public DataJsonMsg (CommonJson json)
        {
            Data = json;
            TypeName = json.GetType ().FullName;
        }
    }

    #region Util
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

        public static T GetDataOrDefault<T> (this IDataMsg msg)
        {
            var typedMsg = msg as DataMsg<T>;
            if (typedMsg == null) {
                return default (T);
            }

            return typedMsg.Data.Match (x => x, e => default (T));
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

    public static class DataSyncMsg
    {
        static public DataSyncMsg<T> Create<T> (T state, IEnumerable<ICommonData> syncData = null)
        {
            return new DataSyncMsg<T> (state, syncData);
        }
    }
    #endregion
}

