using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using static System.Console;

namespace Toggl.Phoebe
{
    public static class ActionRegister
    {
        public static Func<DataMsgUntyped, Task<DataMsgUntyped>> GetCallback (string tag)
        {
            switch (tag) {
            case DataMsg.LOAD_MORE_ENTRIES:
                return LoadMoreEntries;
            default:
                return null;
            }
        }

        static Task<DataMsgUntyped> LoadMoreEntries (object data)
        {
            throw new NotImplementedException ();
        }

        static Task<DataMsgUntyped> RemoveEntry (object data)
        {
            throw new NotImplementedException ();
        }
    }
}

