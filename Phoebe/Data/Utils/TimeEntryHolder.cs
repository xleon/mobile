using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Toggl.Phoebe.Data.DataObjects;
using Toggl.Phoebe.Data.Models;

namespace Toggl.Phoebe.Data.Utils
{
    public interface IHolder : IEquatable<IHolder>
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
        Task LoadAsync (TimeEntryData data, ITimeEntryHolder previous);
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

        public TimeEntryHolder ()
        {
        }

        public async Task LoadAsync (TimeEntryData data, ITimeEntryHolder previous)
        {
            // Ignore previous
            Data = data;
            Info = await TimeEntryInfo.LoadAsync (data);
        }

        public bool Equals (IHolder obj)
        {
            var other = obj as TimeEntryHolder;
            return other != null && other.Data.Id == Data.Id;
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
