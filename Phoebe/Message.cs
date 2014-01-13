using System;

namespace Toggl.Phoebe
{
    public abstract class Message
    {
        private readonly object sender;

        public Message (object sender)
        {
            this.sender = sender;
        }

        public object Sender {
            get { return sender; }
        }
    }
}
