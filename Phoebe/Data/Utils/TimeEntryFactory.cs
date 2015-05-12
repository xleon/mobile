using System;
using System.Threading.Tasks;
using System.Linq;
using System.Collections.Generic;
using Toggl.Phoebe.Data.Models;
using Toggl.Phoebe.Data.Utils;

namespace Toggl.Phoebe.Data.Utils
{
    public static class TimeEntryFactory
    {
        public async static Task<ITimeEntryModel> Get(List<Guid> guids) {
            if (guids.Count <= 1) {
                return new TimeEntryModel (guids.First ());
            } else {
                var grp = new TimeEntryGroup ();
                await grp.BuildFromGuids (guids);
                return grp;
            }
        }
    }
}

