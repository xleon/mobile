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
    public abstract class DataMsg
    {
        protected Either<object, Exception> RawData { get; set; }

        protected DataMsg ()
        {
            RawData = Either<object, Exception>.Left (null);
        }

        public sealed class ReceivedFromServer : DataMsg
        {
            public Either<IEnumerable<CommonData>, Exception> Data
            {
                get { return RawData.CastLeft<IEnumerable<CommonData>> (); }
                set { RawData = value.CastLeft<object> (); }
            }

            public ReceivedFromServer (Exception ex)
            {
                Data = Either<IEnumerable<CommonData>, Exception>.Right (ex);
            }

            public ReceivedFromServer (IEnumerable<CommonData> data)
            {
                Data = Either<IEnumerable<CommonData>, Exception>.Left (data);
            }
        }

        public sealed class TimeEntriesLoad : DataMsg
        {
            public Either<object, Exception> Data
            {
                get { return RawData; }
                set { RawData = value; }
            }
        }

        public sealed class TimeEntryStop : DataMsg
        {
            public Either<ITimeEntryData, Exception> Data
            {
                get { return RawData.CastLeft<ITimeEntryData> (); }
                set { RawData = value.CastLeft<object> (); }
            }

            public TimeEntryStop (ITimeEntryData data)
            {
                Data = Either<ITimeEntryData, Exception>.Left (data);
            }
        }

        public sealed class TimeEntryContinue : DataMsg
        {
            public Either<ITimeEntryData, Exception> Data
            {
                get { return RawData.CastLeft<ITimeEntryData> (); }
                set { RawData = value.CastLeft<object> (); }
            }

            public TimeEntryContinue (ITimeEntryData data)
            {
                Data = Either<ITimeEntryData, Exception>.Left (data);
            }
        }

        public sealed class TimeEntryAdd : DataMsg
        {
            public Either<ITimeEntryData, Exception> Data
            {
                get { return RawData.CastLeft<ITimeEntryData> (); }
                set { RawData = value.CastLeft<object> (); }
            }

            public TimeEntryAdd (ITimeEntryData data)
            {
                Data = Either<ITimeEntryData, Exception>.Left (data);
            }
        }
        public sealed class TimeEntriesRemoveWithUndo : DataMsg
        {
            public Either<IEnumerable<ITimeEntryData>, Exception> Data
            {
                get { return RawData.CastLeft<IEnumerable<ITimeEntryData>> (); }
                set { RawData = value.CastLeft<object> (); }
            }

            public TimeEntriesRemoveWithUndo (IEnumerable<ITimeEntryData> data)
            {
                Data = Either<IEnumerable<ITimeEntryData>, Exception>.Left (data);
            }
        }
        public sealed class TimeEntriesRestoreFromUndo : DataMsg
        {
            public Either<IEnumerable<ITimeEntryData>, Exception> Data
            {
                get { return RawData.CastLeft<IEnumerable<ITimeEntryData>> (); }
                set { RawData = value.CastLeft<object> (); }
            }

            public TimeEntriesRestoreFromUndo (IEnumerable<ITimeEntryData> data)
            {
                Data = Either<IEnumerable<ITimeEntryData>, Exception>.Left (data);
            }
        }

        public sealed class TimeEntriesRemovePermanently : DataMsg
        {
            public Either<IEnumerable<ITimeEntryData>, Exception> Data
            {
                get { return RawData.CastLeft<IEnumerable<ITimeEntryData>> (); }
                set { RawData = value.CastLeft<object> (); }
            }

            public TimeEntriesRemovePermanently (IEnumerable<ITimeEntryData> data)
            {
                Data = Either<IEnumerable<ITimeEntryData>, Exception>.Left (data);
            }
        }

        // Launch this message when connection has been recovered after a while
        public sealed class EmptyQueueAndSync : DataMsg
        {
            public Either<DateTime, Exception> Data
            {
                get { return RawData.CastLeft<DateTime> (); }
                set { RawData = value.CastLeft<object> (); }
            }

            public EmptyQueueAndSync (DateTime data)
            {
                Data = Either<DateTime, Exception>.Left (data);
            }
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

    public static class DataSyncMsg
    {
        static public DataSyncMsg<T> Create<T> (T state, IEnumerable<ICommonData> syncData = null)
        {
            return new DataSyncMsg<T> (state, syncData);
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
                Type type;
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
}

