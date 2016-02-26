using SQLite.Net;

namespace Toggl.Phoebe.Data
{
    public interface IDataStoreContext
    {
        T Put<T> (T obj) where T : class, new();

        bool Delete (object obj);

        SQLiteConnection Connection { get; }
    }
}
