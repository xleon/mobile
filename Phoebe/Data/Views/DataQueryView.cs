using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SQLite.Net.Async;

namespace Toggl.Phoebe.Data.Views
{
    /// <summary>
    /// Query view wraps IDataQuery and retrieves only required amount of data at once.
    /// </summary>
    public sealed class DataQueryView<T> : IDataView<T>
        where T : class, new()
    {
        private readonly AsyncTableQuery<T> query;
        private readonly int batchSize;
        private readonly List<T> data = new List<T> ();

        public DataQueryView (AsyncTableQuery<T> query, int batchSize) {
            this.query = query;
            this.batchSize = batchSize;
            Reload ();
        }

        public event EventHandler Updated;

        public async void Reload () {
            if (IsLoading) {
                return;
            }

            data.Clear ();
            IsLoading = true;
            OnUpdated ();

            try {
                var queryTask = query.Skip (data.Count).Take (batchSize).ToListAsync ();
                var countTask = query.CountAsync ();
                await Task.WhenAll (queryTask, countTask);

                data.AddRange (queryTask.Result);

                var count = countTask.Result;
                HasMore = data.Count < count;
            } finally {
                IsLoading = false;
                OnUpdated ();
            }
        }

        public async void LoadMore () {
            if (IsLoading) {
                return;
            }

            IsLoading = true;
            OnUpdated ();

            try {
                var queryTask = query.Skip (data.Count).Take (batchSize).ToListAsync ();
                var countTask = query.CountAsync ();
                await Task.WhenAll (queryTask, countTask);

                data.AddRange (queryTask.Result);

                var count = countTask.Result;
                HasMore = data.Count < count;
            } finally {
                IsLoading = false;
                OnUpdated ();
            }
        }

        private void OnUpdated () {
            var handler = Updated;
            if (handler != null) {
                handler (this, EventArgs.Empty);
            }
        }

        public IEnumerable<T> Data {
            get { return data; }
        }

        public long Count {
            get { return data.Count; }
        }

        public bool HasMore { get; private set; }

        public bool IsLoading { get; private set; }
    }
}
