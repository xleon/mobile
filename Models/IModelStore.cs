using System;
using System.Linq.Expressions;
using System.Collections.Generic;

namespace Toggl.Phoebe.Models
{
    public interface IModelStore
    {
        T Get<T> (long id)
            where T : Model;

        Model Get (Type type, long id);

        IModelQuery<T> Query<T> (
            Expression<Func<T, bool>> expr = null,
            Func<IEnumerable<T>, IEnumerable<T>> filter = null)
            where T : Model, new();

        long GetLastId (Type type);

        void ModelChanged (Model model, string property);

        void Commit ();
    }
}
