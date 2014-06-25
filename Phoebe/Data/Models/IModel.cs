using System;
using System.ComponentModel;

namespace Toggl.Phoebe.Data.Models
{
    public interface IModel : INotifyPropertyChanged
    {
        Guid Id { get; }
    }
}
