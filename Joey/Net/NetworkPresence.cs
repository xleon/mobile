using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Net;
using Java.Lang;
using Toggl.Phoebe.Net;
using XPlatUtils;

namespace Toggl.Joey.Net
{
    public class NetworkPresence : INetworkPresence
    {
        private readonly Context context;
        private readonly ConnectivityManager connectivityManager;

        public NetworkPresence(Context context, ConnectivityManager connectivityManager)
        {
            this.context = context;
            this.connectivityManager = connectivityManager;
        }

        public bool IsNetworkPresent
        {
            get
            {
                return IsNetworkConnected(connectivityManager.ActiveNetworkInfo);
            }
        }

        public void RegisterSyncWhenNetworkPresent()
        {
            SetSyncWhenNetworkPresent(true);
        }

        public void UnregisterSyncWhenNetworkPresent()
        {
            SetSyncWhenNetworkPresent(false);
        }

        private void SetSyncWhenNetworkPresent(bool enable)
        {
            var receiver = new ComponentName(context, Class.FromType(typeof(SyncOnNetworkPresentChangeReceiver)));
            var setting = context.PackageManager.GetComponentEnabledSetting(receiver);

            if (enable)
            {
                if (setting != ComponentEnabledState.Enabled)
                    context.PackageManager.SetComponentEnabledSetting(receiver, ComponentEnabledState.Enabled, ComponentEnableOption.DontKillApp);
            }
            else
            {
                if (setting == ComponentEnabledState.Enabled)
                    context.PackageManager.SetComponentEnabledSetting(receiver, ComponentEnabledState.Disabled, ComponentEnableOption.DontKillApp);
            }
        }

        private static bool IsNetworkConnected(NetworkInfo networkInfo)
        {
            return networkInfo != null && networkInfo.IsConnected;
        }

        [BroadcastReceiver(Enabled = false), 
            IntentFilter(new[] { ConnectivityManager.ConnectivityAction }, 
                Categories = new[] { "com.toggl.timer" })]
        public class SyncOnNetworkPresentChangeReceiver : BroadcastReceiver
        {
            public override void OnReceive(Context context, Intent intent)
            {
                var info = intent.Extras.Get(ConnectivityManager.ExtraNetworkInfo) as NetworkInfo;

                if (IsNetworkConnected(info))
                {
                    ServiceContainer.Resolve<SyncManager>().Run();
                }
            }
        }
    }
}