using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Toggl.Phoebe.Data.DataObjects;
using Toggl.Phoebe.Data.Models;

namespace Toggl.Phoebe.Data.Utils
{
    public interface IHolder : IDiffComparable
    {
    }

    public interface ITimeEntryHolder : IHolder
    {
        TimeEntryData Data { get; }
        TimeEntryInfo Info { get; }
        IList<string> Guids { get; }

        Task DeleteAsync ();
        Task LoadInfoAsync ();
        TimeSpan GetDuration ();
        DateTime GetStartTime ();
        bool IsAffectedByPut (TimeEntryData data);
        ITimeEntryHolder UpdateOrDelete (TimeEntryData data, out bool isAffectedByDelete);
    }

    public class TimeEntryHolder : ITimeEntryHolder
    {
        public TimeEntryData Data { get; private set; }
        public TimeEntryInfo Info { get; private set; }

        public IList<string> Guids
        {
            get {
                return new List<string>() { Data.Id.ToString() };
            }
        }

        public TimeEntryHolder (TimeEntryData data)
        {
            Data = data;
        }

        public async Task LoadInfoAsync ()
        {
            Info = await TimeEntryInfo.LoadAsync (Data);
        }

        public ITimeEntryHolder UpdateOrDelete (TimeEntryData data, out bool isAffectedByDelete)
        {
            isAffectedByDelete = data.Id == Data.Id;
            return null; // Always delete if affected
        }

        public DiffComparison Compare (IDiffComparable other)
        {
            if (object.ReferenceEquals (this, other)) {
                return DiffComparison.Same;
            } else {
                var other2 = other as TimeEntryHolder;
                return other2 != null && other2.Data.Id == Data.Id
                       ? DiffComparison.Update : DiffComparison.Different;
            }
        }

        public bool IsAffectedByPut (TimeEntryData data)
        {
            return data.Id == Data.Id;
        }

        public DateTime GetStartTime()
        {
            return Data.StartTime;
        }

        public TimeSpan GetDuration()
        {
            return TimeEntryModel.GetDuration (Data, Time.UtcNow);
        }

        public async Task DeleteAsync()
        {
            await TimeEntryModel.DeleteTimeEntryDataAsync (Data);
        }

        public override string ToString ()
        {
            return string.Format ("[{0:MM/dd HH:mm}, Id={1}]", Data.StartTime, Data.Id);
        }
    }
}
