using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Threading.Tasks;
using Toggl.Phoebe.Data;

namespace Toggl.Phoebe.Data.Views
{
    public interface IModelsView<T> : INotifyPropertyChanged
        where T : Model
    {
        IEnumerable<T> Models { get; }

        long Count { get; }

        long? TotalCount { get; }

        bool HasMore { get; }

        void Reload ();

        void LoadMore ();

        bool IsLoading { get; }

        bool HasError { get; }
    }
}
