using System;
using System.Linq.Expressions;
using System.Collections.Generic;

namespace Toggl.Phoebe.Data
{
    public interface IModelStore
    {
        T Get<T> (Guid id)
            where T : Model;

        Model Get (Type type, Guid id);

        T GetByRemoteId<T> (long id)
            where T : Model;

        Model GetByRemoteId (Type type, long id);

        IModelQuery<T> Query<T> (
            Expression<Func<T, bool>> expr = null,
            Func<IEnumerable<T>, IEnumerable<T>> filter = null)
            where T : Model, new();

        void Commit ();

        bool TryCommit ();
    }
}
