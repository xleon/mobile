using Toggl.Phoebe.Net;

namespace Toggl.Ross.Net
{
    public class NetworkPresence : INetworkPresence
    {
        public bool IsNetworkPresent
        {
            get { return true; }
        }

        public void RegisterSyncWhenNetworkPresent()
        {

        }

        public void UnregisterSyncWhenNetworkPresent()
        {

        }
    }
}