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
            Store.Singleton.Observe ()
                .Subscribe (msg => msg.RawData.Match (
                    x => {
                        DataSyncMsg singleMsg;
                        IDataSyncGroup groupMsg;
                        if ((singleMsg = x as DataSyncMsg) != null
                            && singleMsg.Dir == DataDir.Outcoming) {
                            EnqueueOrSend (new [] { singleMsg });
                        }
                        else if ((groupMsg = x as IDataSyncGroup) != null) {
                            var outMsgs = groupMsg.SyncMessages.Where (
                                y => y.Dir == DataDir.Outcoming).ToList ();

                            if (outMsgs.Count > 0)
                                EnqueueOrSend (outMsgs);
                        }
                    },
                    e => {}
                )
            );
        }

        void EnqueueOrSend (IList<DataSyncMsg> msgs)
        {
            // TODO: Check internet connection
            // TODO: Check queue size, if it reaches a limit, empty it and request full sync next time

            dataStore.ExecuteInTransactionSilent (async ctx => {
                foreach (var msg in msgs) {
                    bool alreadyQueued = false;
                    var exported = msg.Data.Export (ctx);

                    try {
                        string json = null;
                        ctx.Enqueue (JsonConvert.SerializeObject (new DataJsonMsg (msg.Action, exported)));
                        alreadyQueued = true;
                        
                        if (ctx.TryDequeue (out json)) {
                            // Send queue to server
                            do {
                                var jsonMsg = JsonConvert.DeserializeObject<DataJsonMsg> (json);
                                await SendMessage (jsonMsg.Action, jsonMsg.Data);

                                // If we sent the message successfully, remove it from the queue
                                ctx.TryDequeue (out json);
                            } while (ctx.TryPeekQueue (out json));
                        }
                        else {
                            // If there's no queue, try to send the message directly
                            await SendMessage (msg.Action, exported);
                        }
                    }
                    catch (Exception ex) {
                        if (!alreadyQueued) {
                            ctx.Enqueue (JsonConvert.SerializeObject (new DataJsonMsg (msg.Action, exported)));
                        }
                        var log = ServiceContainer.Resolve<ILogger> ();
                        log.Error (typeof(SyncOutManager).Name, ex, "Failed to send data to server");
                    }
                }
            });
        }

        // TODO: Check client methods results -> Assign ID for newly created json
        async Task SendMessage (DataAction action, CommonJson json)
        {
            // TODO: Check RemoteId to see if we need to Create or Update
            switch (action) {
//            case DataAction.Post:
//                await client.Create (json);
//                break;
            case DataAction.Put:
                await client.Update (json);
                break;
            case DataAction.Delete:
                await client.Delete (json);
                break;
            }
        }
    }
}
