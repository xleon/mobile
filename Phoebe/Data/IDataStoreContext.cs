using SQLite.Net;

namespace Toggl.Phoebe.Data
{
    public interface IDataStoreContext
    {
        T Put<T> (T obj) where T : new();

        bool Delete (object obj);

        SQLiteConnectionWithLock Connection { get; }
    }
}
