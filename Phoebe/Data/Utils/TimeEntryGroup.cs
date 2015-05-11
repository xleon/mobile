using System;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Toggl.Phoebe.Data.DataObjects;
using Toggl.Phoebe.Data.Models;
using Toggl.Phoebe.Net;
using XPlatUtils;
using TTask = System.Threading.Tasks.Task;

namespace Toggl.Phoebe.Data.Utils
{
    /// <summary>
    // Wrapper to manage groups of TimeEntryData objects
    // The class presents a TimeEntryModel (the last time entry added) to work correclty with
    // the Views created but actually manage a list of TimeEntryData
    /// </summary>
    public class TimeEntryGroup : ITimeEntryModel
    {
        private readonly List<TimeEntryData> dataObjects = new List<TimeEntryData> ();
        private TimeEntryModel model;


        public TimeEntryGroup (TimeEntryData data)
        {
            Add (data);
        }

        public TimeEntryGroup ()
        {

        }

        public async Task BuildFromGuids (List<Guid> guids)
        {
            var first = new TimeEntryModel (guids.First ());
            await first.LoadAsync ();
            Add (first.Data);
            foreach (var guid in guids.Skip (1)) {
                var mdl = new TimeEntryModel (guid);
                await mdl.LoadAsync ();
                UpdateIfPossible (mdl.Data);
            }
        }

        public TimeEntryModel Model
        {
            get {
                if (model == null) {
                    model = (TimeEntryModel)dataObjects.Last();
                    model.PropertyChanged += (sender, e) => {
                        if (PropertyChanged != null) {
                            PropertyChanged.Invoke (sender, e);
                        }
                    };
                } else {
                    if (!model.Data.Matches (dataObjects.Last())) {
                        model.Data = dataObjects.Last();
                    }
                }
                return model;
            }
        }

        public IList<string> Ids
        {
            get {
                return TimeEntryGuids.ToList ();
            }
        }

        public List<TimeEntryData> TimeEntryList
        {
            get {
                return dataObjects;
            }
        }

        public string[] TimeEntryGuids
        {
            get {
                return dataObjects.AsEnumerable ().Select (r => r.Id.ToString ()).ToArray ();
            }
        }

        public int Count
        {
            get {
                return dataObjects.Count;
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
                return dataObjects.Last().StartTime;
            }
        }

        public int DistinctDays
        {
            get {
                return dataObjects.GroupBy (e => e.StartTime.Date).Count();
            }
        }

        public void Add (TimeEntryData data)
        {
            dataObjects.Add (data);
        }

        public void Update (TimeEntryData data)
        {
            dataObjects.UpdateData (data);
            Sort ();
        }

        public void UpdateIfPossible (TimeEntryData entry)
        {
            if (CanContain (entry)) {
                Add (entry);
            }
        }

        public void Remove (TimeEntryData entry)
        {
            if (dataObjects.Contains<TimeEntryData> (entry)) {
                dataObjects.Remove (entry);
            } else {
                dataObjects.RemoveAll (d => d.Id == entry.Id);
            }
        }

        public void Sort()
        {
            dataObjects.Sort ((a, b) => a.StartTime.CompareTo (b.StartTime));
        }

        public bool CanContain (TimeEntryData data)
        {
            return dataObjects.Last().IsGroupableWith (data);
        }

        public bool Contains (TimeEntryData entry, out TimeEntryData existingTimeEntry)
        {
            foreach (var item in dataObjects)
                if (item.Matches (entry)) {
                    existingTimeEntry = item;
                    return true;
                }

            existingTimeEntry = null;
            return false;
        }

        public void Dispose()
        {
            model = null;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public TimeSpan GetDuration ()
        {
            return Duration;
        }

        public void SetDuration (TimeSpan value)
        {
        }

        public Task StartAsync ()
        {
            return Model.StartAsync ();
        }

        public Task StoreAsync ()
        {
            return Model.StoreAsync ();
        }

        public Task StopAsync ()
        {
            return Model.StopAsync ();
        }

        public async Task DeleteAsync ()
        {
            await TTask.Run (() => Parallel.ForEach (dataObjects, obj => {
                var m = new TimeEntryModel (obj);
                m.DeleteAsync();
            }));
            Dispose ();
        }

        public async Task SaveAsync ()
        {
            var dataStore = ServiceContainer.Resolve<IDataStore> ();
            await TTask.Run (() => Parallel.ForEach (dataObjects, obj => {
                Model<TimeEntryData>.MarkDirty (obj);
                dataStore.PutAsync (obj);
            }));
        }

        public async Task Apply (Func<TimeEntryModel, Task> action)
        {
            foreach (var obj in dataObjects) {
                await action (new TimeEntryModel (obj));
            }
            await SaveAsync();
        }

        public TimeEntryData Data
        {

            get {
                return Model.Data;
            }

            set {
                Model.Data = value;
            }
        }

        public Task<TimeEntryModel> ContinueAsync ()
        {
            return Model.ContinueAsync ();
        }

        public Task MapTagsFromModel (TimeEntryModel model)
        {
            return Model.MapTagsFromModel (model);
        }

        public Task MapMinorsFromModel (TimeEntryModel model)
        {
            return Model.MapMinorsFromModel (model);
        }

        public TimeEntryState State
        {
            get {
                return Model.State;
            } set {
                Model.State = value;
            }
        }

        public DateTime StartTime
        {
            get {
                return TimeEntryList.FirstOrDefault ().StartTime;
            } set {
                if (TimeEntryList.Count == 1) {
                    Model.StartTime = value;
                } else {
                    var startModel = new TimeEntryModel (TimeEntryList.FirstOrDefault ());
                    startModel.StartTime = value;
                }
            }
        }

        public DateTime? StopTime
        {
            get {
                return Model.StopTime;
            } set {
                Model.StopTime = value;
            }
        }

        public bool IsBillable
        {
            get {
                return Model.IsBillable;
            }

            set {
                foreach (var item in dataObjects) {
                    item.IsBillable = value;
                }

                SaveAsync ();
            }
        }

        public UserModel User
        {
            get {
                return Model.User;
            }

            set {
                foreach (var item in dataObjects) {
                    item.UserId = value.Id;
                }

                SaveAsync ();
            }
        }

        public WorkspaceModel Workspace
        {
            get {
                return Model.Workspace;
            } set {
                foreach (var item in dataObjects) {
                    item.WorkspaceId = value.Id;
                }

                SaveAsync ();
            }
        }

        public string Description
        {
            get {
                return dataObjects.Last().Description;
            }

            set {
                if (Description != value) {
                    foreach (var item in dataObjects) {
                        item.Description = value;
                    }

                    SaveAsync ();
                }
            }
        }

        public ProjectModel Project
        {
            get {
                return Model.Project;
            }

            set {
                foreach (var item in dataObjects) {
                    item.ProjectId = value.Id;
                }
            }
        }

        public Guid Id
        {
            get {
                return dataObjects.Last().Id;
            }
        }

        public TaskModel Task
        {
            get {
                return Model.Task;
            }

            set {
                foreach (var item in dataObjects) {
                    item.TaskId = value.Id;
                }

                SaveAsync ();
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

