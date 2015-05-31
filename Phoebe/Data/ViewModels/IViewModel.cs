using System;

namespace Toggl.Phoebe.Data.ViewModels
{
    public interface IViewModel<T>
    {
        T Model { get; }

        bool IsLoading { get; }

        event EventHandler OnIsLoadingChanged;

        void Dispose ();
    }
}

