using System;
using SQLite.Net.Attributes;

namespace Toggl.Phoebe.Data.DataObjects
{
    public class TimeCorrectionData
    {
        [Indexed]
        public DateTime MeasuredAt { get; set; }

        public long Correction { get; set; }
    }
}
