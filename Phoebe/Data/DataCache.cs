using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Toggl.Phoebe.Data.DataObjects;
using XPlatUtils;

namespace Toggl.Phoebe.Data
{
    public sealed class DataCache : IDisposable
    {
        private readonly object syncRoot = new Object ();
        private readonly List<IEntry> registry = new List<IEntry> ();
        private readonly int sizeLimit;
        private readonly TimeSpan deadGracePeriod;
        private Subscription<DataChangeMessage> subscriptionDataChange;

        public DataCache () : this (1000, TimeSpan.FromMinutes (5))
        {
        }

        public DataCache (int sizeLimit, TimeSpan deadGracePeriod)
        {
            this.sizeLimit = sizeLimit;
            this.deadGracePeriod = deadGracePeriod;

            var bus = ServiceContainer.Resolve<MessageBus> ();
            subscriptionDataChange = bus.Subscribe<DataChangeMessage> (OnDataChange);
        }

        ~DataCache ()
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
                registry.Clear ();

                if (subscriptionDataChange != null) {
                    var bus = ServiceContainer.Resolve<MessageBus> ();
                    bus.Unsubscribe (subscriptionDataChange);
                    subscriptionDataChange = null;
                }
            }
        }

        private void OnDataChange (DataChangeMessage msg)
        {
            var data = msg.Data as CommonData;
            if (data == null) {
                return;
            }

            lock (syncRoot) {
                if (msg.Action == DataAction.Delete) {
                    // Clear the item from cache
                    registry.RemoveAll (e => EntryMatches (e, data));
                } else {
                    // Update the item in cache if it is cached
                    foreach (var entry in registry) {
                        if (EntryMatches (entry, data)) {
                            entry.Data.SetTarget (data);
                            entry.Touch ();
                        }
                    }
                }
            }
        }

        public async Task<T> GetAsync<T> (Guid id)
        where T : CommonData, new()
        {
            return (T)await GetAsync (typeof(T), id).ConfigureAwait (false);
        }

        public void Trim ()
        {
            lock (syncRoot) {
                // Make sure that the cache is in the allowed size
                if (registry.Count > sizeLimit * 1.1f) {
                    // Remove dead elements
                    registry.RemoveAll (IsDeadEntry);

                    if (registry.Count > sizeLimit) {
                        // Sort list by access time in descending order and trim excess
                        registry.Sort ((a, b) => b.AccessTime.CompareTo (a.AccessTime));

                        var startIdx = sizeLimit - 1;
                        registry.RemoveRange (startIdx, registry.Count - startIdx);
                    }
                } else {
                    // Purge all items that have been dead for more than the grace period
                    registry.RemoveAll (e => IsDeadEntry (e) && e.AccessTime < Time.UtcNow - deadGracePeriod);
                }
            }
        }

        public Task<CommonData> GetAsync (Type type, Guid id)
        {
            CommonData data;
            IEntry entry;

            lock (syncRoot) {
                entry = registry.FirstOrDefault (e => e.DataType == type && e.Id == id);
                if (entry != null) {
                    // Check that the data has not been collected already or is not in the process of loading
                    if (entry.Data.TryGetTarget (out data)) {
                        entry.Touch ();
                        return Task.FromResult (data);
                    }
                }

                if (entry == null) {
                    // Purge old entries before trying to create new ones
                    Trim ();

                    // Add the new entry
                    entry = (IEntry)Activator.CreateInstance (typeof(Entry<>).MakeGenericType (type), type, id);
                    registry.Add (entry);
                }

                return entry.LoadAsync ();
            }
        }

        public bool TryGetCached<T> (Guid id, out T data)
        where T : CommonData, new()
        {
            CommonData commonData;
            if (TryGetCached (typeof(T), id, out commonData)) {
                data = (T)commonData;
                return true;
            }
            data = null;
            return false;
        }

        public bool TryGetCached (Type type, Guid id, out CommonData data)
        {
            data = null;

            lock (syncRoot) {
                var entry = registry.FirstOrDefault (e => e.DataType == type && e.Id == id);
                if (entry == null) {
                    return false;
                }

                // Check that the data has not been collected already or is not in the process of loading
                if (!entry.Data.TryGetTarget (out data)) {
                    return false;
                }

                entry.Touch ();
                return true;
            }
        }

        private static bool IsDeadEntry (IEntry entry)
        {
            CommonData data;
            return !entry.IsLoading && !entry.Data.TryGetTarget (out data);
        }

        private static bool EntryMatches (IEntry entry, CommonData data)
        {
            return entry.Id == data.Id && entry.DataType == data.GetType ();
        }

        private interface IEntry
        {
            void Touch ();

            Task<CommonData> LoadAsync ();

            Type DataType { get; }

            Guid Id { get; }

            WeakReference<CommonData> Data { get; }

            DateTime AccessTime { get; }

            bool IsLoading { get; }
        }

        private class Entry<T> : IEntry
            where T : CommonData, new()
        {
            private TaskCompletionSource<CommonData> loadTCS;

            public Entry (Type dataType, Guid id) {
                DataType = dataType;
                Id = id;
                Data = new WeakReference<CommonData> (null);
            }

            public void Touch () {
                AccessTime = Time.UtcNow;
            }

            public Task<CommonData> LoadAsync () {
                if (loadTCS != null) {
                    return loadTCS.Task;
                }

                var tcs = loadTCS = new TaskCompletionSource<CommonData> ();
                Touch ();

                StartLoad ();

                return tcs.Task;
            }

            private async void StartLoad () {
                var tcs = loadTCS;
                CommonData data = null;

                try {
                    var dataStore = ServiceContainer.Resolve<IDataStore> ();
                    var rows = await dataStore.Table<T> ()
                               .Take (1)
                               .QueryAsync (r => r.Id == Id)
                               .ConfigureAwait (false);

                    data = rows.FirstOrDefault ();
                    if (data != null) {
                        Data.SetTarget (data);
                    }
                } finally {
                    loadTCS = null;
                    tcs.SetResult (data);
                }
            }

            public Type DataType { get; private set; }

            public Guid Id { get; private set; }

            public WeakReference<CommonData> Data { get; private set; }

            public DateTime AccessTime { get; private set; }

            public bool IsLoading {
                get { return loadTCS != null; }
            }
        }
    }
}
