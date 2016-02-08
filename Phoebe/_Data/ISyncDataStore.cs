using System;

namespace Toggl.Phoebe._Data
{
    public interface ISyncDataStore
    {
        void ExecuteInTransaction (Action<ISyncDataStoreContext> worker);
    }

    public interface ISyncDataStoreContext
    {
        bool Put (object obj);
        bool Delete (object obj);

        int GetQueueSize (string queueId);
        bool TryEnqueue (string queueId, string json);
        bool TryDequeue (string queueId, out string json);
        bool TryPeekQueue (string queueId, out string json);
    }
}

