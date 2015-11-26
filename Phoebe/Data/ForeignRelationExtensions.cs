using System.Collections.Generic;
using System.Threading.Tasks;
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

        public static Task<CommonData> ToListAsync (this ForeignRelation relation)
        {
            var manager = ServiceContainer.Resolve<ForeignRelationManager> ();
            return manager.ToListAsync (relation);
        }
    }
}
