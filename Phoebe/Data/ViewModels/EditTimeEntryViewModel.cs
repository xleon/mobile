using System;
using System.Collections.Generic;
using System.ComponentModel;
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

        private TimeEntryModel model;
        private Timer durationTimer;

        EditTimeEntryViewModel (TimeEntryModel model, List<TagData> tagList)
        {
            this.model = model;
            durationTimer = new Timer ();
            TagList = tagList;

            model.PropertyChanged += OnPropertyChange;
            durationTimer.Elapsed += DurationTimerCallback;

            ServiceContainer.Resolve<ITracker> ().CurrentScreen = "Edit Time Entry";
            UpdateView ();
        }

        public static async Task<EditTimeEntryViewModel> Init (Guid timeEntryId)
        {
            var model = new TimeEntryModel (timeEntryId);
            await model.LoadAsync ();

            var tagList = await ServiceContainer.Resolve<IDataStore> ().GetTimeEntryTags (timeEntryId);;

            // If the entry is new, setup it a bit.
            if (model.State == TimeEntryState.New) {
                model.StartTime = Time.UtcNow.AddMinutes (-5);
                model.StopTime = Time.UtcNow;
                tagList = await GetDefaultTagList (model.Workspace.Id);
            }

            return new EditTimeEntryViewModel (model, tagList);
        }

        public void Dispose ()
        {
            durationTimer.Elapsed -= DurationTimerCallback;
            durationTimer.Dispose ();

            model.PropertyChanged -= OnPropertyChange;
            model = null;
        }

        #region viewModel State properties
        public bool IsPremium { get; set; }

        public bool IsRunning { get; set; }

        public bool IsManual { get; set; }

        public string Duration { get; set; }

        public DateTime StartDate { get; set; }

        public DateTime StopDate { get; set; }

        public string ProjectName { get; set; }

        public string ClientName { get; set; }

        public string Description { get; set; }

        public List<TagData> TagList { get; set; }

        public bool IsBillable { get; set; }

        public Guid WorkspaceId { get; set; }

        #endregion

        public async Task SetProjectAndTask (Guid projectId, Guid taskId)
        {
            if (projectId == Guid.Empty) {
                model.Project = null;
                model.Task = null;
            } else {
                var projectModel = new ProjectModel (projectId);
                await projectModel.LoadAsync ();

                model.Project = projectModel;
                model.Workspace = new WorkspaceModel (projectModel.Workspace);
                model.Task = taskId != Guid.Empty ? new TaskModel (taskId) : null;
            }
            UpdateView ();
        }

        public void ChangeTimeEntryDuration (TimeSpan newDuration)
        {
            model.SetDuration (newDuration);
            ServiceContainer.Resolve<ITracker> ().CurrentScreen = "Change Duration";
        }

        public void ChangeTimeEntryStart (TimeSpan diffTime)
        {
            model.StartTime += diffTime;
            ServiceContainer.Resolve<ITracker> ().CurrentScreen = "Change Start Time";
        }

        public void ChangeTimeEntryStop (TimeSpan diffTime)
        {
            model.StopTime += diffTime;
            if (diffTime.TotalSeconds > 0) {

                model.StartTime = model.StartTime.Truncate (TimeSpan.TicksPerMinute);
                model.StopTime = ((DateTime)model.StopTime).Truncate (TimeSpan.TicksPerMinute);
            }

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

        public async Task SaveModel ()
        {
            if (IsManual) {
                return;
            }

            model.IsBillable = IsBillable;
            model.Description = Description;

            await SaveTagRelationships (TagList);
            await model.SaveAsync ();
        }

        public async Task SaveModelManual ()
        {
            model.IsBillable = IsBillable;
            model.Description = Description;
            model.State = TimeEntryState.Finished;

            await SaveTagRelationships (TagList);
            await model.SaveAsync ();
        }

        private void OnPropertyChange (object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "Data" ||
                    e.PropertyName == "StartTime" ||
                    e.PropertyName == "Duration" ||
                    e.PropertyName == "StopTime") {
                UpdateView ();
            }
        }

        private void UpdateView ()
        {
            // Ensure that this content runs in UI thread
            ServiceContainer.Resolve<IPlatformUtils> ().DispatchOnUIThread (() => {

                StartDate = model.StartTime == DateTime.MinValue ? DateTime.UtcNow.AddMinutes (-1).ToLocalTime () : model.StartTime.ToLocalTime ();
                StopDate = model.StopTime.HasValue ? model.StopTime.Value.ToLocalTime () : DateTime.UtcNow.ToLocalTime ();
                Duration = TimeSpan.FromSeconds (model.GetDuration ().TotalSeconds).ToString ().Substring (0, 8); // TODO: check substring function for long times
                Description = model.Description;
                ProjectName = model.Project != null ? model.Project.Name : string.Empty;
                IsBillable = model.IsBillable;
                IsPremium = model.Workspace.IsPremium;
                WorkspaceId = model.Workspace.Id;
                IsManual = model.State == TimeEntryState.New;

                if (model.Project != null) {
                    ClientName = model.Project.Client != null ? model.Project.Client.Name : string.Empty;
                } else {
                    ClientName = string.Empty;
                }

                if (model.State == TimeEntryState.Running && !IsRunning) {
                    IsRunning = true;
                    durationTimer.Start ();
                } else if (model.State != TimeEntryState.Running) {
                    IsRunning = false;
                    durationTimer.Stop ();
                }
            });
        }

        private void DurationTimerCallback (object sender, ElapsedEventArgs e)
        {
            var duration = model.GetDuration ();
            durationTimer.Interval = 1000 - duration.Milliseconds;

            // Update on UI Thread
            ServiceContainer.Resolve<IPlatformUtils> ().DispatchOnUIThread (() => {
                Duration = TimeSpan.FromSeconds (duration.TotalSeconds).ToString ().Substring (0, 8);
            });
        }

        private async Task SaveTagRelationships (List<TagData> newTagList)
        {
            // Create tag list.
            var dataStore = ServiceContainer.Resolve<IDataStore> ();
            var existingTagRelations = new List<TimeEntryTagData> ();

            var tags = await dataStore.Table<TimeEntryTagData> ()
                       .Where (r => r.TimeEntryId == model.Id && r.DeletedAt == null)
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
            .Select (data => new TimeEntryTagModel { TimeEntry = model, Tag = new TagModel (data)} .SaveAsync ())
            .ToList();

            await Task.WhenAll (deleteTasks.Concat (createTasks));

            if (deleteTasks.Any<Task> () || createTasks.Any<Task> ()) {
                model.Touch (); // why it needs to be called?
            }
        }

        private static async Task<List<TagData>> GetDefaultTagList (Guid workspaceId)
        {
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
    }
}

