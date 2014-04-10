using System;
using System.Collections.Generic;

namespace Toggl.Phoebe.Data.Views
{
    /// <summary>
    /// Query view wraps IModelQuery and retrieves only required amount of data at once.
    /// </summary>
    public sealed class ModelQueryView<T> : IDataView<T>
        where T : Model, new()
    {
        private readonly IModelQuery<T> query;
        private readonly int batchSize;
        private readonly List<T> data = new List<T> ();

        public ModelQueryView (IModelQuery<T> query, int batchSize)
        {
            this.query = query;
            this.batchSize = batchSize;
            Reload ();
        }

        public event EventHandler Updated;

        public void Reload ()
        {
            data.Clear ();
            data.AddRange (query.Skip (data.Count).Take (batchSize));
            var count = query.Count ();
            HasMore = data.Count < count;
            OnUpdated ();
        }

        public void LoadMore ()
        {
            data.AddRange (query.Skip (data.Count).Take (batchSize));
            var count = query.Count ();
            HasMore = data.Count < count;
            OnUpdated ();
        }

        private void OnUpdated ()
        {
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

        public bool IsLoading {
            get { return false; }
        }
    }
}

