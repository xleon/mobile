using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;
using Android.App;
using Android.Content;
using Android.Gms.Common;
using Android.Gms.Common.Apis;
using Android.Gms.Wearable;
using Android.Util;
using Java.Interop;
using Java.Util.Concurrent;
using Toggl.Joey.UI.Activities;
using Toggl.Phoebe.Data;
using Toggl.Phoebe.Data.Models;
using Toggl.Phoebe.Net;
using XPlatUtils;

namespace Toggl.Joey.Wear
{
    /// <summary>
    /// Listens to DataItems and Messages from the local node
    /// </summary>
    [Service, IntentFilter (new [] { "com.google.android.gms.wearable.BIND_LISTENER" }) ]
    public class WearDataService : WearableListenerService,  GoogleApiClient.IConnectionCallbacks
    {
        public const string Tag = "WearableTag";
        public const string DataStorePath = "/TimeEntryDataStore";
        private GoogleApiClient googleApiClient;
        private List<SimpleTimeEntryData> entryData;
        private PutDataMapRequest mapReq;
        private List<DataMap> currentDataMap;

        public WearDataService()
        {
        }

        public WearDataService (Context ctx)
        {
            Init (ctx);
        }

        private void Init (Context ctx)
        {
            googleApiClient = new GoogleApiClient.Builder (ctx)
            .AddApi (WearableClass.API)
            .Build ();
            googleApiClient.Connect ();
        }
        public override void OnCreate ()
        {
            base.OnCreate ();
            Init (this);
            var manager = ServiceContainer.Resolve<ActiveTimeEntryManager> ();
            if (manager.Active == null) {
                return;
            }
            manager.PropertyChanged += OnActiveTimeEntryManagerPropertyChanged;
        }

        private async void OnActiveTimeEntryManagerPropertyChanged (object sender, PropertyChangedEventArgs args)
        {
            if (args.PropertyName == ActiveTimeEntryManager.PropertyActive) {
                await UpdateSharedTimeEntryList ();
            }
        }

        public override void OnDataChanged (DataEventBuffer dataEvents)
        {
            if (!googleApiClient.IsConnected) {
                ConnectionResult connectionResult = googleApiClient.BlockingConnect (30, TimeUnit.Seconds);
                if (!connectionResult.IsSuccess) {
                    Log.Error (Tag, "DataLayerListenerService failed to connect to GoogleApiClient");
                    return;
                }
            }
        }

        public async void OnConnected (Android.OS.Bundle connectionHint)
        {
            await UpdateSharedTimeEntryList();
        }

        public void OnConnectionSuspended (int cause)
        {
        }

        public async override void OnMessageReceived (IMessageEvent messageEvent)
        {
            LOGD (Tag, "OnMessageReceived: " + messageEvent);
            await HandleMessage (messageEvent);
            base.OnMessageReceived (messageEvent);
        }

        public override void OnPeerConnected (INode peer)
        {
            LOGD (Tag, "OnPeerConnected: " + peer);
        }

        public override void OnPeerDisconnected (INode peer)
        {
            LOGD (Tag, "OnPeerDisconnected: " + peer);
        }

        private async Task HandleMessage (IMessageEvent message)
        {
            try {
                Log.Info ("WearIntegration", "Received Message");

                if (!googleApiClient.IsConnected) {
                    googleApiClient.Connect();
                }

                var authManager = ServiceContainer.Resolve<AuthManager> ();
                if (!authManager.IsAuthenticated) {
                    NotifyNotLoggedIn();
                    return;
                }

                var path = message.Path;

                try {
                    if (path == Common.StartStopTimeEntryPath) {

                        await WearDataProvider.StartStopTimeEntry (BaseContext);
                        await UpdateSharedTimeEntryList ();
                    } else if (path == Common.ContinueTimeEntryPath) {

                        var guid = Guid.Parse (Common.GetString (message.GetData()));
                        await StartEntry (guid);
                        await UpdateSharedTimeEntryList ();
                    } else if (path == Common.RequestSyncPath) {

                        await UpdateSharedTimeEntryList ();
                    } else if (path == Common.OpenHandheldPath) {

                        StartMainActivity ();
                    }
                } catch (Exception e) {
                    Log.Error ("WearIntegration", e.ToString ());
                }
            } catch (Exception e) {
                Log.Error ("WearIntegration", e.ToString ());
            }
        }

        private void NotifyNotLoggedIn()
        {
            Task.Run (() => {
                var apiResult = WearableClass.NodeApi.GetConnectedNodes (googleApiClient) .Await ().JavaCast<INodeApiGetConnectedNodesResult> ();
                var nodes = apiResult.Nodes;
                foreach (var node in nodes) {
                    WearableClass.MessageApi.SendMessage (googleApiClient, node.Id,
                                                          Common.UserNotLoggedInPath,
                                                          new byte[0]);
                }
            });
        }

        private void StartMainActivity ()
        {
            var intent = new Intent (this, typeof (MainDrawerActivity));
            intent.AddFlags (ActivityFlags.NewTask);
            StartActivity (intent);
        }

        private async Task StartEntry (Guid id)
        {
            var model = new TimeEntryModel (id);
            await model.LoadAsync();
            await model.ContinueAsync();
        }

        public async Task UpdateSharedTimeEntryList ()
        {
            entryData = await WearDataProvider.GetTimeEntryData ();

            mapReq = PutDataMapRequest.Create (Common.TimeEntryListPath);

            currentDataMap = new List<DataMap> ();

            foreach (var entry in entryData) {
                currentDataMap.Add (entry.DataMap);
            }
            mapReq.DataMap.PutDataMapArrayList (Common.TimeEntryListKey, currentDataMap);
            await WearableClass.DataApi.PutDataItem (googleApiClient, mapReq.AsPutDataRequest ());
        }

        public static void LOGD (string tag, string message)
        {
            if (Log.IsLoggable (tag, LogPriority.Debug)) {
                Log.Debug (tag, message);
            }
        }
    }
}

