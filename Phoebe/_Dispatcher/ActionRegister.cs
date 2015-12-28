using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using static System.Console;

namespace Toggl.Phoebe
{
    public static class ActionRegister
    {
        public static Func<object, Task<ActionMsg>> GetCallback (string tag)
        {
            switch (tag) {
            case ActionMsg.LOAD_MORE_ENTRIES:
                return LoadMoreEntries;
            default:
                return null;
            }
        }

        static Task<ActionMsg> LoadMoreEntries (object data)
        {
            throw new NotImplementedException ();
        }

        static Task<ActionMsg> RemoveEntry (object data)
        {
            throw new NotImplementedException ();
        }
    }
}

