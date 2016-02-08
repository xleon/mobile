using System;
using System.Collections.Generic;
using SQLite.Net;
using Toggl.Phoebe._Data.Models;

namespace Toggl.Phoebe._Data
{
    public interface ISyncDataStore
    {
        TableQuery<T> Table<T> () where T : CommonData;
        void UpdateQueue (Action<ISyncDataStoreQueue> worker);
        IReadOnlyList<DataSyncMsg> Update (DataDir dir, Action<ISyncDataStoreContext> worker);
    }

    public interface ISyncDataStoreContext
    {
        void Put<T> (T obj) where T : CommonData;
        void Delete<T> (T obj) where T : CommonData;
        IReadOnlyList<DataSyncMsg> Messages { get; }
    }

    public interface ISyncDataStoreQueue
    {
        int GetQueueSize (string queueId);
        bool TryEnqueue (string queueId, string json);
        bool TryDequeue (string queueId, out string json);
        bool TryPeekQueue (string queueId, out string json);
    }
}

