using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace TogglDoodle
{
    public interface IModelsView<T> : INotifyPropertyChanged
        where T : Model
    {
        // TODO: This should be observable somehow? Maybe create a custom ObservableCollection?
        IEnumerable<T> Models { get; }

        bool HasMore { get; }

        Task LoadMore ();
        // Optional
        long? TotalCount { get; }

        bool IsLoading { get; }

        bool HasError { get; }
        // TODO: Getters for this view's predicates
    }
}
