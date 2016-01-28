using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Toggl.Phoebe.Data;
using Toggl.Phoebe.Data.Json;
using Toggl.Phoebe.Data.Json.Converters;
using Toggl.Phoebe.Logging;
using Toggl.Phoebe.Net;
using XPlatUtils;

namespace Toggl.Phoebe.Sync
{
    public class SyncOutManager
    {
        public static SyncOutManager Singleton { get; private set; }

        public static void Init ()
        {
            Store.Init ();
            Singleton = new SyncOutManager ();
        }

        readonly IDataStore dataStore = ServiceContainer.Resolve<IDataStore> ();
        readonly ITogglClient client = ServiceContainer.Resolve<ITogglClient> ();

        SyncOutManager ()
        {   
        }

        void HandleMsg (DataMsg<DataSyncMsg> msg)
        {
            msg.Data.Match (
                x => { if (x.Dir == DataDir.Outcoming) { EnqueueOrSend (new [] { x }); } },
                e => {}  // TODO: Error handling
            );
        }

        void HandleGroupMsg (DataMsg<IDataSyncGroup> msg)
        {
            msg.Data.Match (
                x => EnqueueOrSend (x.SyncMessages.Where (y => y.Dir == DataDir.Outcoming).ToList ()),
                e => {}  // TODO: Error handling
            );
        }

        void EnqueueOrSend (IList<DataSyncMsg> msgs)
        {
            // TODO: Check internet connection
            // TODO: Check queue size, if it reaches a limit, empty it and request full sync next time

            dataStore.ExecuteInTransactionWithMessagesAsync (async ctx => {
                foreach (var msg in msgs) {
                    bool alreadyQueued = false;
                    var exported = msg.Data.Export (ctx);

                    try {
                        string json = null;
                        ctx.Enqueue (JsonConvert.SerializeObject (new DataJsonMsg (msg.Verb, exported)));
                        alreadyQueued = true;
                        
                        if (ctx.TryDequeue (out json)) {
                            // Send queue to server
                            do {
                                var jsonMsg = JsonConvert.DeserializeObject<DataJsonMsg> (json);
                                await SendMessage (jsonMsg.Verb, jsonMsg.Data);

                                // If we sent the message successfully, remove it from the queue
                                ctx.TryDequeue (out json);
                            } while (ctx.TryPeekQueue (out json));
                        }
                        else {
                            // If there's no queue, try to send the message directly
                            await SendMessage (msg.Verb, exported);
                        }
                    }
                    catch (Exception ex) {
                        if (!alreadyQueued) {
                            ctx.Enqueue (JsonConvert.SerializeObject (new DataJsonMsg (msg.Verb, exported)));
                        }
                        var log = ServiceContainer.Resolve<ILogger> ();
                        log.Error (typeof(SyncOutManager).Name, ex, "Failed to send data to server");
                    }
                }
            });
        }

        // TODO: Check client methods results -> Assign ID for newly created json
        async Task SendMessage (DataVerb action, CommonJson json)
        {
            switch (action) {
            case DataVerb.Post:
                await client.Create (json);
                break;
            case DataVerb.Put:
                await client.Update (json);
                break;
            case DataVerb.Delete:
                await client.Delete (json);
                break;
            }
        }
    }
}
