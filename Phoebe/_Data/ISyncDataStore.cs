using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using SQLite.Net;
using Toggl.Phoebe._Data.Models;

namespace Toggl.Phoebe._Data
{
    public interface ISyncDataStore
    {
        TableQuery<T> Table<T> () where T : CommonData, new();
        IReadOnlyList<ICommonData> Update (Action<ISyncDataStoreContext> worker);
        void WipeTables ();

        int GetQueueSize (string queueId);
        bool TryEnqueue (string queueId, string json);
        bool TryDequeue (string queueId, out string json);
        bool TryPeek (string queueId, out string json);
    }

    public interface ISyncDataStoreContext
    {
        void Put (ICommonData obj);
        void Delete (ICommonData obj);
        ICommonData GetByColumn (Type type, string colName, object colValue);
        IReadOnlyList<ICommonData> UpdatedItems { get; }
        SQLiteConnection Connection { get; }
    }
}

