using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Toggl.Phoebe.Data;
using Toggl.Phoebe.Data.DataObjects;
using Toggl.Phoebe.Data.Json;
using Toggl.Phoebe.Helpers;
using Toggl.Phoebe.Logging;
using XPlatUtils;

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

    public class DataSyncMsg
    {
        public DataDir Dir { get; private set; }
        public DataVerb Verb { get; private set; }
        public CommonData Data { get; private set; }

        public DataSyncMsg (DataDir dir, DataVerb verb, CommonData data)
        {
            Dir = dir;
            Verb = verb;
            Data = data;
        }
    }

    public interface IDataSyncGroup
    {
        IEnumerable<DataSyncMsg> SyncMessages { get; }
    }

    public class DataJsonMsg
    {
        public DataVerb Verb { get; set; }
        public string TypeName { get; set; }
        public IDictionary<string, object> RawData { get; set; }

        [JsonIgnore]
        public CommonJson Data
        {
            get {
                var type = Assembly.GetExecutingAssembly ().GetType (TypeName);
                var data = Activator.CreateInstance (type);
                foreach (var prop in type.GetProperties ()) {
                    prop.SetValue (data, RawData [prop.Name]);
                }
                return (CommonJson)data;
            }
            set {
                var data = value;
                var dic = new Dictionary<string, object> ();
                foreach (var prop in data.GetType ().GetProperties ()) {
                    dic.Add (prop.Name, prop.GetValue (data, null));
                }
                RawData = dic;
            }
        }

        public DataJsonMsg (DataVerb action, CommonJson json)
        {
            Verb = action;
            Data = json;
            TypeName = json.GetType ().FullName;
        }
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

