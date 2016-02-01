using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Toggl.Phoebe.Logging;
using Toggl.Phoebe._Data;
using Toggl.Phoebe._Data.Json;
using Toggl.Phoebe._Net;
using XPlatUtils;

namespace Toggl.Phoebe._Reactive
{
    public class SyncOutManager
    {
        public static SyncOutManager Singleton { get; private set; }

        public static void Init ()
        {
            Store.Init ();
            Singleton = new SyncOutManager ();
        }

        readonly JsonMapper mapper = new JsonMapper ();

        readonly Toggl.Phoebe.Net.INetworkPresence networkPresence =
              ServiceContainer.Resolve<Toggl.Phoebe.Net.INetworkPresence> ();

        readonly Toggl.Phoebe.Data.IDataStore dataStore =
            ServiceContainer.Resolve<Toggl.Phoebe.Data.IDataStore> ();

        readonly ITogglClient client = new TogglRestClient (
            Build.ApiUrl, ServiceContainer.Resolve<Toggl.Phoebe.Net.AuthManager> ().Token);

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
            // TODO: Limit queue size?

            // Check internet connection
            var isConnected = networkPresence.IsNetworkPresent;

            dataStore.ExecuteInTransactionSilent (async ctx => {
                foreach (var msg in msgs) {
                    bool alreadyQueued = false;
                    var exported = mapper.MapToJson (msg.Data);

                    // If there's no connection, just enqueue the message
                    if (!isConnected) {
                        Enqueue (ctx, msg.Action, exported);
                        continue;
                    }

                    try {
                        string json = null;
                        if (ctx.TryDequeue (out json)) {
                            // Send queue to server
                            do {
								Enqueue (ctx, msg.Action, exported);
								alreadyQueued = true;
								
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
                            Enqueue (ctx, msg.Action, exported);
                        }
                        var log = ServiceContainer.Resolve<ILogger> ();
                        log.Error (typeof(SyncOutManager).Name, ex, "Failed to send data to server");
                    }
                }
            });
        }

        void Enqueue (Toggl.Phoebe.Data.IDataStoreContext ctx, DataAction action, CommonJson json)
        {
            try {
                ctx.Enqueue (JsonConvert.SerializeObject (new DataJsonMsg (action, json)));
            }
            catch (Exception ex) {
                // TODO: Retry?
                var log = ServiceContainer.Resolve<ILogger> ();
                log.Error (typeof(SyncOutManager).Name, ex, "Failed to queue message");
            }
        }

        async Task SendMessage (DataAction action, CommonJson json)
        {
            if (action == DataAction.Put) {
                if (json.RemoteId != null) {
                    await client.Update (json);
                }
                else {
                    var res = await client.Create (json);
                    // TODO: Store RemoteId
                }
            }
            else {
                await client.Delete (json);
            }
        }
    }
}
