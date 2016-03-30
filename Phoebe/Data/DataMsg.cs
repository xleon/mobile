using System;
using System.Collections.Generic;
using System.Linq;
using Toggl.Phoebe.Data.Models;
using Toggl.Phoebe.Helpers;
using Toggl.Phoebe.Net;
using Toggl.Phoebe.Reactive;

namespace Toggl.Phoebe.Data
{
    public abstract class DataMsg
    {
        protected Either<object, Exception> RawData { get; set; }

        protected DataMsg ()
        {
            RawData = Either<object, Exception>.Left (null);
        }

        public sealed class Request : DataMsg
        {
            public ServerRequest Data
            {
                get { return RawData.ForceLeft () as ServerRequest; }
                private set { RawData = Either<object, Exception>.Left ((object)value); }
            }

            public Request (ServerRequest req)
            {
                Data = req;
            }
        }

        public sealed class ReceivedFromDownload : DataMsg
        {
            public Either<IEnumerable<CommonData>, Exception> Data
            {
                get { return RawData.CastLeft<IEnumerable<CommonData>> (); }
                set { RawData = value.CastLeft<object> (); }
            }

            public ReceivedFromDownload (Exception ex)
            {
                Data = Either<IEnumerable<CommonData>, Exception>.Right (ex);
            }

            public ReceivedFromDownload (IEnumerable<CommonData> data)
            {
                Data = Either<IEnumerable<CommonData>, Exception>.Left (data);
            }
        }

        public sealed class ReceivedFromSync : DataMsg
        {
            public Tuple<UserData, DateTime> FullSyncInfo { get; private set; }

            public Either<IEnumerable<CommonData>, Exception> Data
            {
                get { return RawData.CastLeft<IEnumerable<CommonData>> (); }
                set { RawData = value.CastLeft<object> (); }
            }

            public ReceivedFromSync (Exception ex)
            {
                Data = Either<IEnumerable<CommonData>, Exception>.Right (ex);
            }

            public ReceivedFromSync (IEnumerable<CommonData> data, Tuple<UserData, DateTime> fullSyncInfo = null)
            {
                FullSyncInfo = fullSyncInfo;
                Data = Either<IEnumerable<CommonData>, Exception>.Left (data);
            }
        }

        public sealed class ResetState : DataMsg
        {
        }

        public sealed class TimeEntriesLoad : DataMsg
        {
        }

        public sealed class FullSync : DataMsg
        {
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
            public bool StartedByFAB { get; private set; }
            public Either<ITimeEntryData, Exception> Data
            {
                get { return RawData.CastLeft<ITimeEntryData> (); }
                set { RawData = value.CastLeft<object> (); }
            }

            public TimeEntryContinue (ITimeEntryData data, bool startedByFAB = false)
            {
                StartedByFAB = startedByFAB;
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

        public sealed class TimeEntriesRemove : DataMsg
        {
            public Either<IEnumerable<ITimeEntryData>, Exception> Data
            {
                get { return RawData.CastLeft<IEnumerable<ITimeEntryData>> (); }
                set { RawData = value.CastLeft<object> (); }
            }

            public TimeEntriesRemove (ITimeEntryData data)
            : this (new ITimeEntryData[] { data })
            {
            }

            public TimeEntriesRemove (IEnumerable<ITimeEntryData> data)
            {
                Data = Either<IEnumerable<ITimeEntryData>, Exception>.Left (data);
            }
        }

        public sealed class TagsPut : DataMsg
        {
            public Either<IEnumerable<ITagData>, Exception> Data
            {
                get { return RawData.CastLeft<IEnumerable<ITagData>> (); }
                set { RawData = value.CastLeft<object> (); }
            }

            public TagsPut (IEnumerable<ITagData> tags)
            {
                Data = Either<IEnumerable<ITagData>, Exception>.Left (tags);
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
            public Either<IProjectData, Exception> Data
            {
                get { return RawData.CastLeft<IProjectData> (); }
                set { RawData = value.CastLeft<object> (); }
            }

            public ProjectDataPut (IProjectData project)
            {
                Data = Either<IProjectData, Exception>.Left (project);
            }
        }

        public sealed class ClientDataPut : DataMsg
        {
            public Either<IClientData, Exception> Data
            {
                get { return RawData.CastLeft<IClientData> (); }
                set { RawData = value.CastLeft<object> (); }
            }

            public ClientDataPut (IClientData data)
            {
                Data = Either<IClientData, Exception>.Left (data);
            }
        }

        public sealed class UserDataPut : DataMsg
        {
            public class AuthException : Exception
            {
                public AuthResult AuthResult { get; private set; }
                public AuthException (AuthResult authResult)
                : base (Enum.GetName (typeof (AuthResult), authResult))
                {
                    AuthResult = authResult;
                }
            }

            public Either<IUserData, AuthException> Data
            {
                get { return RawData.Cast<IUserData, AuthException> (); }
                set { RawData = value.Cast<object, Exception> (); }
            }

            public UserDataPut (AuthResult authResult, IUserData data = null)
            {
                if (authResult == AuthResult.Success && data != null) {
                    Data = Either<IUserData, AuthException>.Left (data);
                } else {
                    Data = Either<IUserData, AuthException>.Right (new AuthException (authResult));
                }
            }
        }

        public sealed class UpdateSetting : DataMsg
        {
            public class SettingChangeInfo : Tuple<string, object>
            {
                public SettingChangeInfo (string propName, object value) : base (propName, value)
                {
                }
            }

            public Either<SettingChangeInfo, Exception> Data
            {
                get { return RawData.CastLeft<SettingChangeInfo> (); }
                set { RawData = value.CastLeft<object> (); }
            }

            public UpdateSetting (string settingName, object value)
            {
                var info = new SettingChangeInfo (settingName, value);
                Data = Either<SettingChangeInfo, Exception>.Left (info);
            }
        }
    }

    public abstract class ServerRequest
    {
        protected ServerRequest () {}

        public sealed class FullSync : ServerRequest
        {
        }

        public sealed class DownloadEntries : ServerRequest
        {
        }

        public sealed class Authenticate : ServerRequest
        {
            public readonly string Username;
            public readonly string Password;
            public Authenticate (string username, string password)
            {
                Username = username;
                Password = password;
            }
        }
        public sealed class AuthenticateWithGoogle : ServerRequest
        {
            public readonly string AccessToken;
            public AuthenticateWithGoogle (string accessToken)
            {
                AccessToken = accessToken;
            }
        }
        public sealed class SignUp : ServerRequest
        {
            public readonly string Email;
            public readonly string Password;
            public SignUp (string email, string password)
            {
                Email = email;
                Password = password;
            }
        }
        public sealed class SignUpWithGoogle : ServerRequest
        {
            public readonly string AccessToken;
            public SignUpWithGoogle (string accessToken)
            {
                AccessToken = accessToken;
            }
        }
    }

    public class DataSyncMsg<T>
    {
        public T State { get; private set; }
        public SyncTestOptions SyncTest { get; private set; }
        public IReadOnlyList<ICommonData> SyncData { get; private set; }

        readonly List<ServerRequest> serverRequests;
        public IEnumerable<ServerRequest> ServerRequests { get { return serverRequests; } }

        public DataSyncMsg (T state, IEnumerable<ICommonData> syncData = null, IEnumerable<ServerRequest> serverRequests = null, SyncTestOptions syncTest = null)
        {
            if (syncData != null) {
                DataSyncMsg.CheckSyncDataOrder (syncData);
            }

            State = state;
            SyncTest = syncTest;
            SyncData = syncData != null ? syncData.ToList () : new List<ICommonData> ();
            this.serverRequests = serverRequests != null ? serverRequests.ToList () : new List<ServerRequest> ();
        }

        public DataSyncMsg<T> With (SyncTestOptions syncTest)
        {
            return new DataSyncMsg<T> (this.State, this.SyncData, this.ServerRequests, syncTest);
        }

        public DataSyncMsg<U> Cast<U> ()
        {
            return new DataSyncMsg<U> ((U) (object)this.State, this.SyncData, this.serverRequests, this.SyncTest);
        }
    }

    public static class DataSyncMsg
    {
        public static IReadOnlyList<string> DataOrder = new string[] {
            nameof (UserData), nameof(WorkspaceData), nameof(ClientData), nameof(ProjectData),
            nameof(TaskData), nameof(TagData), nameof(TimeEntryData)
        };

        public static void CheckSyncDataOrder (IEnumerable<ICommonData> syncData)
        {
            int lastIndex = 0;
            foreach (var data in syncData) {
                var dataType = data.GetType ().Name;
                var curIndex = DataOrder.IndexOf (t => t == dataType);
                if (curIndex < lastIndex) {
                    throw new Exception (string.Format ("{0} cannot come after {1}", dataType, DataOrder[lastIndex]));
                }
                lastIndex = curIndex;
            }
        }

        static public DataSyncMsg<T> Create<T> (T state, IEnumerable<ICommonData> syncData = null, ServerRequest request = null, SyncTestOptions syncTest = null)
        {
            var serverRequests = request != null ? new List<ServerRequest> { request } : null;
            return new DataSyncMsg<T> (state, syncData, serverRequests, syncTest);
        }
    }

    public class SyncTestOptions
    {
        public bool IsConnectionAvailable { get; private set; }
        public Action<AppState, List<CommonData>, List<SyncManager.QueueItem>> Continuation { get; private set; }

        public SyncTestOptions (bool isCnnAvailable, Action<AppState, List<CommonData>, List<SyncManager.QueueItem>> continuation)
        {
            IsConnectionAvailable = isCnnAvailable;
            Continuation = continuation;
        }
    }
}
