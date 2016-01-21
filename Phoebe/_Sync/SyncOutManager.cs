using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Toggl.Phoebe.Data.DataObjects;
using Toggl.Phoebe.Logging;
using Toggl.Phoebe.Net;
using XPlatUtils;
using System.Threading.Tasks;

namespace Toggl.Phoebe.Sync
{
    public static class SyncOutManager
    {
        // TODO: This queue must be persistant
        static ConcurrentQueue<IDataSyncMsg> queue = new ConcurrentQueue<IDataSyncMsg> ();
        static ITogglClient client = ServiceContainer.Resolve<ITogglClient> ();

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
            // TODO: Check queue size, if it reaches a limit, empty and request full sync next time

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
        static Task<bool> SendMessage (IDataSyncMsg msg)
        {
            throw new NotImplementedException ();

//            object res = null;
//            switch (msg.Verb) {
//            case DataVerb.Post:
//                res = await client.Create (msg.Data);
//            case DataVerb.Put:
//                res = await client.Update (msg.Data);
//            case DataVerb.Delete:
//                res = await client.Delete (msg.Data);
//            }
//
//            return res != null;
        }
    }
}
