using System;
using System.Collections.Generic;
using Toggl.Phoebe.Data.DataObjects;
using XPlatUtils;

namespace Toggl.Phoebe.Data
{
    public static class ForeignRelationExtensions
    {
        public static IEnumerable<ForeignRelation> GetRelations (this CommonData data)
        {
            var manager = ServiceContainer.Resolve<ForeignRelationManager> ();
            return manager.GetRelations (data);
        }
    }
}
