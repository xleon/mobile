using System;
using SQLite.Net.Attributes;

namespace Toggl.Phoebe._Data.Models
{
    public class TimeCorrectionData
    {
        [Indexed]
        public DateTime MeasuredAt { get; set; }

        public long Correction { get; set; }
    }
}
