using System;
using System.Collections.Generic;
using System.Linq;

namespace Toggl.Phoebe.Data
{
    internal class MemoryModelCache
    {
        private readonly Dictionary<Guid, WeakReference> idIndex = new Dictionary<Guid, WeakReference> ();
        private readonly Dictionary<long, WeakReference> remoteIdIndex = new Dictionary<long, WeakReference> ();

        public void Add (Model model)
        {
            var weak = new WeakReference (model);
            idIndex.Add (model.Id.Value, weak);
            if (model.RemoteId.HasValue)
                remoteIdIndex.Add (model.RemoteId.Value, weak);
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
            PurgeIndex (idIndex);
            PurgeIndex (remoteIdIndex);
        }

        public void UpdateRemoteId (Model model, long? oldValue, long? newValue)
        {
            WeakReference weak;
            if (model.Id == null || !idIndex.TryGetValue (model.Id.Value, out weak))
                return;
            if (oldValue.HasValue)
                remoteIdIndex.Remove (oldValue.Value);
            if (newValue.HasValue)
                remoteIdIndex.Add (newValue.Value, weak);
        }

        public T GetById<T> (Guid id)
            where T : Model
        {
            WeakReference weak;
            if (idIndex.TryGetValue (id, out weak)) {
                var inst = weak.Target as T;
                if (inst != null)
                    return inst;
                PurgeDead ();
            }
            return default(T);
        }

        public T GetByRemoteId<T> (long remoteId)
            where T : Model
        {
            WeakReference weak;
            if (remoteIdIndex.TryGetValue (remoteId, out weak)) {
                var inst = weak.Target as T;
                if (inst != null)
                    return inst;
                PurgeDead ();
            }
            return default(T);
        }

        public IEnumerable<T> All<T> ()
            where T : Model
        {
            return idIndex.Values.Select ((r) => r.Target as T)
                    .Where ((m) => m != null);
        }
    }
}

