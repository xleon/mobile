using System;
using Android.Gms.Wearable;

namespace Toggl.Chandler
{
    public static class Common
    {
        public static readonly string StartTimeEntryPath = "/toggl/wear/start";
        public static readonly string StopTimeEntryPath = "/toggl/wear/stop";
        public static readonly string RestartTimeEntryPath = "/toggl/wear/restart";
        public static readonly string TimeEntryListPath = "/toggl/wear/data";
        public static readonly string RequestSyncPath = "/toggl/wear/sync/";

        public static readonly string TimeEntryListKey = "time_entry_list_key";
        public static readonly string SingleEntryKey = "time_entry_key";
    }

    public class SimpleTimeEntryData
    {
        private const string PropertyIdKey = "idKey";
        private const string PropertyIsRunningKey = "isRunningKey";
        private const string PropertyDescriptionKey = "descriptionKey";
        private const string PropertyProjectNameKey = "projectKey";
        private const string PropertyProjectColorKey = "projectColorKey";
        private const string PropertyStartTimeKey = "startTimeKey";
        private const string PropertyStopTimeKey = "stopTimeKey";

        private Guid id;
        private bool isRunning;
        private string description;
        private string project;
        private string projectColor;
        private DateTime startTime;
        private DateTime stopTime;

        private DataMap dataMap = new DataMap ();

        public SimpleTimeEntryData ()
        {
        }

        public SimpleTimeEntryData (DataMap map)
        {
            dataMap = map;

            id = map.ContainsKey (PropertyIdKey) ? Guid.Parse (dataMap.GetString (PropertyIdKey)) : Guid.Empty;
            isRunning = map.ContainsKey (PropertyIsRunningKey) && dataMap.GetBoolean (PropertyIsRunningKey);
            description = map.ContainsKey (PropertyDescriptionKey) ? dataMap.GetString (PropertyDescriptionKey) : String.Empty;
            project = map.ContainsKey (PropertyProjectNameKey) ? dataMap.GetString (PropertyProjectNameKey) : String.Empty;
            projectColor = map.ContainsKey (PropertyProjectColorKey) ? dataMap.GetString (PropertyProjectColorKey) : String.Empty;
            startTime = map.ContainsKey (PropertyStartTimeKey) ? DateTime.Parse (dataMap.GetString (PropertyStartTimeKey)) : DateTime.UtcNow;
            stopTime = map.ContainsKey (PropertyStopTimeKey) ? DateTime.Parse (dataMap.GetString (PropertyStopTimeKey)) : DateTime.UtcNow;
        }

        public DataMap DataMap
        {
            get {

                dataMap = new DataMap();

                dataMap.PutString (PropertyIdKey, Id.ToString());
                dataMap.PutBoolean (PropertyIsRunningKey, isRunning);
                dataMap.PutString (PropertyDescriptionKey, description);
                dataMap.PutString (PropertyProjectNameKey, project);
                dataMap.PutString (PropertyProjectColorKey, projectColor);
                dataMap.PutString (PropertyStartTimeKey, startTime.ToString());
                dataMap.PutString (PropertyStopTimeKey, stopTime.ToString());

                return dataMap;
            } set {
                dataMap = value;
            }
        }

        public Guid Id
        {
            get {
                return id;
            } set {
                id = value;
            }
        }

        public bool IsRunning
        {
            get {
                return isRunning;
            } set {
                isRunning = value;
            }
        }

        public string Description
        {
            get {
                return description;
            } set {
                description = value;
            }
        }

        public string Project
        {
            get {
                return project;
            } set {
                project = value;
            }
        }

        public string ProjectColor
        {
            get {
                return projectColor;
            } set {
                projectColor = value;
            }
        }

        public DateTime StartTime
        {
            get {
                return startTime;
            } set {
                startTime = value;
            }
        }

        public DateTime StopTime
        {
            get {
                return stopTime;
            } set {
                stopTime = value;
            }
        }

        public TimeSpan GetDuration ()
        {
            var stop = StopTime == DateTime.MinValue ? DateTime.UtcNow : StopTime;
            var duration = stop - StartTime;
            if (duration <=  TimeSpan.Zero) {
                duration = TimeSpan.Zero;
            }
            return duration;
        }
    }
}
