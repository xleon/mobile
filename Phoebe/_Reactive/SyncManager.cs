using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Toggl.Phoebe.Logging;
using Toggl.Phoebe._Data;
using Toggl.Phoebe._Data.Json;
using Toggl.Phoebe._Data.Models;
using Toggl.Phoebe._Helpers;
using Toggl.Phoebe._Net;
using XPlatUtils;

namespace Toggl.Phoebe._Reactive
{
    public class SyncManager
    {
        public const string QueueId = "SYNC_OUT";
        public static SyncManager Singleton { get; private set; }

        public static void Init ()
        {
            Singleton = Singleton ?? new SyncManager ();
        }

        public static void Cleanup ()
        {
            Singleton = null;
        }

        readonly string Tag = typeof (SyncManager).Name;
        readonly JsonMapper mapper;
        readonly Net.INetworkPresence networkPresence;
        readonly ISyncDataStore dataStore;
        readonly ITogglClient client;
        readonly Subject<Tuple<ServerRequest, AppState>> requestManager = new Subject<Tuple<ServerRequest, AppState>> ();


        SyncManager ()
        {
            mapper = new JsonMapper ();
            networkPresence = ServiceContainer.Resolve<Net.INetworkPresence> ();
            dataStore = ServiceContainer.Resolve<ISyncDataStore> ();
            client = ServiceContainer.Resolve<ITogglClient> ();

            StoreManager.Singleton
            .Observe ()
            .SelectAsync (EnqueueOrSend)
            .Subscribe ();

            requestManager
            // Make sure requests are run one after the other
            .Synchronize ()
            // TODO: Use Throttle here?
            .SelectAsync (async x => {
                if (x.Item1 is ServerRequest.DownloadEntries) {
                    await DownloadEntries (x.Item2);
                } else if (x.Item1 is ServerRequest.FullSync) {
                    await FullSync (x.Item2);
                } else if (x.Item1 is ServerRequest.Authenticate) {
                    var req = x.Item1 as ServerRequest.Authenticate;
                    await AuthenticateAsync (req.Username, req.Password);
                } else if (x.Item1 is ServerRequest.AuthenticateWithGoogle) {
                    var req = x.Item1 as ServerRequest.AuthenticateWithGoogle;
                    await AuthenticateWithGoogleAsync (req.AccessToken);
                } else if (x.Item1 is ServerRequest.SignUp) {
                    var req = x.Item1 as ServerRequest.SignUp;
                    await SignupAsync (req.Email, req.Password);
                } else if (x.Item1 is ServerRequest.SignUpWithGoogle) {
                    var req = x.Item1 as ServerRequest.SignUpWithGoogle;
                    await SignupWithGoogleAsync (req.AccessToken);
                }
            })
            .Subscribe ();
        }

        void logError (Exception ex, string msg = "Failed to sync")
        {
            var logger = ServiceContainer.Resolve<ILogger> ();
            logger.Error (Tag, ex, msg);
        }

        void logInfo (string msg, Exception exc = null)
        {
            var logger = ServiceContainer.Resolve<ILogger> ();
            if (exc == null)
                logger.Info (Tag, msg);
            else
                logger.Info (Tag, exc, msg);
        }

        void logWarning (string msg, Exception exc = null)
        {
            var logger = ServiceContainer.Resolve<ILogger> ();
            if (exc == null)
                logger.Warning (Tag, msg);
            else
                logger.Warning (Tag, exc, msg);
        }

        async Task EnqueueOrSend (DataSyncMsg<AppState> syncMsg)
        {
            var authToken = syncMsg.State.User.ApiToken;
            var remoteObjects = new List<CommonData> ();
            var enqueuedItems = new List<DataJsonMsg> ();
            var isConnected = syncMsg.SyncTest != null
                              ? syncMsg.SyncTest.IsConnectionAvailable
                              : networkPresence.IsNetworkPresent;

            // Try to empty queue first
            bool queueEmpty = await tryEmptyQueue (authToken, remoteObjects, isConnected);

            // Deal with messages
            foreach (var msg in syncMsg.SyncData) {


                if (msg is TimeEntryData) {
                    var d = (TimeEntryData)msg;
                    if (d.ProjectRemoteId == null) {
                        Console.WriteLine ("vacaaar! : "  + StoreManager.Singleton.AppState.Projects [d.ProjectId].RemoteId);
                        ((TimeEntryData)msg).ProjectRemoteId = StoreManager.Singleton.AppState.Projects [d.ProjectId].RemoteId;
                    }
                }

                var exported = mapper.MapToJson (msg);

                if (queueEmpty && isConnected) {
                    try {
                        await SendMessage (authToken, remoteObjects, msg.Id, exported);
                    } catch (Exception ex) {
                        logError (ex);
                        Enqueue (msg.Id, exported, enqueuedItems);
                        queueEmpty = false;
                    }
                } else {
                    Enqueue (msg.Id, exported, enqueuedItems);
                    queueEmpty = false;
                }
            }

            // TODO: Try to empty queue again?

            // Return remote objects
            if (remoteObjects.Count > 0) {
                RxChain.Send (new DataMsg.ReceivedFromDownload (remoteObjects));
            }

            foreach (var req in syncMsg.ServerRequests) {
                requestManager.OnNext (Tuple.Create (req, syncMsg.State));
            }

            if (syncMsg.SyncTest != null) {
                syncMsg.SyncTest.Continuation (syncMsg.State, remoteObjects, enqueuedItems);
            }
        }

        async Task<bool> tryEmptyQueue (string authToken, List<CommonData> remoteObjects, bool isConnected)
        {
            string json = null;
            if (dataStore.TryPeek (QueueId, out json)) {
                if (isConnected) {
                    try {
                        do {
                            var jsonMsg = JsonConvert.DeserializeObject<DataJsonMsg> (json);
                            await SendMessage (authToken, remoteObjects, jsonMsg.LocalId, jsonMsg.Data);

                            // If we sent the message successfully, remove it from the queue
                            dataStore.TryDequeue (QueueId, out json);
                        } while (dataStore.TryPeek (QueueId, out json));
                        return true;
                    } catch (Exception ex) {
                        logError (ex);
                        return false;
                    }
                } else {
                    return false;
                }
            } else {
                return true;
            }
        }

        void Enqueue (Guid localId, CommonJson json, List<DataJsonMsg> enqueuedItems)
        {
            try {
                var jsonMsg = new DataJsonMsg (localId, json);
                var serialized = JsonConvert.SerializeObject (jsonMsg);
                dataStore.TryEnqueue (QueueId, serialized);
                enqueuedItems.Add (jsonMsg);
            } catch (Exception ex) {
                // TODO: Retry?
                logError (ex, "Failed to queue message");
            }
        }

        async Task SendMessage (string authToken, List<CommonData> remoteObjects, Guid localId, CommonJson json)
        {
            try {
                if (json.DeletedAt == null) {
                    CommonJson response;
                    if (json.RemoteId != null) {
                        response = await client.Update (authToken, json);
                    }
                    else {
                        response = await client.Create (authToken, json);
                    }
                    var resData = mapper.Map (response);
                    resData.Id = localId;
                    remoteObjects.Add (resData);
                }
                else {
                    if (json.RemoteId != null) {
                        await client.Delete (authToken, json);
                    }
                    else {
                        // TODO: Make sure the item has not been assigned a remoteId while waiting in the queue?
                    }
                }
            }
            catch {
                // TODO RX: Check the rejection reason: if an item is being specifically rejected,
                // discard it so it doesn't block the syncing of other items
                throw;
            }
        }

        async Task AuthenticateAsync (string username, string password)
        {
            logInfo (string.Format ("Authenticating with email ({0}).", username));
            await AuthenticateAsync (() => client.GetUser (username, password), Net.AuthChangeReason.Login);
        }

        async Task AuthenticateWithGoogleAsync (string accessToken)
        {
            logInfo ("Authenticating with Google access token.");
            await AuthenticateAsync (() => client.GetUser (accessToken), Net.AuthChangeReason.LoginGoogle);
        }

        async Task SignupAsync (string email, string password)
        {
            logInfo (string.Format ("Signing up with email ({0}).", email));
            await AuthenticateAsync (() => client.Create (string.Empty, new UserJson {
                Email = email,
                Password = password,
                Timezone = Time.TimeZoneId,
            }), Net.AuthChangeReason.Signup); //, AccountCredentials.Password);
        }

        async Task SignupWithGoogleAsync (string accessToken)
        {
            logInfo ("Signing up with email Google access token.");
            await AuthenticateAsync (() => client.Create (string.Empty, new UserJson () {
                GoogleAccessToken = accessToken,
                Timezone = Time.TimeZoneId,
            }), Net.AuthChangeReason.SignupGoogle); //, AccountCredentials.Google);
        }

        async Task AuthenticateAsync (
            Func<Task<UserJson>> getUser, Net.AuthChangeReason reason) //, AccountCredentials credentialsType)
        {
            UserJson userJson = null;
            var authResult = Net.AuthResult.Success;
            try {
                userJson = await getUser ();
                if (userJson == null) {
                    authResult = (reason == Net.AuthChangeReason.LoginGoogle) ? Net.AuthResult.NoGoogleAccount : Net.AuthResult.InvalidCredentials;
                } else if (userJson.DefaultWorkspaceRemoteId == 0) {
                    authResult = Net.AuthResult.NoDefaultWorkspace;
                }
            } catch (Exception ex) {
                var reqEx = ex as UnsuccessfulRequestException;
                if (reqEx != null && (reqEx.IsForbidden || reqEx.IsValidationError)) {
                    authResult = Net.AuthResult.InvalidCredentials;
                } else {
                    if (ex.IsNetworkFailure () || ex is TaskCanceledException) {
                        logInfo ("Failed authenticate user. Network error.", ex);
                        authResult = Net.AuthResult.NetworkError;
                    } else {
                        logWarning ("Failed to authenticate user. Unknown error.", ex);
                        authResult = Net.AuthResult.SystemError;
                    }
                }
            }

            // TODO RX: Ping analytics service
            //var tracker = ServiceContainer.Resolve<ITracker> ();
            //switch (reason) {
            //    case Net.AuthChangeReason.Login:
            //        tracker.SendAccountLoginEvent (credentialsType);
            //    break;
            //    case Net.AuthChangeReason.Signup:
            //        tracker.SendAccountCreateEvent (credentialsType);
            //    break;
            //}

            RxChain.Send (new DataMsg.UserDataPut (
                              authResult, userJson != null ? mapper.Map<UserData> (userJson) : null));
        }

        async Task FullSync (AppState state)
        {
            string authToken = state.User.ApiToken;
            DateTime? sinceDate = state.FullSyncResult.SyncLastRun;
            // If Since value is less than two months
            // Use null and let server pick the correct value
            if (sinceDate < DateTime.Now.Date.AddMonths (-2)) {
                sinceDate = null;
            }

            try {
                var changes = await client.GetChanges (authToken, sinceDate);
                var jsonEntries = changes.TimeEntries.ToList ();
                var newWorkspaces = changes.Workspaces.ToList ();
                var newProjects = changes.Projects.ToList ();
                var newClients = changes.Clients.ToList ();
                var newTasks = changes.Tasks.ToList ();
                var newTags = changes.Tags.ToList ();
                var fullSyncInfo = Tuple.Create (mapper.Map<UserData> (changes.User), changes.Timestamp);

                RxChain.Send (new DataMsg.ReceivedFromSync (
                                  newWorkspaces.Select (mapper.Map<WorkspaceData>).Cast<CommonData> ()
                                  .Concat (newTags.Select (mapper.Map<TagData>).Cast<CommonData> ())
                                  .Concat (newClients.Select (mapper.Map<ClientData>).Cast<CommonData> ())
                                  .Concat (newProjects.Select (mapper.Map<ProjectData>).Cast<CommonData> ())
                                  .Concat (newTasks.Select (mapper.Map<TaskData>).Cast<CommonData> ())
                                  .Concat (jsonEntries.Select (x => MapEntryWithTags (x, state))).ToList (),
                                  fullSyncInfo));
            } catch (Exception exc) {
                string errorMsg = string.Format ("Failed to sync data since {0}", state.FullSyncResult.SyncLastRun);

                if (exc.IsNetworkFailure () || exc is TaskCanceledException) {
                    logInfo (errorMsg, exc);
                } else {
                    logWarning (errorMsg, exc);
                }

                RxChain.Send (new DataMsg.ReceivedFromSync (exc));
            }
        }

        async Task DownloadEntries (AppState state)
        {
            long? clientRemoteId = null;
            string authToken = state.User.ApiToken;
            var startDate = state.DownloadResult.DownloadFrom;
            const int endDate = Literals.TimeEntryLoadDays;

            try {
				// Download new Entries
				var jsonEntries = await client.ListTimeEntries (authToken, startDate, endDate);
				
                var newWorkspaces = new List<WorkspaceJson> ();
                var newProjects = new List<ProjectJson> ();
                var newClients = new List<ClientJson> ();
                var newTasks = new List<TaskJson> ();
                var newTags = new List<TagData> ();

                // Check the state contains all related objects
                foreach (var entry in jsonEntries) {
                    if (state.Workspaces.Values.All (x => x.RemoteId != entry.WorkspaceRemoteId) &&
                            newWorkspaces.All (x => x.RemoteId != entry.WorkspaceRemoteId)) {
                        newWorkspaces.Add (await client.Get<WorkspaceJson> (authToken, entry.WorkspaceRemoteId));
                    }

                    if (entry.ProjectRemoteId.HasValue) {
                        var projectData = state.Projects.Values.FirstOrDefault (
                                              x => x.RemoteId == entry.ProjectRemoteId);

                        if (projectData != null) {
                            clientRemoteId = projectData.ClientRemoteId;
                        } else {
                            var projectJson = newProjects.FirstOrDefault (x => x.RemoteId == entry.ProjectRemoteId);
                            if (projectJson == null) {
                                projectJson = await client.Get<ProjectJson> (authToken, entry.ProjectRemoteId.Value);
                                newProjects.Add (projectJson);
                            }
                            clientRemoteId = (projectJson as ProjectJson).ClientRemoteId;
                        }

                        if (clientRemoteId.HasValue) {
                            if (state.Clients.Values.All (x => x.RemoteId != clientRemoteId) &&
                                    newClients.All (x => x.RemoteId != clientRemoteId)) {
                                newClients.Add (await client.Get<ClientJson> (authToken, clientRemoteId.Value));
                            }
                        }
                    }

                    if (entry.TaskRemoteId.HasValue) {
                        if (state.Tasks.Values.All (x => x.RemoteId != entry.TaskRemoteId) &&
                                newTasks.All (x => x.RemoteId != entry.TaskRemoteId)) {
                            newTasks.Add (await client.Get<TaskJson> (authToken, entry.TaskRemoteId.Value));
                        }
                    }
                }

                // ATTENTION: Order is important, containers must come first
                // E.g. projects come after client, because projects contain a reference to ClientId
                RxChain.Send (new DataMsg.ReceivedFromDownload (
                                  newWorkspaces.Select (mapper.Map<WorkspaceData>).Cast<CommonData> ()
                                  .Concat (newTags.Select (mapper.Map<TagData>).Cast<CommonData> ())
                                  .Concat (newClients.Select (mapper.Map<ClientData>).Cast<CommonData> ())
                                  .Concat (newProjects.Select (mapper.Map<ProjectData>).Cast<CommonData> ())
                                  .Concat (newTasks.Select (mapper.Map<TaskData>).Cast<CommonData> ())
                                  .Concat (jsonEntries.Select (x => MapEntryWithTags (x, state)))
                                  .ToList ()));

            } catch (Exception exc) {
                string errorMsg = string.Format (
                                      "Failed to fetch time entries {1} days up to {0}",
                                      startDate, endDate);

                if (exc.IsNetworkFailure () || exc is TaskCanceledException) {
                    logInfo (errorMsg, exc);
                } else {
                    logWarning (errorMsg, exc);
                }

                RxChain.Send (new DataMsg.ReceivedFromDownload (exc));
            }
        }

        CommonData MapEntryWithTags (TimeEntryJson jsonEntry, AppState state)
        {
            var tagIds = new List<Guid> ();
            foreach (var tag in jsonEntry.Tags) {
                var tagData = state.Tags.Values.SingleOrDefault (
                    x => x.WorkspaceRemoteId == jsonEntry.WorkspaceRemoteId && x.Name == tag);
                if (tagData != null) {
                    tagIds.Add (tagData.Id);
                }
                else {
                    // TODO RX: How to retrieve the tag from server without RemoteId?
                    //newTags.Add (await client.Get<TagJson> (authToken, tagRemoteId));
                }
            }

            var te = mapper.Map<TimeEntryData> (jsonEntry);
            te.TagIds = tagIds;
            return te;
        }

        long? getRemoteId (Guid localId, List<CommonData> remoteObjects, ICommonData stateObject)
        {
            if (stateObject.RemoteId != null) {
                return stateObject.RemoteId;
            }
            else {
                var d = remoteObjects.SingleOrDefault (x => x.Id == localId);
                return d != null ? d.RemoteId : null;
            }
        }

        bool BuildRemoteRelationships (ref ICommonData data, List<CommonData> remoteObjects, AppState state)
        {
            if (data is TimeEntryData) {
                var te = (TimeEntryData)data;
				if (te.UserRemoteId == 0) {
					if (state.User.RemoteId != null)
						te.UserRemoteId = state.User.RemoteId.Value;
					else
						return false;
				}
                if (te.WorkspaceRemoteId == 0) {
                    var rid = getRemoteId (te.WorkspaceId, remoteObjects, state.Workspaces[te.WorkspaceId]);
                    if (rid != null)
                        te.WorkspaceRemoteId = rid.Value;
                    else
                        return false;
                }
                if (te.ProjectId != Guid.Empty && te.ProjectRemoteId == null) {
                    var rid = getRemoteId (te.ProjectId, remoteObjects, state.Projects[te.ProjectId]);
                    if (rid != null)
                        te.ProjectRemoteId = rid;
                    else
                        return false;
                }
                if (te.TaskId != Guid.Empty && te.TaskRemoteId == null) {
                    var rid = getRemoteId (te.TaskId, remoteObjects, state.Tasks[te.TaskId]);
                    if (rid != null)
                        te.TaskRemoteId = rid;
                    else
                        return false;
                }
            }
            else if (data is ProjectData) {
                var pr = (ProjectData)data;
                if (pr.WorkspaceRemoteId == 0) {
                    var rid = getRemoteId (pr.WorkspaceId, remoteObjects, state.Workspaces[pr.WorkspaceId]);
                    if (rid != null)
                        pr.WorkspaceRemoteId = rid.Value;
                    else
                        return false;
                }
                if (pr.ClientId != Guid.Empty && pr.ClientRemoteId == null) {
                    var rid = getRemoteId (pr.ClientId, remoteObjects, state.Clients[pr.ClientId]);
                    if (rid != null)
                        pr.ClientRemoteId = rid;
                    else
                        return false;
                }
            }
            else if (data is ClientData) {
                var cl = (ClientData)data;
                if (cl.WorkspaceRemoteId == 0) {
                    var rid = getRemoteId (cl.WorkspaceId, remoteObjects, state.Workspaces[cl.WorkspaceId]);
                    if (rid != null)
                        cl.WorkspaceRemoteId = rid.Value;
                    else
                        return false;
                }
            }
            else if (data is TaskData) {
                var ts = (TaskData)data;
                if (ts.WorkspaceRemoteId == 0) {
                    var rid = getRemoteId (ts.WorkspaceId, remoteObjects, state.Workspaces[ts.WorkspaceId]);
                    if (rid != null)
                        ts.WorkspaceRemoteId = rid.Value;
                    else
                        return false;
                }
                if (ts.ProjectRemoteId == 0) {
                    var rid = getRemoteId (ts.ProjectId, remoteObjects, state.Projects[ts.ProjectId]);
                    if (rid != null)
                        ts.ProjectRemoteId = rid.Value;
                    else
                        return false;
                }
            }
            else if (data is TagData) {
                var t = (TagData)data;
                if (t.WorkspaceRemoteId == 0) {
                    var rid = getRemoteId (t.WorkspaceId, remoteObjects, state.Workspaces[t.WorkspaceId]);
                    if (rid != null)
                        t.WorkspaceRemoteId = rid.Value;
                    else
                        return false;
                }
            }
            else if (data is UserData) {
                //var u = (UserData)data;
                // TODO RX: How to get DefaultWorkspaceRemoteId
            }
            else {
                // TODO RX: Throw exception? Return false?
            }
            return true;
        }
    }
}
