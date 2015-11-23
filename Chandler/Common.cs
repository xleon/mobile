using System;
using System.Threading.Tasks;
using Android.Gms.Wearable;

namespace Toggl.Chandler
{
    public static class Common
    {
        public static readonly string StartStopTimeEntryPath = "/toggl/wear/start";
        public static readonly string ContinueTimeEntryPath = "/toggl/wear/restart";
        public static readonly string TimeEntryListPath = "/toggl/wear/data";
        public static readonly string RequestSyncPath = "/toggl/wear/sync/";
        public static readonly string UserNotLoggedInPath = "/toggl/wear/login/";
        public static readonly string OpenHandheldPath = "/toggl/wear/startapp/";

        public static readonly string TimeEntryListKey = "time_entry_list_key";
        public static readonly string SingleEntryKey = "time_entry_key";

        public static byte[] GetBytes (string str)
        {
            byte[] bytes = new byte[str.Length * sizeof (char)];
            System.Buffer.BlockCopy (str.ToCharArray(), 0, bytes, 0, bytes.Length);
            return bytes;
        }

        public static string GetString (byte[] bytes)
        {   
            char[] chars = new char[bytes.Length / sizeof (char)];
            System.Buffer.BlockCopy (bytes, 0, chars, 0, bytes.Length);
            return new string (chars);
        }

        public static T Get<T>(this DataMap dataMap, string propertyName, T defaultValue)
        {
            var key = propertyName + "Key";
            if (!dataMap.ContainsKey(key)) {
                return defaultValue;
            }
            else {
                var t = typeof(T);
                if (t == typeof(bool)) {
                    return (T)(object)dataMap.GetBoolean(key);
                }
                else if (typeof(T) == typeof(string)) {
                    return (T)(object)dataMap.GetString(key);
                }
                else if (t == typeof(Guid)) {
                    var s = dataMap.GetString(key);
                    return (T)(object)Guid.Parse(s);
                }
                else if (t == typeof(DateTime)) {
                    var s = dataMap.GetString(key);
                    return (T)(object)DateTime.Parse(s);
                }
                else {
                    throw new NotSupportedException(typeof(T).Name);
                }
			}
        }

        public static void Put<T>(this DataMap dataMap, string propertyName, T value)
        {
            var key = propertyName + "Key";
            var t = typeof(T);
            if (t == typeof(bool)) {
                dataMap.PutBoolean(key, (bool)(object)value);
            }
            else if (typeof(T) == typeof(string)) {
                dataMap.PutString(key, (string)(object)value);
            }
            else if (t == typeof(Guid)) {
                dataMap.PutString(key, value.ToString());
            }
            else if (t == typeof(DateTime)) {
                dataMap.PutString(key, value.ToString());
            }
            else {
                throw new NotSupportedException(typeof(T).Name);
            }
        }

        /// <summary>
        /// Throws a TimeoutException if the task within the specified time (milliseconds)
        /// </summary>
        public static async Task TimedAwait(int timeout, Task task)
        {
            var firstTask = await Task.WhenAny(task, Task.Delay(timeout));
            if (firstTask != task)
                throw new TimeoutException();
        }
    }

    public class SimpleTimeEntryData
    {
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

            id = map.Get(nameof(id), Guid.Empty);
            isRunning = map.Get(nameof(isRunning), false);
            description = map.Get(nameof(description), string.Empty);
            project = map.Get(nameof(project), string.Empty);
            projectColor = map.Get(nameof(projectColor), string.Empty);
            startTime = map.Get(nameof(startTime), DateTime.UtcNow);
            stopTime = map.Get(nameof(stopTime), DateTime.UtcNow);
        }

        public DataMap DataMap
        {
            get {

                dataMap = new DataMap();

                dataMap.Put(nameof(id), id);
                dataMap.Put(nameof(isRunning), isRunning);
                dataMap.Put(nameof(description), description);
                dataMap.Put(nameof(project), project);
                dataMap.Put(nameof(projectColor), projectColor);
                dataMap.Put(nameof(startTime), startTime);
                dataMap.Put(nameof(stopTime), stopTime);

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
