using System;

namespace Toggl.Phoebe.Data
{
    public class DataStoreIdleMessage : Message
    {
        public DataStoreIdleMessage (IDataStore sender) : base (sender)
        {
        }
    }
}
