using System;

namespace Toggl.Phoebe.Data.Views
{
    public interface IView<T>
    {
        T Model { get; }

        bool IsLoading { get; }

        event EventHandler OnIsLoadingChanged;

        void Dispose ();
    }
}

