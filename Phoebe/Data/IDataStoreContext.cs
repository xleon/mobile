using SQLite.Net;
using SQLite.Net.Async;
using System.Threading.Tasks;

namespace Toggl.Phoebe.Data
{
    public interface IDataStoreContext
    {
        Task<T> PutAsync<T> (T obj) where T : new();

        Task<bool> DeleteAsync (object obj);

        SQLiteAsyncConnection Connection { get; }
    }

    public interface IDataStoreContextSync
    {
        T Put<T> (T obj) where T : new();

        bool Delete (object obj);

        SQLiteConnection Connection { get; }
    }
}
