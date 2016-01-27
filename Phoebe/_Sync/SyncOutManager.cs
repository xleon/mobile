using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Toggl.Phoebe.Data;
using Toggl.Phoebe.Data.DataObjects;
using Toggl.Phoebe.Data.Json.Converters;
using Toggl.Phoebe.Logging;
using Toggl.Phoebe.Net;
using XPlatUtils;
using Toggl.Phoebe.Data.Json;
using Newtonsoft.Json;

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
                    var msgJson = new DataJsonMsg (msg, ctx);

                    string json = null;
                    bool alreadyQueued = false;
                    try {
                        if (ctx.TryDequeue (out json)) {
                            ctx.Enqueue (JsonConvert.SerializeObject (msgJson));
                            alreadyQueued = true;

                            // Send queue to server
                            while (ctx.TryPeekQueue (out json)) {
                                msgJson = JsonConvert.DeserializeObject<DataJsonMsg> (json);
                                await SendMessage (msgJson.Verb, msgJson.Data);

                                // If we sent the message successfully, remove it from the queue
                                ctx.TryDequeue (out json);
                            }
                        }
                        else {
                            // If there's no queue, try to send the message directly
                            await SendMessage (msgJson.Verb, msgJson.Data);
                        }
                    }
                    catch (Exception ex) {
                        if (!alreadyQueued) {
                            ctx.Enqueue (JsonConvert.SerializeObject (msgJson)); // TODO
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
            object res = null;
            switch (action) {
            case DataVerb.Post:
                res = await client.Create (json);
                break;
            case DataVerb.Put:
                res = await client.Update (json);
                break;
            case DataVerb.Delete:
                await client.Delete (json);
                break;
            }
        }
    }
}
