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
    public static class SyncOutManager
    {
        static readonly IDataStore dataStore = ServiceContainer.Resolve<IDataStore> ();
        static readonly ITogglClient client = ServiceContainer.Resolve<ITogglClient> ();

        static SyncOutManager ()
        {   
            Store.Observe <DataSyncMsg> ().Subscribe (HandleMsg);
            Store.Observe <IDataSyncGroup> ().Subscribe (HandleGroupMsg);
        }

        static async void HandleMsg (DataMsg<DataSyncMsg> msg)
        {
            await msg.Data.MatchAsync (
                async x => {
                    if (x.Dir == DataDir.Outcoming)
                        await EnqueueOrSend (new [] { x });
                },
                e => {}  // TODO: Error handling
            );
        }

        static async void HandleGroupMsg (DataMsg<IDataSyncGroup> msg)
        {
            await msg.Data.MatchAsync (
                x => EnqueueOrSend (x.SyncMessages.Where (y => y.Dir == DataDir.Outcoming).ToList ()),
                e => {}  // TODO: Error handling
            );
        }

        static async Task EnqueueOrSend (IList<DataSyncMsg> msgs)
        {
            // TODO: Check internet connection
            // TODO: Check queue size, if it reaches a limit, empty it and request full sync next time

            await dataStore.ExecuteInTransactionWithMessagesAsync (async ctx => {
                foreach (var msg in msgs) {
                    var exported = msg.Data.Export (ctx);

                    string json = null;
                    DataJsonMsg jsonMsg = null;

                    bool alreadyQueued = false;
                    try {
                        if (ctx.TryDequeue (out json)) {
                            ctx.Enqueue (JsonConvert.SerializeObject (new DataJsonMsg (msg.Verb, exported)));
                            alreadyQueued = true;

                            // Send queue to server
                            while (ctx.TryPeekQueue (out json)) {
                                jsonMsg = JsonConvert.DeserializeObject<DataJsonMsg> (json);
                                await SendMessage (jsonMsg.Verb, jsonMsg.Data);

                                // If we sent the message successfully, remove it from the queue
                                ctx.TryDequeue (out json);
                            }
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

        // TODO: Check client methods results
        static async Task SendMessage (DataVerb action, CommonJson json)
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
