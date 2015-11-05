using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Android.App;
using Android.Gms.Common;
using Android.Gms.Common.Apis;
using Android.Gms.Wearable;
using Android.OS;
using Android.Support.Wearable.Views;
using Android.Util;
using Java.Interop;
using Java.Util.Concurrent;
using Toggl.Chandler.UI.Adapters;

namespace Toggl.Chandler.UI.Activities
{
    [Activity (Label = "Toggl", MainLauncher = true, Icon = "@drawable/Icon" )]
    public class MainActivity : Activity, IDataApiDataListener, GoogleApiClient.IConnectionCallbacks, IMessageApiMessageListener, INodeApiNodeListener
    {
        private const string Tag = "MainActivity";
        private GridViewPager ViewPager;
        private GoogleApiClient googleApiClient;
        private PagesAdapter adapter;
        private List<SimpleTimeEntryData> timeEntries = new List<SimpleTimeEntryData> ();

        protected override void OnCreate (Bundle savedInstanceState)
        {
            base.OnCreate (savedInstanceState);

            SetContentView (Resource.Layout.Main);

            ViewPager = FindViewById<GridViewPager> (Resource.Id.pager);
            adapter = new PagesAdapter (this, FragmentManager);

            ViewPager.Adapter = adapter;

            googleApiClient = new GoogleApiClient.Builder (this)
            .AddApi (WearableClass.API)
            .AddConnectionCallbacks (this)
            .Build ();
            googleApiClient.Connect();
            if (CollectionChanged != null) {
                CollectionChanged (this, EventArgs.Empty);
            }
        }

        public List<SimpleTimeEntryData> Data
        {
            get {
                return timeEntries;
            }
        }

        public event EventHandler CollectionChanged;


        protected override void OnResume ()
        {
            base.OnResume ();
            googleApiClient.Connect ();
        }

        protected override void OnPause ()
        {
            base.OnPause ();
            WearableClass.DataApi.RemoveListener (googleApiClient, this);
            WearableClass.MessageApi.RemoveListener (googleApiClient, this);
            WearableClass.NodeApi.RemoveListener (googleApiClient, this);
            googleApiClient.Disconnect ();
        }

        public void SendNewData (GoogleApiClient googleApiClient)
        {
            // Publis changes to weareable using DataItems
            var mapReq = PutDataMapRequest.Create (Common.TimeEntryListPath);
            var map = mapReq.DataMap;
            map.PutBoolean (Common.SingleEntryKey, true);
            WearableClass.DataApi.PutDataItem (googleApiClient, mapReq.AsPutDataRequest ());
        }

        public void OnDataChanged (DataEventBuffer dataEvents)
        {
            if (!googleApiClient.IsConnected) {
                ConnectionResult connectionResult = googleApiClient.BlockingConnect (30, TimeUnit.Seconds);
                if (!connectionResult.IsSuccess) {
                    Log.Error (Tag, "DataLayerListenerService failed to connect to GoogleApiClient");
                    return;
                }
            }

            foreach (var data in dataEvents) {
                if (data != null && data.Type == DataEvent.TypeChanged && data.DataItem.Uri.Path == Common.TimeEntryListPath) {
                    OnDataChanged (data.DataItem);
                }
            }
        }

        private void OnDataChanged (IDataItem dataItem)
        {
            var map = DataMapItem.FromDataItem (dataItem).DataMap;
            var list = map.GetDataMapArrayList (Common.TimeEntryListKey);
            if (list == null) {
                return;
            }
            foreach (var mapItem in list) {
                var en = new SimpleTimeEntryData (mapItem);
                timeEntries.Add (en);
            }
            if (CollectionChanged != null) {
                CollectionChanged (this, EventArgs.Empty);
            }
        }

        public void RequestSync ()
        {
            Task.Run (() => {
                var apiResult = WearableClass.NodeApi.GetConnectedNodes (googleApiClient) .Await ().JavaCast<INodeApiGetConnectedNodesResult> ();
                var nodes = apiResult.Nodes;
                foreach (var node in nodes) {
                    WearableClass.MessageApi.SendMessage (googleApiClient, node.Id,
                                                          Common.RequestSyncPath,
                                                          new byte[0]);
                }
            });
            SendNewData (googleApiClient);
        }

        private void SendStartStopMessage ()
        {
            Task.Run (() => {
                var apiResult = WearableClass.NodeApi.GetConnectedNodes (googleApiClient) .Await ().JavaCast<INodeApiGetConnectedNodesResult> ();
                var nodes = apiResult.Nodes;
                foreach (var node in nodes) {
                    WearableClass.MessageApi.SendMessage (googleApiClient, node.Id,
                                                          Common.StartTimeEntryPath,
                                                          new byte[0]);
                }
            });
            SendNewData (googleApiClient);
        }

        public void OnMessageReceived (IMessageEvent messageEvent)
        {
            LOGD (Tag, "OnMessageReceived: " + messageEvent);
        }

        public void OnConnected (Bundle bundle)
        {
            LOGD (Tag, "OnConnected(): Successfully connected to Google API client");
            WearableClass.DataApi.AddListener (googleApiClient, this);
            WearableClass.MessageApi.AddListener (googleApiClient, this);
            WearableClass.NodeApi.AddListener (googleApiClient, this);

            // I'm online, give me the new data & state.
            RequestSync ();
        }

        public void OnConnectionSuspended (int p0)
        {
            LOGD (Tag, "OnConnectionSuspended(): Connection to Google API clinet was suspended");
        }

        public void OnConnectionFailed (Android.Gms.Common.ConnectionResult result)
        {
            LOGD (Tag, "OnConnectionFailed(): Failed to connect, with result: " + result);
        }

        public void OnPeerConnected (INode peer)
        {
            LOGD (Tag, "OnPeerConnected: " + peer);
        }

        public void OnPeerDisconnected (INode peer)
        {
            LOGD (Tag, "OnPeerDisconnected: " + peer);
        }

        public static void LOGD (string tag, string message)
        {
            if (Log.IsLoggable (tag, LogPriority.Debug)) {
                Log.Debug (tag, message);
            }
        }
    }
}
