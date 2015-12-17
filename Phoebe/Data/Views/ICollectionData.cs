using System;
using System.Collections.Generic;
using System.Collections.Specialized;

namespace Toggl.Phoebe.Data.Views
{
    public interface ICollectionData<T> : INotifyCollectionChanged, IDisposable
    {
        IEnumerable<T> Data { get; }

        int Count { get; }
    }
}