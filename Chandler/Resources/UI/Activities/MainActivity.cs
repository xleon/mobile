using System;
using System.Collections.Generic;
using Android.App;
using Android.Gms.Common;
using Android.Gms.Common.Apis;
using Android.Gms.Wearable;
using Android.OS;
using Android.Support.Wearable.Views;
using Android.Util;
using Java.Util.Concurrent;
using Toggl.Chandler.Resources.UI.Adapters;

namespace Toggl.Chandler.Resources.UI.Activities
{
    [Activity (Label = "Toggl", MainLauncher = true, Icon = "@drawable/Icon" )]
    public class MainActivity : Activity, IDataApiDataListener, GoogleApiClient.IConnectionCallbacks, IMessageApiMessageListener, INodeApiNodeListener
    {
        private const string Tag = "MainActivity";
        private GridViewPager ViewPager;
        private GoogleApiClient googleApiClient;
        private PagesAdapter adapter;

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
        }

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
            var entryList = new List<SimpleTimeEntryData> ();
            foreach (var mapItem in list) {
                var en = new SimpleTimeEntryData (mapItem);
                Console.WriteLine ("en.Desc: {0}, proj: {1}", en.Description, en.Project);
                entryList.Add (en);
            }
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
