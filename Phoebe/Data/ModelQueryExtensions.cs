using System;
using Toggl.Phoebe.Data.Views;
using Toggl.Phoebe.Net;
using XPlatUtils;

namespace Toggl.Phoebe.Data
{
    public static class ModelQueryExtensions
    {
        public static IModelQuery<TimeEntryModel> ForCurrentUser (this IModelQuery<TimeEntryModel> q)
        {
            var authManager = ServiceContainer.Resolve<AuthManager> ();
            return q.Where ((entry) => entry.UserId == authManager.UserId);
        }

        public static IModelsView<T> ToView<T> (this IModelQuery<T> q, int batchSize = 25)
            where T : Model, new()
        {
            return new ModelQueryView<T> (q, batchSize);
        }
    }
}
