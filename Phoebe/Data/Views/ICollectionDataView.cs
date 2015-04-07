using System.Collections.Generic;
using System.Collections.Specialized;
using System;

namespace Toggl.Phoebe.Data.Views
{
    public interface ICollectionDataView<T> : INotifyCollectionChanged
    {
        IEnumerable<T> Data { get; }

        long Count { get; }

        bool HasMore { get; }

        void Reload ();

        void LoadMore ();

        bool IsLoading { get; }

        event EventHandler Updated;
    }
}

