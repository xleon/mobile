using System;
using System.Collections.Generic;
using System.Threading;
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
    public class MainActivity : Activity, IDataApiDataListener, GoogleApiClient.IConnectionCallbacks, IMessageApiMessageListener
    {
        private const string Tag = "MainActivity";
        private GridViewPager ViewPager;
        private GoogleApiClient googleApiClient;
        private PagesAdapter adapter;
        private List<SimpleTimeEntryData> timeEntries = new List<SimpleTimeEntryData> ();
        private const int RebindTime = 2000;

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
            googleApiClient.Connect ();

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
            googleApiClient.Disconnect ();
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

            adapter.Timer.UserLoggedIn = true;
            foreach (var data in dataEvents) {
                if (data != null && data.Type == DataEvent.TypeChanged && data.DataItem.Uri.Path == Common.TimeEntryListPath) {
                    OnDataChanged (data.DataItem);
                }
            }

            if (CollectionChanged != null) {
                CollectionChanged (this, EventArgs.Empty);
            }
        }

        private void OnDataChanged (IDataItem dataItem)
        {
            var map = DataMapItem.FromDataItem (dataItem).DataMap;
            var list = map.GetDataMapArrayList (Common.TimeEntryListKey);
            if (list == null) {
                return;
            }
            timeEntries.Clear();
            foreach (var mapItem in list) {
                var en = new SimpleTimeEntryData (mapItem);
                timeEntries.Add (en);
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
                Thread.Sleep (RebindTime);
                if (Data.Count == 0 && adapter.Timer.UserLoggedIn) {
                    RequestSync();
                }
            });
        }

        public void RequestStartStop ()
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
        }

        public void StartEntry (string guid)
        {
            Task.Run (() => {
                var apiResult = WearableClass.NodeApi.GetConnectedNodes (googleApiClient) .Await ().JavaCast<INodeApiGetConnectedNodesResult> ();
                var nodes = apiResult.Nodes;
                foreach (var node in nodes) {
                    WearableClass.MessageApi.SendMessage (googleApiClient, node.Id,
                                                          Common.RestartTimeEntryPath,
                                                          Common.GetBytes (guid));
                }
            });
            ViewPager.SetCurrentItem (0, 0, true);
        }

        public void StartHandheldApp ()
        {
            Task.Run (() => {
                var apiResult = WearableClass.NodeApi.GetConnectedNodes (googleApiClient) .Await ().JavaCast<INodeApiGetConnectedNodesResult> ();
                var nodes = apiResult.Nodes;
                foreach (var node in nodes) {
                    WearableClass.MessageApi.SendMessage (googleApiClient, node.Id,
                                                          Common.StartHandheldApp,
                                                          new byte[0]);
                }
            });
            ViewPager.SetCurrentItem (0, 0, true);
        }

        public void OnMessageReceived (IMessageEvent messageEvent)
        {
            LOGD (Tag, "OnMessageReceived: " + messageEvent);
            if (messageEvent.Path == Common.UserNotLoggedIn) {
                adapter.Timer.UserLoggedIn = false;
                Task.Run (() => {
                    Thread.Sleep (RebindTime);
                    RequestSync();
                });
            }
        }

        public void OnConnected (Bundle bundle)
        {
            LOGD (Tag, "OnConnected(): Successfully connected to Google API client");
            WearableClass.DataApi.AddListener (googleApiClient, this);
            WearableClass.MessageApi.AddListener (googleApiClient, this);

            // I'm online, give me the new data & state.
            RequestSync ();
        }

        public void OnConnectionSuspended (int cause)
        {
            LOGD (Tag, "OnConnectionSuspended(): Disconnected Google API client");
        }

        public static void LOGD (string tag, string message)
        {
            if (Log.IsLoggable (tag, LogPriority.Debug)) {
                Log.Debug (tag, message);
            }
        }
    }
}
