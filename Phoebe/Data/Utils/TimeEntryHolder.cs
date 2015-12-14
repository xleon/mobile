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

    // TODO: Check if this really needs to implement IDisposable
    public interface ITimeEntryHolder : IHolder
    {
        TimeEntryData Data { get; }
        TimeEntryInfo Info { get; }
        IList<string> Guids { get; }

        Task DeleteAsync ();
        TimeSpan GetDuration ();
        DateTime GetStartTime ();
        bool Matches (TimeEntryData data);
        Task LoadInfoAsync ();
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

        public DiffComparison Compare (IDiffComparable other)
        {
            if (object.ReferenceEquals (this, other)) {
                return DiffComparison.Same;
            } else {
                var other2 = other as TimeEntryHolder;
                return other2 != null && other2.Data.Id == Data.Id
                       ? DiffComparison.Updated : DiffComparison.Different;
            }
        }

        public bool Matches (TimeEntryData data)
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
    }
}
