using System;
using System.Linq.Expressions;
using Newtonsoft.Json;

namespace Toggl.Phoebe.Data.Models
{
    public class TimeEntryTagModel : IntermediateModel<TimeEntryModel, TagModel>
    {
        public static implicit operator TimeEntryModel (TimeEntryTagModel m)
        {
            return m.From;
        }

        public static implicit operator TagModel (TimeEntryTagModel m)
        {
            return m.To;
        }
    }
}
