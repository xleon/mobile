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
            dataObjects.Add (data);
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
                return (TimeSpan) (dataObjects.Last ().StopTime - dataObjects.First ().StartTime);
            }
        }

        public DateTime StartTime
        {
            get {
                return dataObjects.First ().StartTime;
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

        public bool Contains (TimeEntryData data)
        {
            return dataObjects.Last().IsGroupableWith (data);
        }

        public void InitModel()
        {
            model = (TimeEntryModel)dataObjects.Last();
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
    }
}

