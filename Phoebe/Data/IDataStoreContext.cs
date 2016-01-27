using SQLite.Net;
using System.Threading.Tasks;

namespace Toggl.Phoebe.Data
{
    public interface IDataStoreContext
    {
        T Put<T> (T obj) where T : class, new();

        bool Delete (object obj);

        SQLiteConnection Connection { get; }

        void Enqueue (string json);
        
        bool TryDequeue (out string json);
        
        bool TryPeekQueue (out string json);
    }
}
