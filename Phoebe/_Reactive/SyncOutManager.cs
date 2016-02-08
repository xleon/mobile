using System;
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

        SyncOutManager ()
        {
            StoreManager.Singleton
                .Observe ()
                .SelectAsync (msg => msg.RawData.Match (
                    x => HandleMessage(x),
                    e => Task.Run(() => {}))) // Do nothing
                .Subscribe ();
        }

        async Task HandleMessage (object msg)
        {
            var syncMsg = msg as IDataSyncMsg;
            if (syncMsg != null && syncMsg.Dir == DataDir.Outcoming &&
                syncMsg.Data.Count > 0) {
                await EnqueueOrSend (syncMsg.Data);
            }
        }

        async Task EnqueueOrSend (IReadOnlyList<CommonData> msgs)
        {
            // TODO: Limit queue size?

            // Check internet connection
            var isConnected = networkPresence.IsNetworkPresent;

            foreach (var msg in msgs) {
                bool alreadyQueued = false;
                var exported = mapper.MapToJson (msg);

                // If there's no connection, just enqueue the message
                if (!isConnected) {
                    Enqueue (exported);
                    continue;
                }

                try {
                    string json = null;
                    if (dataStore.TryPeek (QueueId, out json)) {
                        Enqueue (exported);
                        alreadyQueued = true;
                        
                        // Send dataStore to server
                        do {
                            var jsonMsg = JsonConvert.DeserializeObject<DataJsonMsg> (json);
                            await SendMessage (jsonMsg.Data);

                            // If we sent the message successfully, remove it from the dataStore
                            dataStore.TryDequeue (QueueId, out json);
                        } while (dataStore.TryPeek (QueueId, out json));
                    }
                    else {
                        // If there's no queue, try to send the message directly
                        await SendMessage (exported);
                    }
                }
                catch (Exception ex) {
                    if (!alreadyQueued) {
                        Enqueue (exported);
                    }
                    var log = ServiceContainer.Resolve<ILogger> ();
                    log.Error (typeof(SyncOutManager).Name, ex, "Failed to send data to server");
                }
            }
        }

        void Enqueue (CommonJson json)
        {
            try {
                var serialized = JsonConvert.SerializeObject (new DataJsonMsg (json));
                dataStore.TryEnqueue (QueueId, serialized);
            }
            catch (Exception ex) {
                // TODO: Retry?
                var log = ServiceContainer.Resolve<ILogger> ();
                log.Error (typeof(SyncOutManager).Name, ex, "Failed to queue message");
            }
        }

        async Task SendMessage (CommonJson json)
        {
            if (json.DeletedAt == null) {
                if (json.RemoteId != null) {
                    await client.Update (json);
                }
                else {
                    var res = await client.Create (json);
                    // TODO: Store RemoteId
                }
            }
            else {
                // If the entry wasn't synced with the server we don't need to notify
                // (the entry was already removed in the view at the start of the undo timeout)
                if (json.RemoteId != null) {
                    await client.Delete (json);
                }
            }
        }

        async void DownloadEntries(DateTime startFrom)
        {
            const int daysLoad = Literals.TimeEntryLoadDays;
            try {
                // Download new Entries
                var jsonEntries = await client.ListTimeEntries (startFrom, daysLoad);

                RxChain.Send (this.GetType (), DataTag.TimeEntryReceivedFromServer, jsonEntries);
            }
            catch (Exception exc) {
                var tag = typeof(StoreRegister).Name;
                var log = ServiceContainer.Resolve<ILogger> ();
                const string errorMsg = "Failed to fetch time entries {1} days up to {0}";

                if (exc.IsNetworkFailure () || exc is TaskCanceledException) {
                    log.Info (tag, exc, errorMsg, startFrom, daysLoad);
                }
                else {
                    log.Warning (tag, exc, errorMsg, startFrom, daysLoad);
                }

                RxChain.SendError<List<TimeEntryJson>> (this.GetType (), DataTag.TimeEntryReceivedFromServer, exc);
            }
        }
    }
}
