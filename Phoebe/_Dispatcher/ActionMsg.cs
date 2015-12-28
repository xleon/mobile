using System;
using System.Threading.Tasks;
using Toggl.Phoebe.Logging;
using XPlatUtils;

namespace Toggl.Phoebe
{
    public struct ActionMsg {
        public const string ERROR = "ERROR";
        public const string LOAD_MORE_ENTRIES = "LOAD_MORE_ENTRIES";

        public readonly string Tag;
        public readonly object Data;

        public ActionMsg (string tag, object data)
        {
            Tag = tag;
            Data = data;
        }

        public async Task<ActionMsg> TryProcess (Func<object,Task<ActionMsg>> callback, bool isStore = false)
        {
            try {
                return await callback (Data);
            } catch (Exception ex) {
                var log = ServiceContainer.Resolve<ILogger> ();
                log.Error (isStore ? "STORE" : "DISPATCHER", ex, "Tag: " + Tag);
                return new ActionMsg (ActionMsg.ERROR, ex);
            }
        }

        public Task<ActionMsg> TryProcessInStore (Func<object,Task<ActionMsg>> callback)
        {
            return TryProcess (callback, true);
        }
    }
}

