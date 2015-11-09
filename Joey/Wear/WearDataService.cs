using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;
using Android.App;
using Android.Gms.Common;
using Android.Gms.Common.Apis;
using Android.Gms.Wearable;
using Android.Util;
using Java.Util.Concurrent;
using Toggl.Phoebe.Data;
using Toggl.Phoebe.Data.Models;
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

        GoogleApiClient googleApiClient;

        public override void OnCreate ()
        {
            base.OnCreate ();
            googleApiClient = new GoogleApiClient.Builder (this)
            .AddApi (WearableClass.API)
            .Build ();
            googleApiClient.Connect ();

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
            await UpdateSharedTimeEntryList ();
        }

        public void OnConnectionSuspended (int cause)
        {
        }

        public async override void OnMessageReceived (IMessageEvent messageEvent)
        {
            LOGD (Tag, "OnMessageReceived: " + messageEvent);

            base.OnMessageReceived (messageEvent);
            await HandleMessage (messageEvent);
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

                var path = message.Path;

                try {
                    if (path == Common.StartTimeEntryPath || path == Common.StopTimeEntryPath) {

                        // Start new time entry.
                        await WearDataProvider.StartStopTimeEntry ();
                        await UpdateSharedTimeEntryList ();

                    } else if (path == Common.RestartTimeEntryPath) {

                        var guid = Guid.Parse (Common.GetString (message.GetData()));
                        await StartEntry (guid);
                        await UpdateSharedTimeEntryList ();
                        // Get time entry Id needed.
                    } else if (path == Common.RequestSyncPath) {

                        await UpdateSharedTimeEntryList ();
                    }
                } finally {
                }
            } catch (Exception e) {
                Log.Error ("WearIntegration", e.ToString ());
            }
        }

        private async Task StartEntry (Guid id)
        {
            var model = new TimeEntryModel (id);
            await model.LoadAsync();
            await model.ContinueAsync();
        }

        private async Task UpdateSharedTimeEntryList ()
        {
            var entryData = await WearDataProvider.GetTimeEntryData ();

            // Publish changes to weareable using DataItems
            var mapReq = PutDataMapRequest.Create (Common.TimeEntryListPath);

            var children = new List<DataMap> ();

            foreach (var entry in entryData) {
                children.Add (entry.DataMap);
            }
            mapReq.DataMap.PutDataMapArrayList (Common.TimeEntryListKey, children);
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

