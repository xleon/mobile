using System;
using System.ComponentModel;

namespace Toggl.Phoebe.Data.NewModels
{
    public interface IModel : INotifyPropertyChanged
    {
        Guid Id { get; }
    }
}
