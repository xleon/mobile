using System;
using System.Collections.Generic;

namespace Toggl.Joey.UI.Utils
{
    /// <summary>
    /// Simple object pool. Not thread-safe.
    /// </summary>
    public class Pool<T>
        where T : class
    {
        private readonly Queue<T> instances = new Queue<T>();
        private readonly Func<T> factory;
        private readonly Action<T> reset;

        public Pool(Func<T> factory, Action<T> reset = null)
        {
            if (factory == null)
            {
                throw new ArgumentNullException("factory");
            }

            this.factory = factory;
            this.reset = reset;
        }

        public T Obtain()
        {
            if (instances.Count > 0)
            {
                return instances.Dequeue();
            }

            return factory();
        }

        public void Release(T inst)
        {
            if (reset != null)
            {
                reset(inst);
            }
            instances.Enqueue(inst);
        }

        public int Count
        {
            get { return instances.Count; }
            set
            {
                if (value > instances.Count)
                {
                    // Increase the size of the pool
                    var count = value - instances.Count;
                    for (var i = 0; i < count; i++)
                    {
                        instances.Enqueue(factory());
                    }
                }
                else if (value < instances.Count)
                {
                    // Trim the size of the pool
                    while (value < instances.Count)
                    {
                        var inst = instances.Dequeue();

                        var disposable = inst as IDisposable;
                        if (disposable != null)
                        {
                            disposable.Dispose();
                        }
                    }
                }
            }
        }
    }
}
