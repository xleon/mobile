using SQLite.Net;
using System.Threading.Tasks;

namespace Toggl.Phoebe.Data
{
    public interface IDataStoreContext
    {
        T Put<T> (T obj) where T : new();

        bool Delete (object obj);

        SQLiteConnection Connection { get; }

        int GetQueueSize ();

        void Enqueue (string json);

        bool TryDequeue (out string json);

        bool TryPeekQueue (out string json);
    }
}
