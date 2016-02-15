using System;
using System.Linq;
using System.Collections.Generic;
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

        readonly JsonMapper mapper =
            new JsonMapper ();

        readonly Toggl.Phoebe.Net.INetworkPresence networkPresence =
            ServiceContainer.Resolve<Toggl.Phoebe.Net.INetworkPresence> ();

        readonly ISyncDataStore dataStore =
            ServiceContainer.Resolve<ISyncDataStore> ();

        readonly ITogglClient client =
            ServiceContainer.Resolve<ITogglClient> ();

        #if DEBUG
        readonly System.Reactive.Subjects.Subject<System.Reactive.Unit> subject =
            new System.Reactive.Subjects.Subject<System.Reactive.Unit> ();

        public IObservable<System.Reactive.Unit> Observable {
            get { return subject; }
        }       
        #endif

        SyncOutManager ()
        {
            StoreManager.Singleton
            .Observe ()
            .SelectAsync (EnqueueOrSend)
            #if DEBUG
            .Subscribe (subject.OnNext);
            #endif
            #if RELEASE
            .Subscribe ();
            #endif
        }

        void log (Exception ex)
        {
            var log = ServiceContainer.Resolve<ILogger> ();
            log.Error (typeof (SyncOutManager).Name, ex, "Failed to send data to server");
        }

        async Task EnqueueOrSend (DataSyncMsg<AppState> syncMsg)
        {
            // Check internet connection
            var isConnected = networkPresence.IsNetworkPresent;
            var remoteIds = new List<CommonData> ();

            // Try to empty queue first
            bool queueEmpty = await tryEmptyQueue (remoteIds, isConnected);

            // Deal with messages
            foreach (var msg in syncMsg.SyncData) {
                bool alreadyQueued = false;
                var exported = mapper.MapToJson (msg);

                if (queueEmpty && isConnected) {
                    try {
                        await SendMessage (remoteIds, exported);
                    } catch (Exception ex) {
                        log (ex);
                        Enqueue (exported);
                        queueEmpty = false;
                    }
                } else {
                    Enqueue (exported);
                    queueEmpty = false;
                }
            }

            // TODO: Try to empty queue again?

            // Return assigned remoteIds
            if (remoteIds.Count > 0) {
                RxChain.Send (new DataMsg.ReceivedFromServer (remoteIds));
            }

            if (syncMsg.IsSyncRequested) {
                DownloadEntries (syncMsg.State.TimerState);
            }
        }

        async Task<bool> tryEmptyQueue (List<CommonData> remoteIds, bool isConnected)
        {
            string json = null;
            if (dataStore.TryPeek (QueueId, out json)) {
                if (isConnected) {
                    try {
                        do {
                            var jsonMsg = JsonConvert.DeserializeObject<DataJsonMsg> (json);
                            await SendMessage (remoteIds, jsonMsg.Data);

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

        void Enqueue (CommonJson json)
        {
            try {
                var serialized = JsonConvert.SerializeObject (new DataJsonMsg (json));
                dataStore.TryEnqueue (QueueId, serialized);
            } catch (Exception ex) {
                // TODO: Retry?
                var log = ServiceContainer.Resolve<ILogger> ();
                log.Error (typeof (SyncOutManager).Name, ex, "Failed to queue message");
            }
        }

        async Task SendMessage (List<CommonData> remoteIds, CommonJson json)
        {
            if (json.DeletedAt == null) {
                if (json.RemoteId != null) {
                    await client.Update (json);
                } else {
                    var res = await client.Create (json);
                    remoteIds.Add (mapper.Map (res));
                }
            } else {
                if (json.RemoteId != null) {
                    await client.Delete (json);
                } else {
                    // TODO: Make sure the item has not been assigned a remoteId by a previous item in the queue
                }
            }
        }

        async void DownloadEntries (TimerState state)
        {
            var startDate = state.DownloadInfo.DownloadFrom;
            var endDate = Literals.TimeEntryLoadDays;

            try {
                var jsonEntries = await client.ListTimeEntries (startDate, endDate);
                // Download new Entries

                var newWorkspaces = new List<CommonJson> ();
                var newProjects = new List<CommonJson> ();
                var newClients = new List<CommonJson> ();
                var newTasks = new List<CommonJson> ();

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
                            newTasks.Add (await client.Get<ClientJson> (entry.TaskRemoteId.Value));
                        }
                    }

                    // TODO: Tags
                }

                RxChain.Send (new DataMsg.ReceivedFromServer (
                                  jsonEntries.Select (mapper.Map<TimeEntryData>).Cast<CommonData> ()
                                  .Concat (newWorkspaces.Select (mapper.Map<WorkspaceData>).Cast<CommonData> ())
                                  .Concat (newProjects.Select (mapper.Map<ProjectData>).Cast<CommonData> ())
                                  .Concat (newClients.Select (mapper.Map<ClientData>).Cast<CommonData> ())
                                  .Concat (newTasks.Select (mapper.Map<TaskData>).Cast<CommonData> ()).ToList ()));

            } catch (Exception exc) {
                var tag = this.GetType ().Name;
                var log = ServiceContainer.Resolve<ILogger> ();
                string errorMsg = string.Format (
                                      "Failed to fetch time entries {1} days up to {0}",
                                      startDate, endDate);

                if (exc.IsNetworkFailure () || exc is TaskCanceledException) {
                    log.Info (tag, exc, errorMsg);
                } else {
                    log.Warning (tag, exc, errorMsg);
                }

                RxChain.Send (new DataMsg.ReceivedFromServer (exc));
            }
        }
    }
}
