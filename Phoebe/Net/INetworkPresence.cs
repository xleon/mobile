
using Plugin.Connectivity;
using Toggl.Phoebe.Data;
using Toggl.Phoebe.Reactive;

namespace Toggl.Phoebe.Net
{
    public interface INetworkPresence
    {
        bool IsNetworkPresent { get; }
    }

    public class NetworkPresence : INetworkPresence
    {
        public NetworkPresence()
        {
            CrossConnectivity.Current.ConnectivityChanged += (sender, args) =>
            {
                if (args.IsConnected)
                    RxChain.Send(new ServerRequest.GetChanges());
            };
        }

        public bool IsNetworkPresent
        {
            get { return CrossConnectivity.Current.IsConnected; }
        }

        public void RegisterSyncWhenNetworkPresent()
        {

        }

        public void UnregisterSyncWhenNetworkPresent()
        {

        }
    }
}
