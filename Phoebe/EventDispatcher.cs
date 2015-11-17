using System;

namespace Toggl.Phoebe
{
    public class EventDispatcher<T>
    {
        public event EventHandler<T> Handler;

        public EventDispatcher ()
        {
        }

        public void Dispatch (T data)
        {
            if (Handler != null) {
                Handler (this, data);
            }
        }
    }
}

