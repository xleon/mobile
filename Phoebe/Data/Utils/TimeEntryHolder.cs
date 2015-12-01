using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Toggl.Phoebe.Data.DataObjects;
using Toggl.Phoebe.Data.Models;
using XPlatUtils;

namespace Toggl.Phoebe.Data.Utils
{
    public interface IHolder : IEquatable<IHolder>
    {
    }

    // TODO: Check if this really needs to implement IDisposable
    public interface ITimeEntryHolder : IHolder, IDisposable
    {
        TimeEntryData Data { get; }
        TimeEntryInfo Info { get; }
        IList<string> Guids { get; }

        Task DeleteAsync();
        TimeSpan GetDuration();
        DateTime GetStartTime();
        bool Matches (TimeEntryData data);
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

        public TimeEntryHolder (TimeEntryData timeEntry)
        {
            if (timeEntry == null) {
                throw new ArgumentNullException ("timeEntry");
            }
            Data = new TimeEntryData (timeEntry);
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

        public void Dispose ()
        {
            Data = new TimeEntryData ();
            Info = null;
        }

        public DateTime GetStartTime()
        {
            return Data.StartTime;
        }

        public TimeSpan GetDuration()
        {
            return TimeEntryModel.GetDuration (Data, Time.UtcNow);
        }

        public async Task LoadAsync ()
        {
            Info = await TimeEntryInfo.LoadAsync (Data);
        }

        public async Task DeleteAsync()
        {
            await TimeEntryModel.DeleteTimeEntryDataAsync (Data);
        }
    }
}
