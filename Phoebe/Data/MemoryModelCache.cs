using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Toggl.Phoebe.Data
{
    internal class MemoryModelCache
    {
        private readonly ReaderWriterLockSlim cacheLock = new ReaderWriterLockSlim ();
        private readonly Dictionary<Guid, WeakReference> idIndex = new Dictionary<Guid, WeakReference> ();
        private readonly Dictionary<long, WeakReference> remoteIdIndex = new Dictionary<long, WeakReference> ();

        public void Add (Model model)
        {
            cacheLock.EnterWriteLock ();
            try {
                var weak = new WeakReference (model);
                idIndex.Add (model.Id.Value, weak);
                if (model.RemoteId.HasValue)
                    remoteIdIndex.Add (model.RemoteId.Value, weak);
            } finally {
                cacheLock.ExitWriteLock ();
            }
        }

        private void PurgeIndex<T> (Dictionary<T, WeakReference> index)
        {
            var keys = index.Where ((kvp) => !kvp.Value.IsAlive).Select ((kvp) => kvp.Key).ToList ();
            foreach (var key in keys) {
                index.Remove (key);
            }
        }

        private void PurgeDead ()
        {
            cacheLock.EnterWriteLock ();
            try {
                PurgeIndex (idIndex);
                PurgeIndex (remoteIdIndex);
            } finally {
                cacheLock.ExitWriteLock ();
            }
        }

        public void UpdateRemoteId (Model model, long? oldValue, long? newValue)
        {
            cacheLock.EnterWriteLock ();
            try {
                WeakReference weak;
                if (model.Id == null || !idIndex.TryGetValue (model.Id.Value, out weak))
                    return;
                if (oldValue.HasValue)
                    remoteIdIndex.Remove (oldValue.Value);
                if (newValue.HasValue)
                    remoteIdIndex.Add (newValue.Value, weak);
            } finally {
                cacheLock.ExitWriteLock ();
            }
        }

        public T GetById<T> (Guid id)
            where T : Model
        {
            var needsPurge = false;

            cacheLock.EnterReadLock ();
            try {
                WeakReference weak;
                if (idIndex.TryGetValue (id, out weak)) {
                    var inst = weak.Target as T;
                    if (inst != null) {
                        return inst;
                    } else {
                        needsPurge = true;
                    }
                }
            } finally {
                cacheLock.ExitReadLock ();
            }

            if (needsPurge) {
                PurgeDead ();
            }

            return default(T);
        }

        public T GetByRemoteId<T> (long remoteId)
            where T : Model
        {
            var needsPurge = false;

            cacheLock.EnterReadLock ();
            try {
                WeakReference weak;
                if (remoteIdIndex.TryGetValue (remoteId, out weak)) {
                    var inst = weak.Target as T;
                    if (inst != null) {
                        return inst;
                    } else {
                        needsPurge = true;
                    }
                }
            } finally {
                cacheLock.ExitReadLock ();
            }

            if (needsPurge) {
                PurgeDead ();
            }

            return default(T);
        }

        public IEnumerable<T> All<T> ()
            where T : Model
        {
            cacheLock.EnterReadLock ();
            try {
                return idIndex.Values.Select ((r) => r.Target as T)
                    .Where ((m) => m != null).ToList ();
            } finally {
                cacheLock.ExitReadLock ();
            }
        }
    }
}
