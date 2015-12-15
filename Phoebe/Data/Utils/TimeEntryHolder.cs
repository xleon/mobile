using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Toggl.Phoebe.Data.DataObjects;
using Toggl.Phoebe.Data.Models;

namespace Toggl.Phoebe.Data.Utils
{
    public interface IHolder : IDiffComparable
    {
    }

    public class DateHolder : IHolder
    {
        public DateTime Date { get; }
        public bool IsRunning { get; private set; }
        public TimeSpan TotalDuration { get; private set; }

        public DateHolder (DateTime date, IEnumerable<ITimeEntryHolder> timeHolders = null)
        {
            var totalDuration = TimeSpan.Zero;
            var dataObjects = timeHolders != null ? timeHolders.ToList () : new List<ITimeEntryHolder> ();
            foreach (var item in dataObjects) {
                totalDuration += item.GetDuration ();
            }

            Date = date;
            TotalDuration = totalDuration;
            IsRunning = dataObjects.Any (g => g.Data.State == TimeEntryState.Running);
        }

        public DiffComparison Compare (IDiffComparable other)
        {
            var other2 = other as DateHolder;
            if (other2 == null || other2.Date != Date) {
                return DiffComparison.Different;
            } else {
                var same = other2.TotalDuration == TotalDuration && other2.IsRunning == IsRunning;
                return same ? DiffComparison.Same : DiffComparison.Updated;
            }
        }

        public override string ToString ()
        {
            return string.Format ("Date {0:dd/MM}", Date);
        }
    }

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

        public override string ToString ()
        {
            return string.Format ("[{0:MM/dd HH:mm}]", Data.StartTime);
        }
    }
}
