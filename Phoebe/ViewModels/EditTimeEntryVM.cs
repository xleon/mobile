using System;
using System.Collections.Generic;
using System.Linq;
using GalaSoft.MvvmLight;
using Toggl.Phoebe.Analytics;
using Toggl.Phoebe.Data;
using Toggl.Phoebe.Data.Models;
using Toggl.Phoebe.Helpers;
using Toggl.Phoebe.Reactive;
using XPlatUtils;
using System.Reactive.Linq;
using System.Threading;
using GalaSoft.MvvmLight.Views;

namespace Toggl.Phoebe.ViewModels
{
    public class EditTimeEntryVM : ViewModelBase, IDisposable
    {
        private readonly string ErrorTitle = "Error";
        private readonly string StartTimeError = "Start time should be earlier than stop time!";
        private readonly string StopTimeError = "Stop time should be after start time!";

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
                subscriptionState = StoreManager
                                    .Singleton
                                    .Observe(x => x.State.TimeEntries[timeEntryId])
                                    .ObserveOn(SynchronizationContext.Current)
                                    .Subscribe(newTimeEntryData =>
                {
                    richData = newTimeEntryData;
                });
            }

            subscriptionTimer = Observable.Timer(TimeSpan.FromMilliseconds(1000 - Time.Now.Millisecond),
                                                 TimeSpan.FromSeconds(1))
                                .ObserveOn(SynchronizationContext.Current)
                                .Subscribe(x => UpdateDuration());

            ServiceContainer.Resolve<ITracker> ().CurrentScreen = "Edit Time Entry";
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
                // TODO: check substring function for long times
                return TimeSpan.FromSeconds(richData.Data.GetDuration().TotalSeconds)
                       .ToString().Substring(0, 8);
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

        public bool IsRunning { get { return richData.Data.State == TimeEntryState.Running; } }
        public string Description { get { return richData.Data.Description ?? string.Empty; } }
        public bool IsBillable { get { return richData.Data.IsBillable; } }
        public bool IsPremium { get { return richData.Info.WorkspaceData.IsPremium; } }
        public Guid WorkspaceId { get { return richData.Data.WorkspaceId; } }
        public string ProjectName { get { return richData.Info.ProjectData.Name ?? string.Empty; } }
        public string TaskName { get { return richData.Info.TaskData.Name ?? string.Empty; } }
        public string ClientName { get { return richData.Info.ClientData.Name ?? string.Empty; } }
        public IReadOnlyList<string> Tags { get { return richData.Data.Tags; } }

        #endregion

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
                UpdateView(x => x.ChangeStartTime(x.StartTime + diffTime), nameof(Duration), nameof(StartDate), nameof(StopDate));

            ServiceContainer.Resolve<ITracker> ().CurrentScreen = "Change Start Time";
        }

        public async void ChangeTimeEntryStart(DateTime newStartTime)
        {
            if (await IsStartStopTimeCorrect(newStartTime, isStart: true))
                UpdateView(x => x.ChangeStartTime(newStartTime), nameof(Duration), nameof(StartDate), nameof(StopDate));

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

        public void ChangeDescription(string description)
        {
            UpdateView(x => x.Description = description, nameof(Description));
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
            // TODO Rx Asking.
            // Exclude sync (inherited) properties from comparison with old
            // data 'previousData'. Maybe a clear way to compare properties is needed.
            if (!previousData.Data.PublicInstancePropertiesEqual(richData.Data,
                    nameof(CommonData.ModifiedAt), nameof(CommonData.RemoteId),
                    nameof(CommonData.DeletedAt), nameof(CommonData.SyncState)))
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
            if (richData.Data.ProjectId == taskId)
            {
                return;
            }

            var taskRemoteId = taskId != Guid.Empty ? StoreManager.Singleton.AppState.Tasks [taskId].RemoteId : null;
            UpdateView(x =>
            {
                x.TaskId = taskId;
                x.TaskRemoteId = taskRemoteId;
            });
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
    }
}
