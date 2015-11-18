using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;

namespace Toggl.Phoebe
{
    public class Feed<T> : IDisposable
    {
        event EventHandler<T> _handler;
        public event EventHandler Disposed;

        public Feed ()
        {
        }

        public IObservable<T> Observe ()
        {
            return Observable
                .FromEventPattern<T> (
                    addHandler: h => _handler += h,
                    removeHandler: h => _handler -= h
                )
                .Select (x => x.EventArgs);
        }

        public void Push (T data)
        {
            if (_handler != null) {
                _handler (this, data);
            }
        }

        /// <summary>
        /// This will automatically remove all observers and trigger Disposed event
        /// </summary>
        public void Dispose ()
        {
            _handler = null;
            if (Disposed != null)
                Disposed (this, new EventArgs ());
            Disposed = null;
        }
    }
}

