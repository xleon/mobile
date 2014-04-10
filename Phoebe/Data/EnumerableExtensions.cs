using System;
using System.Collections.Generic;
using System.Linq;
using Toggl.Phoebe.Data.Models;
using Toggl.Phoebe.Data.Views;
using Toggl.Phoebe.Net;
using XPlatUtils;

namespace Toggl.Phoebe.Data
{
    public static class EnumerableExtensions
    {
        public static IEnumerable<TimeEntryModel> ForCurrentUser (this IEnumerable<TimeEntryModel> q)
        {
            var authManager = ServiceContainer.Resolve<AuthManager> ();
            return q.Where ((entry) => entry.UserId == authManager.UserId);
        }

        public static IDataView<T> ToView<T> (this IEnumerable<T> enumerable, int batchSize = 25)
            where T : Model, new()
        {
            var query = enumerable as IModelQuery<T>;
            if (query != null) {
                return new ModelQueryView<T> (query, batchSize);
            } else {
                return new ListModelsView<T> (enumerable);
            }
        }
    }
}
