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

        public sealed class TimeEntriesSync : DataMsg
        {
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

        public sealed class TimeEntryPut : DataMsg
        {
            public Either<ITimeEntryData, Exception> Data
            {
                get { return RawData.CastLeft<ITimeEntryData> (); }
                set { RawData = value.CastLeft<object> (); }
            }

            public TimeEntryPut (ITimeEntryData data)
            {
                Data = Either<ITimeEntryData, Exception>.Left (data);
            }
        }

        public sealed class TimeEntryDelete : DataMsg
        {
            public Either<ITimeEntryData, Exception> Data
            {
                get { return RawData.CastLeft<ITimeEntryData> (); }
                set { RawData = value.CastLeft<object> (); }
            }

            public TimeEntryDelete (ITimeEntryData data)
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

        public sealed class TagsPut : DataMsg
        {
            public Either<Tuple<ITimeEntryData,IEnumerable<string>>, Exception> Data
            {
                get { return RawData.CastLeft<Tuple<ITimeEntryData,IEnumerable<string>>> (); }
                set { RawData = value.CastLeft<object> (); }
            }

            public TagsPut (Tuple<ITimeEntryData,IEnumerable<string>> data)
            {
                Data = Either<Tuple<ITimeEntryData,IEnumerable<string>>, Exception>.Left (data);
            }
        }

        public sealed class TagPut : DataMsg
        {
            public Either<Tuple<Guid, string>, Exception> Data
            {
                get { return RawData.CastLeft<Tuple<Guid, string>> (); }
                set { RawData = value.CastLeft<object> (); }
            }

            public TagPut (Guid workspaceId, string tag)
            {
                Data = Either<Tuple<Guid, string>, Exception>.Left (Tuple.Create (workspaceId, tag));
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

        public sealed class ProjectDataPut : DataMsg
        {
            public Either<ProjectData, Exception> Data
            {
                get { return RawData.CastLeft<ProjectData> (); }
                set { RawData = value.CastLeft<object> (); }
            }

            public ProjectDataPut (ProjectData data)
            {
                Data = Either<ProjectData, Exception>.Left (data);
            }
        }

        public sealed class ProjectUserDataPut : DataMsg
        {
            public Either<ProjectUserData, Exception> Data
            {
                get { return RawData.CastLeft<ProjectUserData> (); }
                set { RawData = value.CastLeft<object> (); }
            }

            public ProjectUserDataPut (ProjectUserData data)
            {
                Data = Either<ProjectUserData, Exception>.Left (data);
            }
        }

        public sealed class ClientDataPut : DataMsg
        {
            public Either<ClientData, Exception> Data
            {
                get { return RawData.CastLeft<ClientData> (); }
                set { RawData = value.CastLeft<object> (); }
            }

            public ClientDataPut (ClientData data)
            {
                Data = Either<ClientData, Exception>.Left (data);
            }
        }
    }

    public class DataSyncMsg<T>
    {
        public T State { get; private set; }
        public bool IsSyncRequested { get; private set; }
        public SyncTestOptions SyncTest { get; private set; }
        public IReadOnlyList<ICommonData> SyncData { get; private set; }

        public DataSyncMsg (T state, IEnumerable<ICommonData> syncData = null, bool isSyncRequested = false, SyncTestOptions syncTest = null)
        {
            State = state;
            SyncTest = syncTest;
            IsSyncRequested = isSyncRequested;
            SyncData = syncData != null ? syncData.ToList () : new List<ICommonData> ();
        }

        public DataSyncMsg<T> With (SyncTestOptions syncTest)
        {
            return new DataSyncMsg<T> (this.State, this.SyncData, this.IsSyncRequested, syncTest);
        }
    }

    public static class DataSyncMsg
    {
        static public DataSyncMsg<T> Create<T> (T state, IEnumerable<ICommonData> syncData = null, bool isSyncRequested = false, SyncTestOptions syncTest = null)
        {
            return new DataSyncMsg<T> (state, syncData, isSyncRequested, syncTest);
        }
    }

    public class DataJsonMsg
    {
        static readonly IDictionary<string, Type> typeCache = new Dictionary<string, Type> ();

        public Guid LocalId { get; set; }
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

        public DataJsonMsg (Guid localId, CommonJson json)
        {
            LocalId = localId;
            Data = json;
            TypeName = json.GetType ().FullName;
        }
    }

    public class SyncTestOptions
    {
        public bool IsConnectionAvailable { get; private set; }
        public Action<List<CommonData>, List<DataJsonMsg>> Continuation { get; private set; }

        public SyncTestOptions (bool isCnnAvailable, Action<List<CommonData>, List<DataJsonMsg>> continuation)
        {
            IsConnectionAvailable = isCnnAvailable;
            Continuation = continuation;
        }
    }
}

