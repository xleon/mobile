using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using GalaSoft.MvvmLight;
using PropertyChanged;
using Toggl.Phoebe._Data;
using Toggl.Phoebe._Reactive;
using Toggl.Phoebe.Analytics;
using Toggl.Phoebe._Data.Models;
using Toggl.Phoebe._ViewModels;
//using Toggl.Phoebe.Data.Views;
using XPlatUtils;
using Toggl.Phoebe._Helpers;

namespace Toggl.Phoebe._ViewModels
{
    [ImplementPropertyChanged]
    public class EditTimeEntryViewModel : ViewModelBase, IDisposable
    {
        internal static readonly string DefaultTag = "mobile";

        private TimerState timerState;
        private TimeEntryData model;
        private TimeEntryInfo modelInfo;
        private System.Timers.Timer durationTimer;

        private void Init ()
        {
            durationTimer = new System.Timers.Timer ();
            IsManual = model.Id == Guid.Empty;

            durationTimer.Elapsed += DurationTimerCallback;

            ServiceContainer.Resolve<ITracker> ().CurrentScreen = "Edit Time Entry";
            UpdateView ();
        }

        public EditTimeEntryViewModel (TimerState timerState, Guid workspaceId)
        {
			var tagList = GetDefaultTagList (timerState, workspaceId);
            this.timerState = timerState;
            this.model = new TimeEntryData {
                Id = Guid.NewGuid (),
                WorkspaceId = workspaceId,
                StartTime = Time.UtcNow.AddMinutes (-5),
                StopTime = Time.UtcNow,
                State = TimeEntryState.Finished,
                Tags = tagList.Select (x => x.Name).ToList ()
            };
            this.modelInfo = timerState.LoadTimeEntryInfo (model).With (tags: tagList);

            Init ();
        }

        public EditTimeEntryViewModel (TimerState timerState, ITimeEntryData timeEntry, TimeEntryInfo timeEntryInfo)
        {
            model = new TimeEntryData (timeEntry);
            modelInfo = timeEntryInfo;

            Init ();
        }

        public void Dispose ()
        {
            durationTimer.Elapsed -= DurationTimerCallback;
            durationTimer.Dispose ();
        }

        #region viewModel State properties
		public IEnumerable<TagData> TagList
        {
            get { return modelInfo.Tags; }
        }

        public bool IsPremium { get; private set; }

        public bool IsRunning { get; private set; }

        public bool IsManual { get; private set; }

        public string Duration { get; private set; }

        public DateTime StartDate { get; private set; }

        public DateTime StopDate { get; private set; }

        public string ProjectName { get; private set; }

        public string ClientName { get; private set; }

        public string Description { get; private set; }

        public bool IsBillable { get; private set; }

        public Guid WorkspaceId { get; private set; }

        #endregion

        public void SetProjectAndTask (Guid workspaceId, Guid projectId, Guid taskId)
        {
            // TODO: Check taskId == Guid.Empty if projectId == Guid.Empty?
            // TODO: What to do with tags if project has changed?

            model = new TimeEntryData (model) {
                WorkspaceId = workspaceId,
                ProjectId = projectId,
                TaskId = taskId
            };
            modelInfo = timerState.LoadTimeEntryInfo (model);

            UpdateView ();
        }

        public void ChangeTimeEntryDuration (TimeSpan newDuration)
        {
            model.SetDuration (newDuration);
            UpdateView ();
            ServiceContainer.Resolve<ITracker> ().CurrentScreen = "Change Duration";
        }

        public void ChangeTimeEntryStart (TimeSpan diffTime)
        {
            model.StartTime += diffTime;
            UpdateView ();
            ServiceContainer.Resolve<ITracker> ().CurrentScreen = "Change Start Time";
        }

        public void ChangeTimeEntryStop (TimeSpan diffTime)
        {
            model.StopTime += diffTime;
            if (diffTime.TotalSeconds > 0) {

                model.StartTime = model.StartTime.Truncate (TimeSpan.TicksPerMinute);
                model.StopTime = ((DateTime)model.StopTime).Truncate (TimeSpan.TicksPerMinute);
            }
            UpdateView ();

            ServiceContainer.Resolve<ITracker> ().CurrentScreen = "Change Stop Time";
        }

        public void ChangeTagList (List<TagData> newTagList)
        {
            modelInfo = modelInfo.With (tags: newTagList);
            RaisePropertyChanged (() => TagList);
        }

        public void AddTag (TagData tagData)
        {
            modelInfo = modelInfo.With (tags: modelInfo.Tags.Append (tagData).ToList ());
            RaisePropertyChanged (() => TagList);
        }

        public void Save ()
        {
            if (IsManual) {
                return;
            }

            model.IsBillable = IsBillable;
            model.Description = Description;

            RxChain.Send (new DataMsg.TagsPut (model, TagList));
        }

        public void SaveManual ()
        {
            IsManual = false;
            Save ();
        }

        private void UpdateView ()
        {
            // Ensure that this content runs in UI thread
            ServiceContainer.Resolve<IPlatformUtils> ().DispatchOnUIThread (() => {

                StartDate = model.StartTime == DateTime.MinValue ? DateTime.UtcNow.AddMinutes (-1).ToLocalTime () : model.StartTime.ToLocalTime ();
                StopDate = model.StopTime.HasValue ? model.StopTime.Value.ToLocalTime () : DateTime.UtcNow.ToLocalTime ();
                Duration = TimeSpan.FromSeconds (model.GetDuration ().TotalSeconds).ToString ().Substring (0, 8); // TODO: check substring function for long times
                Description = model.Description;
                ProjectName = modelInfo.ProjectData != null ? modelInfo.ProjectData.Name : string.Empty;
                IsBillable = model.IsBillable;
                IsPremium = modelInfo.WorkspaceData.IsPremium;
                WorkspaceId = modelInfo.WorkspaceData.Id;

                ClientName = modelInfo.ClientData != null ? modelInfo.ClientData.Name : string.Empty;

                if (model.State == TimeEntryState.Running && !IsRunning) {
                    IsRunning = true;
                    durationTimer.Start ();
                } else if (model.State != TimeEntryState.Running) {
                    IsRunning = false;
                    durationTimer.Stop ();
                }
            });
        }

        private void DurationTimerCallback (object sender, System.Timers.ElapsedEventArgs e)
        {
            var duration = model.GetDuration ();
            durationTimer.Interval = 1000 - duration.Milliseconds;

            // Update on UI Thread
            ServiceContainer.Resolve<IPlatformUtils> ().DispatchOnUIThread (() => {
                Duration = TimeSpan.FromSeconds (duration.TotalSeconds).ToString ().Substring (0, 8);
            });
        }

        private static List<TagData> GetDefaultTagList (TimerState timerState, Guid workspaceId)
        {
            if (!ServiceContainer.Resolve<Data.ISettingsStore> ().UseDefaultTag) {
                return new List<TagData> ();
            }

            var defaultTagList = timerState.Tags.Values.Where (
                r => r.Name == DefaultTag && r.WorkspaceId == workspaceId && r.DeletedAt == null).ToList ();

            if (defaultTagList.Count == 0) {
                defaultTagList = new List<TagData> ();
                var defaultTag = new TagData (workspaceId, DefaultTag);
				defaultTagList.Add (defaultTag);
                RxChain.Send (new DataMsg.TagPut (defaultTag));
            }
            return defaultTagList;
        }
    }
}

