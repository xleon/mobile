using System;
using System.Threading.Tasks;

namespace Toggl.Phoebe.Net
{
    public interface IPushClient
    {
        Task<string> GetPushToken(object deviceToken = null);

        Task Register(string authToken, string pushToken);

        Task Unregister(string authToken, string pushToken);
    }
}
