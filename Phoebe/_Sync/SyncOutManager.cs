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

namespace Toggl.Phoebe.Sync
{
    public static class SyncOutManager
    {
        // TODO: This queue must be persistant
        static ConcurrentQueue<IDataSyncMsg> queue = new ConcurrentQueue<IDataSyncMsg> ();
        // TODO: Export without accessing the database?
        static readonly IDataStore dataStore = ServiceContainer.Resolve<IDataStore> ();
        static readonly ITogglClient client = ServiceContainer.Resolve<ITogglClient> ();

        static SyncOutManager ()
        {   
            Store.Observe <IDataSyncMsg> ().Subscribe (
                x => x.Data.Match (y => HandleMessage (new [] { y }),
                    e => {})); // TODO: Error handling

            Store.Observe <IDataSyncGroupMsg> ().Subscribe (
                x => x.Data.Match (y => HandleMessage (y.RawMessages),
                e => {})); // TODO: Error handling
        }

        static async void HandleMessage (IEnumerable<IDataSyncMsg> msgs)
        {
            foreach (var msg in msgs.Where (x => x.Dir == DataDir.Outcoming)) {
                await EnqueueOrSend (msg);
            }
        }

        static async Task EnqueueOrSend (IDataSyncMsg newMsg)
        {
            // TODO: Check internet connection
            // TODO: Check queue size, if it reaches a limit, empty it and request full sync next time

            bool alreadyQueued = false;
            try {
                if (queue.Count > 0) {
                    queue.Enqueue (newMsg);
                    alreadyQueued = true;

                    // Send queue to server
                    IDataSyncMsg curMsg = null;
                    while (queue.TryPeek (out curMsg)) {
                        var success = await SendMessage (curMsg);
                        if (!success) {
                            break;
                        }

                        // TODO: Check if this has actually been dequeued
                        var dequeued = queue.TryDequeue (out curMsg);
                    }
                }
                else {
                    // If there's no queue, try to send the message directly
                    var success = await SendMessage (newMsg);
                    if (!success) {
                        queue.Enqueue (newMsg);
                    }
                }
            }
            catch (Exception ex) {
                if (!alreadyQueued) {
                    queue.Enqueue (newMsg);
                }
                var log = ServiceContainer.Resolve<ILogger> ();
                log.Error (typeof(SyncOutManager).Name, ex, "Failed to send data to server");
            }
        }

        // TODO: Check client methods results
        // TODO: Convert messages to JSON in groups to improve performance (same transaction)
        static async Task<bool> SendMessage (IDataSyncMsg msg)
        {
            object res = null;
            await dataStore.ExecuteInTransactionWithMessagesAsync(async ctx => {
                var json = msg.RawData.Export (ctx);

                switch (msg.Verb) {
                case DataVerb.Post:
                    res = await client.Create (json);
                    break;
                case DataVerb.Put:
                    res = await client.Update (json);
                    break;
                case DataVerb.Delete:
                    // TODO: Why Delete doesn't return anything?
                    await client.Delete (json);
                    res = new object();
                    break;
                }
            });

            return res != null;
        }
    }
}
