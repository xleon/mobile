using System;

namespace Toggl.Phoebe.Data
{
    public class DataChangeMessage : Message
    {
        private readonly object data;
        private readonly DataAction action;

        public DataChangeMessage (IDataStore sender, object data, DataAction action) : base (sender)
        {
            this.data = data;
            this.action = action;
        }

        public object Data
        {
            get { return data; }
        }

        public DataAction Action
        {
            get { return action; }
        }
    }
}
