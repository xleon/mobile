using System;
using System.Threading;

namespace Toggl.Phoebe.Threading
{
    public sealed class ThreadWeakLocal<T> : IDisposable
        where T : class
    {
        private readonly ThreadLocal<WeakReference<T>> locals;
        private readonly Func<T> factory;

        public ThreadWeakLocal (Func<T> factory)
        {
            this.factory = factory;
            this.locals = new ThreadLocal<WeakReference<T>> (
                () => new WeakReference<T> (factory ()));
        }

        public void Dispose ()
        {
            locals.Dispose ();
        }

        public bool IsValueCreated {
            get { return locals.IsValueCreated; }
        }

        public T Value {
            get {
                var weak = locals.Value;
                T obj;
                if (!weak.TryGetTarget (out obj) || obj == default(T)) {
                    obj = factory ();
                    weak.SetTarget (obj);
                }
                return obj;
            }
            set {
                if (locals.IsValueCreated) {
                    locals.Value.SetTarget (value);
                } else {
                    locals.Value = new WeakReference<T> (value);
                }
            }
        }
    }
}
