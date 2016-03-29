using System;
using System.Threading.Tasks;

namespace Toggl.Phoebe._Net
{
    public interface IPushClient
    {
        Task Register (string authToken, PushService service, string regid);

        Task Unregister (string authToken, PushService service, string regid);
    }
}
