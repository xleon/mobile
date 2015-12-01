using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using PropertyChanged;
using Toggl.Phoebe.Data.DataObjects;
using Toggl.Phoebe.Data.Models;
using XPlatUtils;
using TTask = System.Threading.Tasks.Task;

namespace Toggl.Phoebe.Data.Utils
{
    /// <summary>
    // Wrapper to manage groups of TimeEntryData objects
    // The class presents a TimeEntryModel (the last time entry added) to work correclty with
    // the Views created but actually manage a list of TimeEntryData
    /// </summary>
    [DoNotNotify]
    public class TimeEntryGroup : ITimeEntryModel, ITimeHolder
    {
        private TimeEntryModel model;
        public List<TimeEntryData> TimeEntryList { get; }

        public TimeEntryGroup (TimeEntryData data)
        {
            TimeEntryList = new List<TimeEntryData> () { data };
        }

        public TimeEntryGroup (List<TimeEntryData> dataList)
        {
            TimeEntryList = dataList;
        }

        public bool Equals (IHolder obj)
        {
            var other = obj as TimeEntryGroup;
            return other != null && other.Id == Id;
        }

        public bool Matches (TimeEntryData data)
        {
            return TimeEntryList.Any (x => x.Id == data.Id);
        }

        public static async Task<IList<TimeEntryData>> GetTimeEntryDataList (IList<string> ids)
        {
            var store = ServiceContainer.Resolve<IDataStore> ();
            var list = new List<TimeEntryData> (ids.Count);

            foreach (var stringGuid in ids) {
                var guid = new Guid (stringGuid);
                var rows = await store.Table<TimeEntryData> ()
                           .Where (r => r.Id == guid && r.DeletedAt == null)
                           .ToListAsync();
                var data = rows.FirstOrDefault ();
                list.Add (data);
            }
            return list;
        }

        public bool IsRunning
        {
            get { return State == TimeEntryState.Running; }
        }

        public TimeSpan TotalDuration
        {
            get { return Duration; }
        }

        public TimeEntryModel Model
        {
            get {
                if (model == null) {
                    model = (TimeEntryModel)TimeEntryData;
                    model.PropertyChanged += (sender, e) => {
                        if (PropertyChanged != null) {
                            PropertyChanged.Invoke (sender, e);
                        }
                    };
                } else {
                    if (!model.Data.Matches (TimeEntryData)) {
                        model.Data = TimeEntryData;
                    }
                }
                return model;
            }
        }

        public IList<string> TimeEntryGuids
        {
            get {
                return TimeEntryList.AsEnumerable ().Select (r => r.Id.ToString ()).ToList ();
            }
        }

        public int Count
        {
            get {
                return TimeEntryList.Count;
            }
        }

        public TimeSpan Duration
        {
            get {
                TimeSpan duration = TimeSpan.Zero;
                foreach (var item in TimeEntryList) {
                    duration += TimeEntryModel.GetDuration (item, Time.UtcNow);
                }
                return duration;
            }
        }

        public DateTime LastStartTime
        {
            get {
                return TimeEntryData.StartTime;
            }
        }

        public int DistinctDays
        {
            get {
                return TimeEntryList.GroupBy (e => e.StartTime.Date).Count ();
            }
        }

        public void Update (TimeEntryData data)
        {
            TimeEntryList.UpdateData (data);
            Sort ();
        }

        public void UpdateIfPossible (TimeEntryData entry)
        {
            if (CanContain (entry)) {
                TimeEntryList.Add (entry);
            }
        }

        public void Remove (TimeEntryData entry)
        {
            if (TimeEntryList.Contains<TimeEntryData> (entry)) {
                TimeEntryList.Remove (entry);
            } else {
                TimeEntryList.RemoveAll (d => d.Id == entry.Id);
            }
        }

        public void Sort ()
        {
            TimeEntryList.Sort ((a, b) => a.StartTime.CompareTo (b.StartTime));
        }

        public bool CanContain (TimeEntryData data)
        {
            return TimeEntryData.IsGroupableWith (data);
        }

        public bool Contains (TimeEntryData entry, out TimeEntryData existingTimeEntry)
        {
            foreach (var item in TimeEntryList)
                if (item.Matches (entry)) {
                    existingTimeEntry = item;
                    return true;
                }

            existingTimeEntry = null;
            return false;
        }

        public void Dispose ()
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
            var deleteTasks = new List<Task> ();
            foreach (var item in TimeEntryList) {
                var m = new TimeEntryModel (item);
                deleteTasks.Add (m.DeleteAsync ());
            }
            await TTask.WhenAll (deleteTasks);
            Dispose ();
        }

        public async Task SaveAsync ()
        {
            var dataStore = ServiceContainer.Resolve<IDataStore> ();
            var saveTasks = new List<Task> ();
            foreach (var item in TimeEntryList) {
                saveTasks.Add (dataStore.PutAsync (item));
            }
            await TTask.WhenAll (saveTasks);
        }

        public void Touch ()
        {
            for (int i = 0; i < TimeEntryList.Count; i++) {
                var newData = new TimeEntryData (TimeEntryList[i]); ;
                Model<TimeEntryData>.MarkDirty (newData);
                TimeEntryList[i] = newData;
            }
        }

        public TimeEntryData Data
        {
            get {
                return TimeEntryData;
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

        public TimeEntryData TimeEntryData
        {
            get { return TimeEntryData; }
        }

        public TimeEntryState State
        {
            get {
                return TimeEntryData.State;
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
                return TimeEntryData.IsBillable;
            }

            set {
                foreach (var item in TimeEntryList) {
                    item.IsBillable = value;
                }
                Touch ();
            }
        }

        public UserModel User
        {
            get {
                return Model.User;
            }

            set {
                foreach (var item in TimeEntryList) {
                    item.UserId = value.Id;
                }
                Touch ();
            }
        }

        public WorkspaceModel Workspace
        {
            get {
                return Model.Workspace;
            } set {
                foreach (var item in TimeEntryList) {
                    item.WorkspaceId = value.Id;
                }
                Touch ();
            }
        }

        public string Description
        {
            get {
                return TimeEntryData.Description;
            }

            set {
                if (string.IsNullOrEmpty (Description) && string.IsNullOrEmpty (value)) {
                    return;
                }

                if (Description != value) {
                    foreach (var item in TimeEntryList) {
                        item.Description = value;
                    }
                    Touch ();
                }
            }
        }

        public ProjectModel Project
        {
            get {
                return Model.Project;
            }

            set {
                foreach (var item in TimeEntryList) {
                    if (value != null) {
                        item.ProjectId = value.Id;
                    } else {
                        item.ProjectId = null;
                    }
                }
                Touch ();
            }
        }

        public Guid Id
        {
            get {
                return TimeEntryData.Id;
            }
        }

        public TaskModel Task
        {
            get {
                return Model.Task;
            }

            set {
                foreach (var item in TimeEntryList) {
                    item.TaskId = value.Id;
                }
                Touch ();
            }
        }

        public TTask LoadAsync ()
        {
            return Model.LoadAsync ();
        }

        public Task<int> GetNumberOfTagsAsync ()
        {
            var store = ServiceContainer.Resolve<IDataStore> ();
            return store.Table<TimeEntryTagData> ()
                   .Where (t => t.TimeEntryId == Id)
                   .CountAsync ();
        }
    }
}

