using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace Toggl.Phoebe.Data.Utils
{
    public sealed class PropertyChangeTracker : IDisposable
    {
        private readonly List<Listener> listeners = new List<Listener> ();

        ~PropertyChangeTracker ()
        {
            Dispose (false);
        }

        public void Dispose ()
        {
            Dispose (true);
            GC.SuppressFinalize (this);
        }

        private void Dispose (bool disposing)
        {
            if (disposing) {
                ClearAll ();
            }
        }

        public void MarkAllStale ()
        {
            foreach (var listener in listeners) {
                listener.Stale = true;
            }
        }

        public void Add (INotifyPropertyChanged observable, Action<string> callback)
        {
            var listener = listeners.FirstOrDefault (l => l.Observable == observable);
            if (listener == null) {
                listener = new Listener (observable);
                listeners.Add (listener);
            }

            listener.Callback = callback;
            listener.Stale = false;
        }

        public void ClearAll ()
        {
            foreach (var listener in listeners) {
                listener.Dispose ();
            }
            listeners.Clear ();
        }

        public void ClearStale ()
        {
            var stale = listeners.Where (l => l.Stale).ToList ();
            listeners.RemoveAll (l => l.Stale);
            foreach (var listener in stale) {
                listener.Dispose ();
            }
        }

        private sealed class Listener : IDisposable
        {
            private readonly INotifyPropertyChanged observable;

            public Listener (INotifyPropertyChanged observable)
            {
                this.observable = observable;
                observable.PropertyChanged += HandlePropertyChanged;
            }

            ~Listener ()
            {
                Dispose (false);
            }

            public void Dispose ()
            {
                Dispose (true);
                GC.SuppressFinalize (this);
            }

            private void Dispose (bool disposing)
            {
                if (disposing) {
                    observable.PropertyChanged -= HandlePropertyChanged;
                    Callback = null;
                }
            }

            public INotifyPropertyChanged Observable
            {
                get { return observable; }
            }

            public Action<string> Callback { get; set; }

            public bool Stale { get; set; }

            private void HandlePropertyChanged (object sender, PropertyChangedEventArgs e)
            {
                var cb = Callback;
                if (cb != null) {
                    cb (e.PropertyName);
                }
            }
        }
    }
}
