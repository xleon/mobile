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
    public class SyncOutManager
    {
        public const string QueueId = "SYNC_OUT";
        public static SyncOutManager Singleton { get; private set; }

        public static void Init ()
        {
            Singleton = Singleton ?? new SyncOutManager ();
        }

        public static void Cleanup ()
        {
            Singleton = null;
        }

		readonly string Tag = typeof (SyncOutManager).Name;
        readonly JsonMapper mapper;
        readonly Toggl.Phoebe.Net.INetworkPresence networkPresence;
        readonly ISyncDataStore dataStore;
        readonly ITogglClient client;
        readonly Subject<Tuple<ServerRequest, AppState>> requestManager = new Subject<Tuple<ServerRequest, AppState>> ();


        SyncOutManager ()
        {
            mapper = new JsonMapper ();
            networkPresence = ServiceContainer.Resolve<Toggl.Phoebe.Net.INetworkPresence> ();
            dataStore = ServiceContainer.Resolve<ISyncDataStore> ();
            client = ServiceContainer.Resolve<ITogglClient> ();

            StoreManager.Singleton
            .Observe ()
            .SelectAsync (EnqueueOrSend)
            .Subscribe ();

            requestManager
                // Make sure requests are run one after the other
                .Synchronize ()
                .SelectAsync (async x => {
                    if (x.Item1 is ServerRequest.DownloadEntries) {
                        await DownloadEntries (x.Item2.TimerState);
                    }
                    else if (x.Item1 is ServerRequest.Authenticate) {
                        var req = x.Item1 as ServerRequest.Authenticate;
                        await AuthenticateAsync (req.Username, req.Password);
                    }
                    else if (x.Item1 is ServerRequest.AuthenticateWithGoogle) {
                        var req = x.Item1 as ServerRequest.AuthenticateWithGoogle;
                        await AuthenticateWithGoogleAsync (req.AccessToken);
                    }
                    else if (x.Item1 is ServerRequest.SignUp) {
                        var req = x.Item1 as ServerRequest.SignUp;
                        await SignupAsync (req.Email, req.Password);
                    }
                    else if (x.Item1 is ServerRequest.SignUpWithGoogle) {
                        var req = x.Item1 as ServerRequest.SignUpWithGoogle;
                        await SignupWithGoogleAsync (req.AccessToken);
                    }
                });
        }

        void log (Exception ex, string msg = "Failed to send data to server")
        {
            var logger = ServiceContainer.Resolve<ILogger> ();
            logger.Error (Tag, ex, msg);
        }

        async Task EnqueueOrSend (DataSyncMsg<AppState> syncMsg)
        {
            var remoteObjects = new List<CommonData> ();
            var enqueuedItems = new List<DataJsonMsg> ();
            var isConnected = syncMsg.SyncTest != null
                ? syncMsg.SyncTest.IsConnectionAvailable
                : networkPresence.IsNetworkPresent;

            // Try to empty queue first
            bool queueEmpty = await tryEmptyQueue (remoteObjects, isConnected);

            // Deal with messages
            foreach (var msg in syncMsg.SyncData) {
                var exported = mapper.MapToJson (msg);

                if (queueEmpty && isConnected) {
                    try {
                        await SendMessage (remoteObjects, msg.Id, exported);
                    } catch (Exception ex) {
                        log (ex);
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
                RxChain.Send (new DataMsg.ReceivedFromServer (remoteObjects));
            }

            if (isConnected) {
                // TODO: Discard duplicated requests?
                foreach (var req in syncMsg.ServerRequests) {
                    requestManager.OnNext (Tuple.Create (req, syncMsg.State));
                }
            }

            if (syncMsg.SyncTest != null) {
                syncMsg.SyncTest.Continuation (syncMsg.State, remoteObjects, enqueuedItems);
            }
        }

        async Task<bool> tryEmptyQueue (List<CommonData> remoteObjects, bool isConnected)
        {
            string json = null;
            if (dataStore.TryPeek (QueueId, out json)) {
                if (isConnected) {
                    try {
                        do {
                            var jsonMsg = JsonConvert.DeserializeObject<DataJsonMsg> (json);
                            await SendMessage (remoteObjects, jsonMsg.LocalId, jsonMsg.Data);

                            // If we sent the message successfully, remove it from the queue
                            dataStore.TryDequeue (QueueId, out json);
                        } while (dataStore.TryPeek (QueueId, out json));
                        return true;
                    } catch (Exception ex) {
                        log (ex);
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
                log (ex, "Failed to queue message");
            }
        }

        async Task SendMessage (List<CommonData> remoteObjects, Guid localId, CommonJson json)
        {
            if (json.DeletedAt == null) {
                if (json.RemoteId != null) {
                    // TODO: Save the response to remoteObjects here too?
                    await client.Update (json);
                } else {
                    var res = await client.Create (json);
                    var resData = mapper.Map (res);
                    resData.Id = localId;
                    remoteObjects.Add (resData);
                }
            } else {
                if (json.RemoteId != null) {
                    await client.Delete (json);
                } else {
                    // TODO: Make sure the item has not been assigned a remoteId by a previous item in the queue
                }
            }
        }

        async Task AuthenticateAsync (string username, string password)
        {
            var log = ServiceContainer.Resolve<ILogger> ();
            var client = ServiceContainer.Resolve<ITogglClient> ();

            log.Info (Tag, "Authenticating with email ({0}).", username);
            await AuthenticateAsync (() => client.GetUser (username, password), Net.AuthChangeReason.Login); //, AccountCredentials.Password);
        }

        async Task AuthenticateWithGoogleAsync (string accessToken)
        {
            var log = ServiceContainer.Resolve<ILogger> ();
            var client = ServiceContainer.Resolve<ITogglClient> ();

            log.Info (Tag, "Authenticating with Google access token.");
            await AuthenticateAsync (() => client.GetUser (accessToken), Net.AuthChangeReason.Login); //, AccountCredentials.Google);
        }

        async Task SignupAsync (string email, string password)
        {
            var log = ServiceContainer.Resolve<ILogger> ();
            var client = ServiceContainer.Resolve<ITogglClient> ();

            log.Info (Tag, "Signing up with email ({0}).", email);
            await AuthenticateAsync (() => client.Create (new UserJson {
                Email = email,
                Password = password,
                Timezone = Time.TimeZoneId,
            }), Net.AuthChangeReason.Signup); //, AccountCredentials.Password);
        }

        async Task SignupWithGoogleAsync (string accessToken)
        {
            var log = ServiceContainer.Resolve<ILogger> ();
            var client = ServiceContainer.Resolve<ITogglClient> ();

            log.Info (Tag, "Signing up with email Google access token.");
            await AuthenticateAsync (() => client.Create (new UserJson () {
                GoogleAccessToken = accessToken,
                Timezone = Time.TimeZoneId,
            }), Net.AuthChangeReason.Signup); //, AccountCredentials.Google);
        }

        async Task AuthenticateAsync (
            Func<Task<UserJson>> getUser, Net.AuthChangeReason reason) //, AccountCredentials credentialsType)
        {
            UserJson userJson = null;
            var authResult = Net.AuthResult.Success;
            try {
                userJson = await getUser ();
                if (userJson == null) {
                    authResult = Net.AuthResult.InvalidCredentials;
                } else if (userJson.DefaultWorkspaceRemoteId == 0) {
                    authResult = Net.AuthResult.NoDefaultWorkspace;
                }
            } catch (Exception ex) {
                var reqEx = ex as UnsuccessfulRequestException;
                if (reqEx != null && (reqEx.IsForbidden || reqEx.IsValidationError)) {
                    authResult = Net.AuthResult.InvalidCredentials;
                }

                var log = ServiceContainer.Resolve<ILogger> ();
                if (ex.IsNetworkFailure () || ex is TaskCanceledException) {
                    log.Info (Tag, ex, "Failed authenticate user.");
                } else {
                    log.Warning (Tag, ex, "Failed to authenticate user.");
                }

                authResult = Net.AuthResult.NetworkError;
            }

            // TODO RX
            // Ping analytics service
            //var tracker = ServiceContainer.Resolve<ITracker> ();
            //switch (reason) {
            //    case Net.AuthChangeReason.Login:
            //        tracker.SendAccountLoginEvent (credentialsType);
            //    break;
            //    case Net.AuthChangeReason.Signup:
            //        tracker.SendAccountCreateEvent (credentialsType);
            //    break;
            //}

            var userData = userJson != null ? mapper.Map<UserData> (userJson) : null;
            RxChain.Send (new DataMsg.UserDataPut (authResult, userData));
        }

        async Task DownloadEntries (TimerState state)
        {
            var startDate = state.DownloadResult.DownloadFrom;
            const int endDate = Literals.TimeEntryLoadDays;

            try {
                var jsonEntries = await client.ListTimeEntries (startDate, endDate);
                // Download new Entries

                var newWorkspaces = new List<CommonJson> ();
                var newProjects = new List<CommonJson> ();
                var newClients = new List<CommonJson> ();
                var newTasks = new List<CommonJson> ();
                var newTags = new List<TagJson> ();

                // Check the state contains all related objects
                foreach (var entry in jsonEntries) {
                    if (state.Workspaces.Values.All (x => x.RemoteId != entry.WorkspaceRemoteId) &&
                            newWorkspaces.All (x => x.RemoteId != entry.WorkspaceRemoteId)) {
                        newWorkspaces.Add (await client.Get<WorkspaceJson> (entry.WorkspaceRemoteId));
                    }

                    if (entry.ProjectRemoteId.HasValue) {
                        long? clientRemoteId = null;
                        var projectData = state.Projects.Values.FirstOrDefault (x => x.RemoteId == entry.ProjectRemoteId);

                        if (projectData != null) {
                            clientRemoteId = projectData.ClientRemoteId;
                        } else {
                            var projectJson = newProjects.FirstOrDefault (x => x.RemoteId == entry.ProjectRemoteId);
                            if (projectJson == null) {
                                projectJson = await client.Get<ProjectJson> (entry.ProjectRemoteId.Value);
                                newProjects.Add (projectJson);
                            }
                            clientRemoteId = projectJson.RemoteId;
                        }

                        if (state.Clients.Values.All (x => x.RemoteId != clientRemoteId) &&
                                newClients.All (x => x.RemoteId != clientRemoteId)) {
                            newClients.Add (await client.Get<ClientJson> (clientRemoteId.Value));
                        }
                    }

                    if (entry.TaskRemoteId.HasValue) {
                        if (state.Tasks.Values.All (x => x.RemoteId != entry.TaskRemoteId) &&
                                newTasks.All (x => x.RemoteId != entry.TaskRemoteId)) {
                            newTasks.Add (await client.Get<TaskJson> (entry.TaskRemoteId.Value));
                        }
                    }

                    foreach (var tag in entry.Tags) {
                        if (state.Tags.Values.All (x => x.WorkspaceRemoteId != entry.WorkspaceRemoteId || x.Name != tag) &&
                            newTags.All (x => x.WorkspaceRemoteId != entry.WorkspaceRemoteId || x.Name != tag)) {
                            // TODO: How to get the tag without a remote id?
                            //newTags.Add (await client.Get<TagJson> (tagRemoteId));
							throw new NotImplementedException ();
                        }
                    }
                }

                RxChain.Send (new DataMsg.ReceivedFromServer (
                                  jsonEntries.Select (mapper.Map<TimeEntryData>).Cast<CommonData> ()
                                  .Concat (newWorkspaces.Select (mapper.Map<WorkspaceData>).Cast<CommonData> ())
                                  .Concat (newProjects.Select (mapper.Map<ProjectData>).Cast<CommonData> ())
                                  .Concat (newClients.Select (mapper.Map<ClientData>).Cast<CommonData> ())
                                  .Concat (newTasks.Select (mapper.Map<TaskData>).Cast<CommonData> ())
                                  .Concat (newTags.Select (mapper.Map<TagData>).Cast<CommonData> ())
                                  .ToList ()));

            } catch (Exception exc) {
                var tag = this.GetType ().Name;
                var logger = ServiceContainer.Resolve<ILogger> ();
                string errorMsg = string.Format (
                                      "Failed to fetch time entries {1} days up to {0}",
                                      startDate, endDate);

                if (exc.IsNetworkFailure () || exc is TaskCanceledException) {
                    logger.Info (tag, exc, errorMsg);
                } else {
                    logger.Warning (tag, exc, errorMsg);
                }

                RxChain.Send (new DataMsg.ReceivedFromServer (exc));
            }
        }
    }
}
