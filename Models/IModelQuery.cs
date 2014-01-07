using System;
using System.Linq.Expressions;
using System.Collections.Generic;

namespace Toggl.Phoebe.Models
{
    public interface IModelQuery<T> : IEnumerable<T>
        where T : Model, new()
    {
        IModelQuery<T> Where (Expression<Func<T, bool>> predicate);

        IModelQuery<T> OrderBy<U> (Expression<Func<T, U>> orderExpr, bool asc = true);

        IModelQuery<T> Take (int n);

        IModelQuery<T> Skip (int n);

        int Count ();
    }
}
