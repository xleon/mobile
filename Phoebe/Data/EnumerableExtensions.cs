using System;
using System.Collections.Generic;
using System.Linq;
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
    }
}
