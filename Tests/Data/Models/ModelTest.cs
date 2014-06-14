using System;
using Toggl.Phoebe.Data.NewModels;

namespace Toggl.Phoebe.Tests.Data.Models
{
    public abstract class ModelTest<T> : Test
        where T : IModel
    {
    }
}
