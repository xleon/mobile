
namespace Toggl.Phoebe._Net
{
    public interface INetworkPresence
    {
        bool IsNetworkPresent { get; }
        void RegisterSyncWhenNetworkPresent ();
        void UnregisterSyncWhenNetworkPresent ();
    }
}
