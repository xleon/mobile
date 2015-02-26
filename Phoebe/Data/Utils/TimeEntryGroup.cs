using System;
using System.Collections.Generic;
using System.Linq;
using Toggl.Phoebe.Data.DataObjects;
using Toggl.Phoebe.Data.Models;
using Toggl.Phoebe.Net;
using XPlatUtils;

namespace Toggl.Phoebe.Data.Utils
{
    /// <summary>
    // Wrapper to manage groups of TimeEntryData objects
    // The class presents a TimeEntryModel (the last time entry added) to work correclty with
    // the Views created but actually manage a list of TimeEntryData
    /// </summary>
    public class TimeEntryGroup
    {
        private readonly List<TimeEntryData> dataObjects = new List<TimeEntryData> ();

        private TimeEntryModel model;

        public TimeEntryGroup (TimeEntryData data)
        {
            Add (data);
        }

        public TimeEntryModel Model
        {
            get {
                return model;
            }
        }

        public List<TimeEntryData> TimeEntryList
        {
            get {
                return dataObjects;
            }
        }

        public int Count
        {
            get {
                return dataObjects.Count;
            }
        }

        public string Description
        {
            get {
                return dataObjects.Last().Description;
            }
        }

        public Guid Id
        {
            get {
                return dataObjects.Last().Id;
            }
        }

        public TimeSpan Duration
        {
            get {
                TimeSpan duration = TimeSpan.Zero;
                foreach (var item in dataObjects) {
                    duration += GetDuration (item, Time.UtcNow);
                }
                return duration;
            }
        }

        public DateTime LastStartTime
        {
            get {
                return dataObjects.Last ().StartTime;
            }
        }

        public DateTime? StopTime
        {
            get {
                return dataObjects.Last ().StopTime;
            }
        }

        public TimeEntryState State
        {
            get {
                return dataObjects.Last().State;
            }
        }

        public void InitModel()
        {
            if (model == null) {
                model = (TimeEntryModel)dataObjects.Last();
            } else {
                model.Data = dataObjects.Last();
            }
        }

        public void Add (TimeEntryData data)
        {
            dataObjects.Add (data);
        }

        public void Update (TimeEntryData data)
        {
            dataObjects.UpdateData (data);
            Sort();
        }

        public void Delete (TimeEntryData data)
        {
            if (dataObjects.Contains<TimeEntryData> (data)) {
                dataObjects.Remove (data);
            } else {
                dataObjects.RemoveAll (d => d.Id == data.Id);
            }
        }

        public void Sort()
        {
            dataObjects.Sort ((a, b) => a.StartTime.CompareTo (b.StartTime));
        }

        public bool CanContains (TimeEntryData data)
        {
            return dataObjects.Last().IsGroupableWith (data);
        }

        public void Dispose()
        {
            if (model != null) {
                model = null;
            }

            if (dataObjects.Count > 0) {
                dataObjects.Clear();
            }
        }

        public string GetFormattedDuration ()
        {
            TimeSpan duration = Duration;
            string formattedString = duration.ToString (@"hh\:mm\:ss");
            var user = ServiceContainer.Resolve<AuthManager> ().User;

            if (user == null) {
                return formattedString;
            }

            if (user.DurationFormat == DurationFormat.Classic) {
                if (duration.TotalMinutes < 1) {
                    formattedString = duration.ToString (@"s\ \s\e\c");
                } else if (duration.TotalMinutes > 1 && duration.TotalMinutes < 60) {
                    formattedString = duration.ToString (@"mm\:ss\ \m\i\n");
                } else {
                    formattedString = duration.ToString (@"hh\:mm\:ss");
                }
            } else if (user.DurationFormat == DurationFormat.Decimal) {
                formattedString = String.Format ("{0:0.00} h", duration.TotalHours);
            }
            return formattedString;
        }

        private static TimeSpan GetDuration (TimeEntryData entryData, DateTime now)
        {
            if (entryData.StartTime == DateTime.MinValue) {
                return TimeSpan.Zero;
            }

            var duration = (entryData.StopTime ?? now) - entryData.StartTime;
            if (duration < TimeSpan.Zero) {
                duration = TimeSpan.Zero;
            }

            return duration;
        }
    }
}

