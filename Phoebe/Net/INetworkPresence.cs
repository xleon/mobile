
namespace Toggl.Phoebe.Net
{
    public interface INetworkPresence
    {
        bool IsNetworkPresent { get; }
        void RegisterSyncWhenNetworkPresent();
        void UnregisterSyncWhenNetworkPresent();
    }
}
