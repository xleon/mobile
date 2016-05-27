using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Views;
using Toggl.Phoebe.Analytics;
using Toggl.Phoebe.Data;
using Toggl.Phoebe.Data.Models;
using Toggl.Phoebe.Helpers;
using Toggl.Phoebe.Reactive;
using Toggl.Phoebe.ViewModels.Timer;
using XPlatUtils;

namespace Toggl.Phoebe.ViewModels
{
    public class EditTimeEntryVM : ViewModelBase, IDisposable
    {
        private readonly string ErrorTitle = "Error";
        private readonly string StartTimeError = "Start time should be earlier than stop time!";
        private readonly string StopTimeError = "Stop time should be after start time!";
        private readonly int LoadSuggestionsCharLimit = 2;
        private readonly int LoadSuggestionsResultsLimit = 10;

        private IDisposable subscriptionTimer, subscriptionState;
        private RichTimeEntry richData;
        private RichTimeEntry previousData;

        public EditTimeEntryVM(AppState appState, Guid timeEntryId)
        {
            IsManual = timeEntryId == Guid.Empty;

            if (IsManual)
            {
                richData = new RichTimeEntry(appState.GetTimeEntryDraft(), appState);
                UpdateView(x =>
                {
                    x.Tags = new List<string>(richData.Data.Tags);
                    x.StartTime = Time.UtcNow.AddMinutes(-5);
                    x.StopTime = Time.UtcNow;
                    x.State = TimeEntryState.Finished;
                });
                previousData = new RichTimeEntry(richData.Data.With(x => x.Tags = new List<string>()), appState);
            }
            else
            {
                richData = appState.TimeEntries[timeEntryId];
                previousData = new RichTimeEntry(richData.Data, richData.Info);
                // TODO Rx First ugly code or patch from
                // Unidirectional era. Why the DistinctUntilChanged doesn't
                // work correctly? We should manage RemoteId-LocalId in
                // a better way.
                subscriptionState = StoreManager
                                    .Singleton
                                    .Observe(x => x.State.TimeEntries [timeEntryId])
                                    .DistinctUntilChanged(x => x.Data.RemoteId)
                                    .ObserveOn(SynchronizationContext.Current)
                                    .Subscribe(syncedRichData =>
                {
                    if (!richData.Data.RemoteId.HasValue && syncedRichData.Data.RemoteId.HasValue)
                        richData = syncedRichData;
                });
            }

            subscriptionTimer = Observable.Timer(TimeSpan.FromMilliseconds(1000 - Time.Now.Millisecond),
                                                 TimeSpan.FromSeconds(1))
                                .ObserveOn(SynchronizationContext.Current)
                                .Subscribe(x => UpdateDuration());

            ServiceContainer.Resolve<ITracker> ().CurrentScreen = "Edit Time Entry";
        }


        public void UpdateEntry(TimeEntryData data)
        {
            UpdateView(x =>
            {
                x.Description = data.Description;
                x.ProjectRemoteId = data.ProjectRemoteId;
                x.ProjectId = data.ProjectId;
                x.WorkspaceId = data.WorkspaceId;
                x.WorkspaceRemoteId = data.WorkspaceRemoteId;
                x.IsBillable = data.IsBillable;
            }, nameof(Description) , nameof(ProjectName), nameof(ClientName), nameof(ProjectColorHex), nameof(IsPremium), nameof(IsBillable));
        }

        public void Dispose()
        {
            if (subscriptionState != null)
                subscriptionState.Dispose();
            subscriptionTimer.Dispose();
        }

        #region viewModel State properties
        public bool IsManual { get; private set; }

        public string Duration
        {
            get
            {
                return TimeEntryData.GetFormattedDuration(richData.Data.GetDuration());
            }
        }

        public DateTime StartDate
        {
            get
            {
                return richData.Data.StartTime == DateTime.MinValue
                       ? DateTime.UtcNow.AddMinutes(-1).ToLocalTime()
                       : richData.Data.StartTime.ToLocalTime();
            }
        }

        public DateTime StopDate
        {
            get
            {
                return richData.Data.StopTime.HasValue
                       ? richData.Data.StopTime.Value.ToLocalTime()
                       : DateTime.UtcNow.ToLocalTime();
            }
        }

        public string ProjectColorHex
        {
            get
            {
                return richData.Info.ProjectData.Id != Guid.Empty
                       ? ProjectData.HexColors[richData.Info.ProjectData.Color % ProjectData.HexColors.Length]
                       : ProjectData.HexColors[ProjectData.DefaultColor];
            }
        }

        public async Task<List<TimeEntryData>> LoadSuggestions(string description)
        {
            var entries = new List<TimeEntryData>();
            if (description != null && description.Length >= LoadSuggestionsCharLimit)
            {
                entries = await Task.Run(() =>
                {
                    var dataStore = ServiceContainer.Resolve<ISyncDataStore>();
                    return dataStore.Table<TimeEntryData>()
                           .OrderBy(r => r.StartTime)
                           .Where(r => r.State != TimeEntryState.New
                                  && r.DeletedAt == null
                                  && r.Description != null
                                  && r.Description.Contains(description))
                           .Take(LoadSuggestionsResultsLimit)
                           .ToList();
                });
            }
            return entries;
        }

        public bool IsRunning { get { return richData.Data.State == TimeEntryState.Running; } }
        public string Description { get { return richData.Data.Description ?? string.Empty; } }
        public bool IsBillable { get { return richData.Data.IsBillable; } }
        public bool IsPremium { get { return richData.Info.WorkspaceData.IsPremium; } }
        public Guid WorkspaceId { get { return richData.Data.WorkspaceId; } }
        public string ProjectName { get { return richData.Info.ProjectData.Name ?? string.Empty; } }
        public string TaskName { get { return richData.Info.TaskData.Name ?? string.Empty; } }
        public string ClientName { get { return richData.Info.ClientData.Name ?? string.Empty; } }
        public List<string> Tags { get { return richData.Data.Tags.ToList(); } }

        public List<TimeEntryData> SuggestionsCollection { get; private set; } = new List<TimeEntryData>();

        #endregion
        public async Task SuggestEntries(string desc)
        {
            SuggestionsCollection = await LoadSuggestions(desc);
        }

        public void ChangeProjectAndTask(Guid projectId, Guid taskId)
        {
            if (projectId == richData.Data.ProjectId &&
                    taskId == richData.Data.TaskId)
            {
                return;
            }

            if (projectId != Guid.Empty)
            {
                SetNewProject(projectId);
                SetNewTask(taskId);
            }
            else
            {
                SetEmptyProject();
            }
        }

        public void ChangeTimeEntryDuration(TimeSpan newDuration)
        {
            UpdateView(x => x.SetDuration(newDuration), nameof(Duration), nameof(StartDate), nameof(StopDate));
            ServiceContainer.Resolve<ITracker> ().CurrentScreen = "Change Duration";
        }

        public async void ChangeTimeEntryStart(TimeSpan diffTime)
        {
            // TODO Small experiment to show error Dialogs
            // from ViewModels. It will help in the future.
            // Thinking always with translation in mind.
            if (await IsStartStopTimeCorrect(richData.Data.StartTime + diffTime, isStart: true))
            {
                if (richData.Data.State != TimeEntryState.Running)
                    UpdateView(x => x.ChangeStartTime(x.StartTime + diffTime), nameof(Duration), nameof(StartDate), nameof(StopDate));
                else
                    UpdateView(x => x.ChangeStartTime(x.StartTime + diffTime), nameof(Duration), nameof(StartDate));
            }
            ServiceContainer.Resolve<ITracker> ().CurrentScreen = "Change Start Time";
        }

        public async void ChangeTimeEntryStart(DateTime newStartTime)
        {
            if (await IsStartStopTimeCorrect(newStartTime, isStart: true))
            {
                if (richData.Data.State != TimeEntryState.Running)
                    UpdateView(x => x.ChangeStartTime(newStartTime), nameof(Duration), nameof(StartDate), nameof(StopDate));
                else
                    UpdateView(x => x.ChangeStartTime(newStartTime), nameof(Duration), nameof(StartDate));
            }
            ServiceContainer.Resolve<ITracker> ().CurrentScreen = "Change Start Time";
        }

        public async void ChangeTimeEntryStop(TimeSpan diffTime)
        {
            if (await IsStartStopTimeCorrect(richData.Data.StopTime.Value + diffTime, isStart: false))
                UpdateView(x => x.ChangeStoptime(x.StopTime + diffTime), nameof(Duration), nameof(StopDate));

            ServiceContainer.Resolve<ITracker> ().CurrentScreen = "Change Stop Time";
        }

        public async void ChangeTimeEntryStop(DateTime newStopTime)
        {
            if (await IsStartStopTimeCorrect(newStopTime, isStart: false))
                UpdateView(x => x.ChangeStoptime(newStopTime), nameof(Duration), nameof(StopDate));

            ServiceContainer.Resolve<ITracker> ().CurrentScreen = "Change Stop Time";
        }

        public void ChangeTagList(IEnumerable<string> newTags)
        {
            UpdateView(x => x.Tags = newTags.ToList(), nameof(Tags));
        }

        public async Task ChangeDescription(string description)
        {
            UpdateView(x => x.Description = description, nameof(Description));
            SuggestEntries(description);
        }

        public void ChangeBillable(bool billable)
        {
            UpdateView(x => x.IsBillable = billable, nameof(IsBillable));
        }

        public void Delete()
        {
            RxChain.Send(new DataMsg.TimeEntriesRemove(richData.Data));
        }

        public void Save()
        {
            if (HasTimeEntryChanged(previousData.Data, richData.Data))
            {
                RxChain.Send(new DataMsg.TimeEntryPut(richData.Data, Tags));
                previousData = richData;
            }
        }

        private void UpdateView(Action<TimeEntryData> updater, params string[] changedProperties)
        {
            richData = new RichTimeEntry(richData.Data.With(updater), StoreManager.Singleton.AppState);
            foreach (var prop in changedProperties)
            {
                RaisePropertyChanged(prop);
            }
        }

        private void UpdateDuration()
        {
            if (richData.Data.State == TimeEntryState.Running)
            {
                RaisePropertyChanged(nameof(Duration));
            }
        }

        private void SetNewProject(Guid projectId)
        {
            if (richData.Data.ProjectId == projectId)
            {
                return;
            }

            var projectData = StoreManager.Singleton.AppState.Projects [projectId];
            UpdateView(x =>
            {
                x.ProjectRemoteId = projectData.RemoteId;
                x.ProjectId = projectData.Id;
                x.WorkspaceId = projectData.WorkspaceId;
                x.WorkspaceRemoteId = projectData.WorkspaceRemoteId;
                x.IsBillable = projectData.IsBillable;
            }, nameof(ProjectName), nameof(ClientName), nameof(ProjectColorHex), nameof(IsPremium), nameof(IsBillable));
        }

        private void SetEmptyProject()
        {
            if (richData.Data.ProjectId == Guid.Empty)
            {
                return;
            }

            UpdateView(x =>
            {
                x.ProjectRemoteId = null;
                x.ProjectId = Guid.Empty;
                x.TaskId = Guid.Empty;
                x.TaskRemoteId = null;
            }, nameof(ProjectName), nameof(ClientName), nameof(ProjectColorHex));
        }

        private void SetNewTask(Guid taskId)
        {
            if (richData.Data.TaskId == taskId)
            {
                return;
            }

            var taskRemoteId = taskId != Guid.Empty ? StoreManager.Singleton.AppState.Tasks [taskId].RemoteId : null;
            UpdateView(x =>
            {
                x.TaskId = taskId;
                x.TaskRemoteId = taskRemoteId;
            }, nameof(TaskName));
        }

        private async System.Threading.Tasks.Task<bool> IsStartStopTimeCorrect(DateTime newDateTime, bool isStart)
        {
            if (newDateTime >= richData.Data.StopTime && isStart)
            {
                await ServiceContainer.Resolve<IDialogService> ().ShowMessage(StartTimeError, ErrorTitle);
                RaisePropertyChanged(nameof(TimeEntryData.StartTime));
                return false;
            }
            else if (newDateTime <= richData.Data.StartTime && !isStart)
            {
                await ServiceContainer.Resolve<IDialogService> ().ShowMessage(StopTimeError, ErrorTitle);
                RaisePropertyChanged(nameof(TimeEntryData.StopTime));
                return false;
            }

            return true;
        }

        private bool HasTimeEntryChanged(ITimeEntryData previous, ITimeEntryData current)
        {
            if (previous.StartTime != current.StartTime)
                return true;
            if (previous.StopTime != current.StopTime)
                return true;
            if (previous.IsBillable != current.IsBillable)
                return true;
            if (previous.ProjectId != current.ProjectId)
                return true;
            if (previous.Description != current.Description)
            {
                if (string.IsNullOrEmpty(previous.Description) &&
                        string.IsNullOrEmpty(current.Description))
                    return false;
                return true;
            }
            if (!previous.Tags.SequenceEqual(current.Tags))
                return true;
            if (previous.TaskId != current.TaskId)
                return true;
            return false;
        }
    }
}
