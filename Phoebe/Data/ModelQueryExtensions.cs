using System;
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
    }
}
