using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;
using GalaSoft.MvvmLight;
using PropertyChanged;
using Toggl.Phoebe.Analytics;
using Toggl.Phoebe.Data.DataObjects;
using Toggl.Phoebe.Data.Models;
using Toggl.Phoebe.Data.ViewModels;
using Toggl.Phoebe.Data.Views;
using XPlatUtils;

namespace Toggl.Phoebe.Data.ViewModels
{
    [ImplementPropertyChanged]
    public class EditTimeEntryViewModel : ViewModelBase, IDisposable
    {
        internal static readonly string DefaultTag = "mobile";

        private Timer durationTimer;
        private TimeEntryData data;
        private TimeEntryData initialState;
        private List<TagData> initialTagList;

        EditTimeEntryViewModel (TimeEntryData data, List<TagData> tagList)
        {
            this.data = data;

            // Save previous state.
            initialState = new TimeEntryData (data);
            initialTagList = new List<TagData> (tagList);

            durationTimer = new Timer ();
            durationTimer.Elapsed += DurationTimerCallback;

            TagList = tagList;
            IsManual = data.Id == Guid.Empty;

            if (IsManual) {
                data.StartTime = Time.UtcNow.AddMinutes (-5);
                data.StopTime = Time.UtcNow;
                data.State = TimeEntryState.Finished;
                initialTagList = new List<TagData> ();
            }

            UpdateView ();
            UpdateRelationships (data.ProjectId);

            ServiceContainer.Resolve<ITracker> ().CurrentScreen = "Edit Time Entry";
        }

        public static async Task<EditTimeEntryViewModel> Init (Guid timeEntryId)
        {
            TimeEntryData data;
            List<TagData> tagList;

            if (timeEntryId == Guid.Empty) {
                data = TimeEntryModel.GetDraft ();
                tagList = await GetDefaultTagList (data.WorkspaceId);
            } else {
                data = await TimeEntryModel.GetTimeEntryDataAsync (timeEntryId);
                tagList = await ServiceContainer.Resolve<IDataStore> ().GetTimeEntryTags (timeEntryId);;
            }

            return new EditTimeEntryViewModel (data, tagList);
        }

        public static async Task<EditTimeEntryViewModel> Init (TimeEntryData timeEntryData)
        {
            var tagList = await ServiceContainer.Resolve<IDataStore> ().GetTimeEntryTags (timeEntryData.Id);
            return new EditTimeEntryViewModel (timeEntryData, tagList);
        }

        public void Dispose ()
        {
            durationTimer.Elapsed -= DurationTimerCallback;
            durationTimer.Dispose ();
        }

        #region viewModel State properties
        public bool IsPremium { get; private set; }

        public bool IsRunning { get; private set; }

        public bool IsManual { get; private set; }

        public string Duration { get; private set; }

        public DateTime StartDate { get; private set; }

        public DateTime StopDate { get; private set; }

        public string ProjectName { get; private set; }

        public string TaskName { get; private set; }

        public string ProjectColorHex { get; private set; }

        public string ClientName { get; private set; }

        public string Description { get; set; }

        public List<TagData> TagList { get; private set; }

        public bool IsBillable { get; set; }

        public Guid WorkspaceId { get; private set; }

        public bool SyncError { get; private set; }

        #endregion

        public void SetProjectAndTask (Guid projectId, Guid taskId)
        {
            if (projectId == data.ProjectId && taskId == data.TaskId) {
                return;
            }

            if (taskId != Guid.Empty) {
                data.TaskId = taskId;
            } else {
                data.TaskId = null;
            }

            UpdateRelationships (projectId);
        }

        public void ChangeTimeEntryDuration (TimeSpan newDuration)
        {
            data = TimeEntryModel.SetDuration (data, newDuration);
            UpdateView ();
            ServiceContainer.Resolve<ITracker> ().CurrentScreen = "Change Duration";
        }

        public void ChangeTimeEntryStart (TimeSpan diffTime)
        {
            data = TimeEntryModel.ChangeStartTime (data, data.StartTime + diffTime);
            UpdateView ();
            ServiceContainer.Resolve<ITracker> ().CurrentScreen = "Change Start Time";
        }

        public void ChangeTimeEntryStop (TimeSpan diffTime)
        {
            data = TimeEntryModel.ChangeStoptime (data, data.StopTime + diffTime);
            UpdateView ();
            ServiceContainer.Resolve<ITracker> ().CurrentScreen = "Change Stop Time";
        }

        public void ChangeTagList (List<TagData> newTagList)
        {
            TagList = new List<TagData> (newTagList);
            RaisePropertyChanged (() => TagList);
        }

        public void AddTag (TagData tagData)
        {
            TagList.Add (tagData);
            RaisePropertyChanged (() => TagList);
        }

        public async Task SaveAsync ()
        {
            if (IsManual) {
                return;
            }

            data.IsBillable = IsBillable;
            data.Description = Description;

            if (!data.PublicInstancePropertiesEqual (initialState)) {
                data = await TimeEntryModel.PrepareForSync (data);
                data = await TimeEntryModel.SaveTimeEntryDataAsync (data).ConfigureAwait (false);
            }

            if (!AreEqual (initialTagList, TagList)) {
                await SaveTagRelationships (data, TagList).ConfigureAwait (false);
            }
        }

        public async Task SaveManualAsync ()
        {
            IsManual = false;
            await SaveAsync ();
        }

        private void UpdateView ()
        {
            // Ensure that this content runs in UI thread
            ServiceContainer.Resolve<IPlatformUtils> ().DispatchOnUIThread (() => {

                StartDate = data.StartTime == DateTime.MinValue ? DateTime.UtcNow.AddMinutes (-1).ToLocalTime () : data.StartTime.ToLocalTime ();
                StopDate = data.StopTime.HasValue ? data.StopTime.Value.ToLocalTime () : DateTime.UtcNow.ToLocalTime ();
                var duration = TimeEntryModel.GetDuration (data, Time.UtcNow);
                Duration = TimeSpan.FromSeconds (duration.TotalSeconds).ToString ().Substring (0, 8); // TODO: check substring function for long times
                Description = data.Description;
                WorkspaceId = data.WorkspaceId;

                if (data.State == TimeEntryState.Running && !IsRunning) {
                    IsRunning = true;
                    durationTimer.Start ();
                } else if (data.State != TimeEntryState.Running) {
                    IsRunning = false;
                    durationTimer.Stop ();
                }

                SyncError = (data.RemoteRejected || !data.RemoteId.HasValue);
            });
        }

        private void UpdateRelationships (Guid? projectId)
        {
            // Ensure that this content runs in UI thread
            ServiceContainer.Resolve<IPlatformUtils> ().DispatchOnUIThread (async () => {

                if (projectId != Guid.Empty) {
                    data.ProjectId = projectId;
                } else {
                    data.ProjectId = null;
                }

                if (data.ProjectId.HasValue) {
                    var project = await TimeEntryModel.GetProjectDataAsync (data.ProjectId.Value);
                    ProjectName = project.Name;
                    ProjectColorHex = ProjectModel.HexColors [project.Color % ProjectModel.HexColors.Length];

                    if (project.ClientId.HasValue) {
                        var client = await TimeEntryModel.GetClientDataAsync (project.ClientId.Value);
                        ClientName = client.Name;
                    } else {
                        ClientName = string.Empty;
                    }

                    if (data.TaskId.HasValue) {
                        var task = await TimeEntryModel.GetTaskDataAsync (data.TaskId.Value);
                        TaskName = task.Name;
                    } else {
                        TaskName = string.Empty;
                    }

                    // TODO: Workspace and Billable should change!
                    data.WorkspaceId = project.WorkspaceId;
                    data.IsBillable = project.IsBillable;
                    var workspace = await TimeEntryModel.GetWorkspaceDataAsync (project.WorkspaceId);
                    IsPremium = workspace.IsPremium;

                    WorkspaceId = data.WorkspaceId;
                    IsBillable = data.IsBillable;
                } else {
                    ProjectName = string.Empty;
                    ClientName = string.Empty;
                    TaskName = string.Empty;
                    ProjectColorHex = ProjectModel.HexColors [ProjectModel.DefaultColor];
                }
            });
        }

        private void DurationTimerCallback (object sender, ElapsedEventArgs e)
        {
            var duration = TimeEntryModel.GetDuration (data, Time.UtcNow);
            durationTimer.Interval = 1000 - duration.Milliseconds;

            // Update on UI Thread
            ServiceContainer.Resolve<IPlatformUtils> ().DispatchOnUIThread (() => {
                Duration = TimeSpan.FromSeconds (duration.TotalSeconds).ToString ().Substring (0, 8);
            });
        }

        private async Task<TimeEntryData> SaveTagRelationships (TimeEntryData timeEntry, List<TagData> newTagList)
        {
            // Create tag list.
            var dataStore = ServiceContainer.Resolve<IDataStore> ();
            var existingTagRelations = new List<TimeEntryTagData> ();

            var tags = await dataStore.Table<TimeEntryTagData> ()
                       .Where (r => r.TimeEntryId == timeEntry.Id && r.DeletedAt == null)
                       .ToListAsync();
            existingTagRelations.AddRange (tags);

            // Delete unused tag relations:
            var deleteTasks = existingTagRelations
                              .Where (oldTagRelation => newTagList.All (newTag => newTag.Id != oldTagRelation.TagId))
                              .Select (tagRelation => new TimeEntryTagModel (tagRelation).DeleteAsync ())
                              .ToList();

            // Create new tag relations:
            var createTasks = newTagList
                              .Where (newTag => existingTagRelations.All (oldTagRelation => oldTagRelation.TagId != newTag.Id))
            .Select (data => new TimeEntryTagModel { TimeEntry = new TimeEntryModel (timeEntry), Tag = new TagModel (data)} .SaveAsync ())
            .ToList();

            await Task.WhenAll (deleteTasks.Concat (createTasks));

            if (deleteTasks.Any<Task> () || createTasks.Any<Task> ()) {
                timeEntry = await TimeEntryModel.PrepareForSync (timeEntry);
                timeEntry = await TimeEntryModel.SaveTimeEntryDataAsync (timeEntry);
            }

            return timeEntry;
        }

        private static async Task<List<TagData>> GetDefaultTagList (Guid workspaceId)
        {
            if (!ServiceContainer.Resolve<ISettingsStore> ().UseDefaultTag) {
                return new List<TagData> ();
            }

            var dataStore = ServiceContainer.Resolve<IDataStore> ();
            var defaultTagList = await dataStore.Table<TagData> ()
                                 .Where (r => r.Name == DefaultTag && r.WorkspaceId == workspaceId && r.DeletedAt == null)
                                 .ToListAsync();

            if (defaultTagList.Count == 0) {
                defaultTagList = new List<TagData> ();
                var defaultTag = await dataStore.PutAsync (new TagData {
                    Name = DefaultTag,
                    WorkspaceId = workspaceId,
                });
                defaultTagList.Add (defaultTag);
            }
            return defaultTagList;
        }

        private bool AreEqual (List<TagData> list1, List<TagData> list2)
        {
            if (list1.Count == list2.Count) {
                foreach (var item in list1) {
                    if (list2.Count (t => t.Id == item.Id) != 1) {
                        return false;
                    }
                }
                return true;
            }
            return false;
        }
    }
}

