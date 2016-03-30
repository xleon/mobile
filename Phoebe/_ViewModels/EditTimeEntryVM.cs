using System;
using System.Collections.Generic;
using System.Linq;
using GalaSoft.MvvmLight;
using Toggl.Phoebe.Analytics;
using Toggl.Phoebe._Data;
using Toggl.Phoebe._Data.Models;
using Toggl.Phoebe._Helpers;
using Toggl.Phoebe._Reactive;
using XPlatUtils;
using System.Reactive.Linq;

namespace Toggl.Phoebe._ViewModels
{
    public class EditTimeEntryVM : ViewModelBase, IDisposable
    {
        internal static readonly string DefaultTag = "mobile";

        private IDisposable subscriptionState;
        private AppState appState;
        private RichTimeEntry richData;
        private RichTimeEntry previousData;
        private System.Timers.Timer durationTimer;

        private void Init (AppState state, ITimeEntryData timeData, List<Guid> tagList)
        {
            durationTimer = new System.Timers.Timer ();
            durationTimer.Elapsed += DurationTimerCallback;

            appState = state;
            IsManual = timeData.Id == Guid.Empty;
            richData = new RichTimeEntry (timeData, state);

            UpdateView (x => {
                x.TagIds = tagList;
                if (IsManual) {
                    x.StartTime = Time.UtcNow.AddMinutes (-5);
                    x.StopTime = Time.UtcNow;
                    x.State = TimeEntryState.Finished;
                }
            });

            // Save previous state.
            previousData = IsManual
                           // Hack to force tag saving even if there're no other changes
                           ? new RichTimeEntry (richData.Data.With (x => x.TagIds = new List<Guid> ()), state)
                           : new RichTimeEntry (richData.Data, richData.Info);

            subscriptionState = StoreManager
                                .Singleton
                                .Observe (x => x.State)
                                .StartWith (state)
                                .Subscribe (s => appState = s);

            ServiceContainer.Resolve<ITracker> ().CurrentScreen = "Edit Time Entry";
        }

        public EditTimeEntryVM (AppState appState, Guid timeEntryId)
        {
            ITimeEntryData data;
            List<Guid> tagList;

            if (timeEntryId == Guid.Empty) {
                data = appState.GetTimeEntryDraft();
                tagList = GetDefaultTagList (appState, data).Select (x => x.Id).ToList();
            } else {
                var richTe = appState.TimeEntries[timeEntryId];
                data = richTe.Data;
                tagList = new List<Guid> (richTe.Data.TagIds);
            }

            Init (appState, data, tagList);
        }

        public EditTimeEntryVM (AppState appState, ITimeEntryData timeEntryData, List<Guid> tagList)
        {
            Init (appState, timeEntryData, tagList);
        }

        public void Dispose ()
        {
            subscriptionState.Dispose ();
            durationTimer.Elapsed -= DurationTimerCallback;
            durationTimer.Dispose ();
        }

        #region viewModel State properties
        public bool IsManual { get; private set; }

        public string Duration
        {
            get {
                // TODO: check substring function for long times
                return TimeSpan.FromSeconds (richData.Data.GetDuration ().TotalSeconds)
                       .ToString ().Substring (0, 8);
            }
        }

        public DateTime StartDate
        {
            get {
                return richData.Data.StartTime == DateTime.MinValue
                       ? DateTime.UtcNow.AddMinutes (-1).ToLocalTime ()
                       : richData.Data.StartTime.ToLocalTime ();
            }
        }

        public DateTime StopDate
        {
            get {
                return richData.Data.StopTime.HasValue
                       ? richData.Data.StopTime.Value.ToLocalTime ()
                       : DateTime.UtcNow.ToLocalTime ();
            }
        }

        public string ProjectColorHex
        {
            get {
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
        public IReadOnlyList<ITagData> TagList { get { return richData.Info.Tags; } }

        #endregion

        public void ChangeProjectAndTask (Guid projectId, Guid taskId)
        {
            long? remoteProjectId = null;
            long? remoteTaskId = null;

            if (projectId != Guid.Empty) {
                remoteProjectId = StoreManager.Singleton.AppState.Projects [projectId].RemoteId;
            }
            if (taskId != Guid.Empty) {
                remoteTaskId = StoreManager.Singleton.AppState.Tasks [taskId].RemoteId;
            }

            if (projectId != richData.Data.ProjectId || taskId != richData.Data.TaskId) {
                UpdateView (x => {
                    x.TaskRemoteId = remoteTaskId;
                    x.ProjectRemoteId = remoteProjectId;
                    x.ProjectId = projectId;
                    x.TaskId = taskId;
                }, nameof (ProjectName), nameof (ClientName));
            }
        }

        public void ChangeTimeEntryDuration (TimeSpan newDuration)
        {
            UpdateView (x => x.SetDuration (newDuration), nameof (Duration), nameof (StartDate), nameof (StopDate));
            ServiceContainer.Resolve<ITracker> ().CurrentScreen = "Change Duration";
        }

        public void ChangeTimeEntryStart (TimeSpan diffTime)
        {
            UpdateView (x => x.ChangeStartTime (x.StartTime + diffTime), nameof (Duration), nameof (StartDate), nameof (StopDate));
            ServiceContainer.Resolve<ITracker> ().CurrentScreen = "Change Start Time";
        }

        public void ChangeTimeEntryStart (DateTime newStartTime)
        {
            UpdateView (x => x.ChangeStartTime (newStartTime), nameof (Duration), nameof (StartDate), nameof (StopDate));
            ServiceContainer.Resolve<ITracker> ().CurrentScreen = "Change Start Time";
        }

        public void ChangeTimeEntryStop (TimeSpan diffTime)
        {
            UpdateView (x => x.ChangeStoptime (x.StopTime + diffTime),nameof (Duration), nameof (StopDate));
            ServiceContainer.Resolve<ITracker> ().CurrentScreen = "Change Stop Time";
        }

        public void ChangeTimeEntryStop (DateTime newStopTime)
        {
            UpdateView (x => x.ChangeStoptime (newStopTime), nameof (Duration), nameof (StopDate));
            ServiceContainer.Resolve<ITracker> ().CurrentScreen = "Change Stop Time";
        }

        public void ChangeTagList (IEnumerable<Guid> newTags)
        {
            UpdateView (x => x.TagIds = newTags.ToList (), nameof (TagList));
        }

        public void ChangeDescription (string description)
        {
            UpdateView (x => x.Description = description, nameof (Description));
        }

        public void ChangeBillable (bool billable)
        {
            UpdateView (x => x.IsBillable = billable, nameof (IsBillable));
        }

        public void Save ()
        {
            if (!IsManual) {
                // TODO RX: Exclude TagIds until test finalize.
                // If Public properties are not equal, save it.
                if (!previousData.Data.PublicInstancePropertiesEqual (richData.Data, "TagIds")) {
                    RxChain.Send (new DataMsg.TimeEntryPut (richData.Data));
                    RxChain.Send (new DataMsg.TagsPut (TagList));
                    previousData = richData;
                }
            }
        }

        // TODO RX: Is this method necessary?
        public void Delete ()
        {
            RxChain.Send (new DataMsg.TimeEntriesRemove (richData.Data));
        }

        public void SaveManual ()
        {
            IsManual = false;
            Save ();
        }

        private void UpdateView (Action<TimeEntryData> updater, params string[] changedProperties)
        {
            // Ensure that this content runs in UI thread
            ServiceContainer.Resolve<IPlatformUtils> ().DispatchOnUIThread (() => {

                var oldProjectId = richData.Data.ProjectId;

                richData = new RichTimeEntry (richData.Data.With (updater), appState);

                if (richData.Data.State == TimeEntryState.Running && !durationTimer.Enabled) {
                    durationTimer.Start ();
                } else if (richData.Data.State != TimeEntryState.Running) {
                    durationTimer.Stop ();
                }

                UpdateRelationships (oldProjectId);

                if (changedProperties.Length == 0) {
                    // TODO: This should update all properties, check
                    RaisePropertyChanged ();
                } else {
                    foreach (var prop in changedProperties) {
                        RaisePropertyChanged (prop);
                    }
                }
            });
        }

        private void UpdateRelationships (Guid oldProjectId)
        {
            // Check if project has changed
            if (richData.Data.ProjectId != Guid.Empty && richData.Data.ProjectId != oldProjectId) {

                // Check if the new project belongs to a different workspace
                if (richData.Data.WorkspaceId != richData.Info.ProjectData.WorkspaceId) {
                    var workspace = appState.Workspaces[richData.Info.ProjectData.WorkspaceId];

                    richData = new RichTimeEntry (
                    richData.Data.With (x => {
                        x.WorkspaceId = workspace.Id;
                        x.IsBillable = workspace.IsPremium && x.IsBillable;
                        x.TagIds = UpdateTagsWithWorkspace (appState, x.Id, workspace.Id, TagList)
                                   .Select (t => t.Id).ToList ();
                    }),
                    appState
                    );
                }
            }
        }

        private void DurationTimerCallback (object sender, System.Timers.ElapsedEventArgs e)
        {
            // Update on UI Thread
            ServiceContainer.Resolve<IPlatformUtils> ().DispatchOnUIThread (() => {
                var duration = richData.Data.GetDuration ();
                // Hack to ensure the timer triggers every second even if there're some extra milliseconds
                durationTimer.Interval = 1000 - duration.Milliseconds;
                RaisePropertyChanged (nameof (Duration));
            });
        }

        private static List<ITagData> GetDefaultTagList (AppState appState, ITimeEntryData data)
        {
            if (!appState.Settings.UseDefaultTag) {
                return new List<ITagData> ();
            }

            var defaultTagList =
                appState.Tags.Values.Where (
                    r => r.Name == DefaultTag && r.WorkspaceId == data.WorkspaceId).ToList ();

            if (defaultTagList.Count == 0) {
                defaultTagList = new List<ITagData> { new TagData (DefaultTag, data.WorkspaceId, data.WorkspaceRemoteId) };
                RxChain.Send (new DataMsg.TagsPut (defaultTagList));
            }
            return defaultTagList;
        }

        private static List<ITagData> UpdateTagsWithWorkspace (AppState appState, Guid timeEntryId, Guid workspaceId, IEnumerable<ITagData> oldTagList)
        {
            // Get new workspace tag list.
            var tagList = appState.Tags.Values.Where (r => r.WorkspaceId == workspaceId).ToList ();

            // Get new tags to create and existing tags from previous workspace.
            var tagsToCreate = new List<ITagData> (oldTagList.Where (t => tagList.IndexOf (n => n.Name.Equals (t.Name)) == -1));
            var commonTags = new List<ITagData> (tagList.Where (t => oldTagList.IndexOf (n => n.Name.Equals (t.Name)) != -1));

            // Create new tags
            var newTags = tagsToCreate.Select (x => x.With (y => {
                y.WorkspaceId = workspaceId;
                y.Name = x.Name;
            })).ToList ();
            RxChain.Send (new DataMsg.TagsPut (newTags));

            // Create new tags and concat both lists
            return commonTags.Concat (tagsToCreate).ToList ();
        }
    }
}
