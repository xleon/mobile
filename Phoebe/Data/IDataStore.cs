using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SQLite;

namespace Toggl.Phoebe.Data
{
    public interface IDataStore
    {
        Task<T> PutAsync<T> (T obj)
        where T : new();

        Task<bool> DeleteAsync (object obj);

        Task<T> ExecuteScalarAsync<T> (string query, params object[] args);

        Task<List<T>> QueryAsync<T> (string query, params object[] args)
        where T : new();

        IDataQuery<T> Table<T> ()
        where T : new();

        string GetTableName (Type mappingType);

        /// <summary>
        /// Executes a function on the SQLite background thread giving access to the raw SQLiteConnection.
        /// You should still use the Put and Delete methods provided instead of doing it via the SQLiteConnection
        /// as those methods guarantee that the appropriate messages are sent to the MessageBus.
        /// </summary>
        /// <returns>The task for the result of your function.</returns>
        /// <param name="worker">Worker function that is executed on the background thread.</param>
        /// <typeparam name="T">The 1st type parameter.</typeparam>
        Task<T> ExecuteInTransactionAsync<T> (Func<IDataStoreContext, T> worker);

        /// <summary>
        /// Executes a function on the SQLite background thread giving access to the raw SQLiteConnection.
        /// You should still use the Put and Delete methods provided instead of doing it via the SQLiteConnection
        /// as those methods guarantee that the appropriate messages are sent to the MessageBus.
        /// </summary>
        /// <returns>The task for the async transaction.</returns>
        /// <param name="worker">Worker function that is executed on the background thread.</param>
        Task ExecuteInTransactionAsync (Action<IDataStoreContext> worker);
    }
}