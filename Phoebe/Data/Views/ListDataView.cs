using System;
using System.Collections.Generic;
using System.Linq;

namespace Toggl.Phoebe.Data.Views
{
    public class ListDataView<T> : IDataView<T>
    {
        private readonly List<T> data;

        public ListDataView (IEnumerable<T> enumerable)
        {
            data = enumerable.ToList ();
        }

        public event EventHandler Updated;

        public void Reload ()
        {
        }

        public void LoadMore ()
        {
        }

        public IEnumerable<T> Data {
            get { return data; }
        }

        public long Count {
            get { return data.Count; }
        }

        public bool HasMore {
            get { return false; }
        }

        public bool IsLoading {
            get { return false; }
        }
    }
}
