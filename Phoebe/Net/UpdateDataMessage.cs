using System;
using System.Collections.Generic;
using Toggl.Phoebe.Data.Json;

namespace Toggl.Phoebe.Net
{
    public class UpdateFinishedMessage
    {
        public readonly List<TimeEntryJson> JsonEntries;
        public readonly DateTime StartDate;
        public readonly DateTime EndDate;
        public readonly bool HadErrors;
        public readonly bool HadMore;

        public UpdateFinishedMessage (
            List<TimeEntryJson> jsonEntries, DateTime startDate, DateTime endDate,
            bool hadMore, bool hadErrors)
        {
            this.JsonEntries = jsonEntries;
            this.HadErrors = hadErrors;
            this.StartDate = startDate;
            this.EndDate = endDate;
            this.HadMore = hadMore;
        }
    }
}
