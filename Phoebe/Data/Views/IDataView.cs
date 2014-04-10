using System;
using System.Collections.Generic;

namespace Toggl.Phoebe.Data.Views
{
    public interface IDataView<T>
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
