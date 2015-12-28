using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using static System.Console;

namespace Toggl.Phoebe
{
    public static class ActionRegister
    {
<<<<<<< HEAD
        public static Func<DataMsgUntyped, Task<DataMsgUntyped>> GetCallback (string tag)
        {
            switch (tag) {
            case DataMsg.LOAD_MORE_ENTRIES:
=======
        public static Func<object, Task<ActionMsg>> GetCallback (string tag)
        {
            switch (tag) {
            case ActionMsg.LOAD_MORE_ENTRIES:
>>>>>>> First refactor for unidirectional prototype
                return LoadMoreEntries;
            default:
                return null;
            }
        }

<<<<<<< HEAD
        static Task<DataMsgUntyped> LoadMoreEntries (object data)
=======
        static Task<ActionMsg> LoadMoreEntries (object data)
>>>>>>> First refactor for unidirectional prototype
        {
            throw new NotImplementedException ();
        }

<<<<<<< HEAD
        static Task<DataMsgUntyped> RemoveEntry (object data)
=======
        static Task<ActionMsg> RemoveEntry (object data)
>>>>>>> First refactor for unidirectional prototype
        {
            throw new NotImplementedException ();
        }
    }
}
