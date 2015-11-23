using System;
using System.Linq;
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

        // Send messages to all client nodes in parallel
        private Task _sendMessage(string message, byte[] data)
        {
            return Task.WhenAll(
                from node in clientNodes.AsParallel()
                select WearableClass.MessageApi.SendMessage(
                    googleApiClient, node.Id, message, data).AsAsync()
            );
        }

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
                var connectionResult = googleApiClient.BlockingConnect (30, TimeUnit.Seconds);
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
            if (map.ContainsKey (Common.TimeEntryListKey)) {
                var list = map.GetDataMapArrayList (Common.TimeEntryListKey);

                if (list.Count == 0) {
                    return;
                }

                timeEntries.Clear();
                foreach (var mapItem in list) {
                    var en = new SimpleTimeEntryData (mapItem);
                    timeEntries.Add (en);
                }
            }
        }

        public async Task RequestSync ()
        {
            Exception error = null;
            do {
                try {
                    await Common.TimedAwait(
                        RebindTime,
                        _sendMessage(Common.RequestSyncPath, new byte[0]));
                }
                catch (Exception tempEx) {
                    error = tempEx;
                    Log.Error (Tag, error.Message);
                }
            } while (error != null && adapter.Timer.UserLoggedIn);
        }

        public void RequestStartStop ()
        {
            SendMessage (Common.StartStopTimeEntryPath, new byte[0]);
        }

        public void RequestStartEntry (string guid)
        {
            SendMessage (Common.ContinueTimeEntryPath, Common.GetBytes (guid));
            ViewPager.SetCurrentItem (0, 0, true);
        }

        public void RequestHandheldOpen ()
        {
            SendMessage (Common.OpenHandheldPath, new byte[0]);
            ViewPager.SetCurrentItem (0, 0, true);
        }

        private void SendMessage (string messageKey, byte[] data)
        {
            _sendMessage(messageKey, data).Start();
        }

        private IList<INode> clientNodes
        {
            get {
                return WearableClass
                    .NodeApi
                    .GetConnectedNodes (googleApiClient)
                    .Await ()
                    .JavaCast<INodeApiGetConnectedNodesResult> ()
                    .Nodes;
            }
        }

        public async void OnMessageReceived (IMessageEvent messageEvent)
        {
            if (messageEvent.Path == Common.UserNotLoggedInPath) {
                adapter.Timer.UserLoggedIn = false;
                await Task.Delay(RebindTime);
                await RequestSync();
            }
        }

        public async void OnConnected (Bundle bundle)
        {
            await WearableClass.DataApi.AddListener (googleApiClient, this);
            await WearableClass.MessageApi.AddListener (googleApiClient, this);
            await RequestSync();
        }

        public void OnConnectionSuspended (int cause)
        {
        }
    }
}
