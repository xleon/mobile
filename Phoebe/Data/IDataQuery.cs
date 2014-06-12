using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace Toggl.Phoebe.Data
{
    public interface IDataQuery<T>
        where T : new()
    {
        IDataQuery<T> Where (Expression<Func<T, bool>> predicate);

        IDataQuery<T> OrderBy<U> (Expression<Func<T, U>> orderExpr, bool asc = true);

        IDataQuery<T> Take (int n);

        IDataQuery<T> Skip (int n);

        Task<int> CountAsync ();

        Task<int> CountAsync (Expression<Func<T, bool>> predicate);

        Task<List<T>> QueryAsync ();

        Task<List<T>> QueryAsync (Expression<Func<T, bool>> predicate);
    }
}
