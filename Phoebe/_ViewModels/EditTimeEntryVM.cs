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

namespace Toggl.Phoebe._ViewModels
{
    public class EditTimeEntryVM : ViewModelBase, IDisposable
    {
        internal static readonly string DefaultTag = "mobile";

        private AppState appState;
        private RichTimeEntry richData;
        private RichTimeEntry previousData;
        private System.Timers.Timer durationTimer;

        private void Init (AppState state, TimeEntryData timeData, List<string> tagList)
        {
            durationTimer = new System.Timers.Timer ();
            durationTimer.Elapsed += DurationTimerCallback;

            this.appState = state;
            IsManual = timeData.Id == Guid.Empty;
            richData = new RichTimeEntry (state, timeData);

            UpdateView (x => {
                x.Tags = tagList;
                if (IsManual) {
                    x.StartTime = Time.UtcNow.AddMinutes (-5);
                    x.StopTime = Time.UtcNow;
                    x.State = TimeEntryState.Finished;
                }
            });

            // Save previous state.
            previousData = IsManual
                           // Hack to force tag saving even if there're no other changes
                           ? new RichTimeEntry (state, richData.Data.With (x => x.Tags = new List<string> ()))
                           : new RichTimeEntry (richData.Data, richData.Info);

            ServiceContainer.Resolve<ITracker> ().CurrentScreen = "Edit Time Entry";
        }

        public EditTimeEntryVM (AppState appState, Guid timeEntryId)
        {
            TimeEntryData data;
            List<string> tagList;

            if (timeEntryId == Guid.Empty) {
                data = appState.GetTimeEntryDraft ();
                tagList = GetDefaultTagList (appState, data.WorkspaceId).Select (x => x.Name).ToList ();
            } else {
                var richTe = appState.TimeEntries[timeEntryId];
                data = new TimeEntryData (richTe.Data);
                tagList = new List<string> (richTe.Data.Tags);
            }

            Init (appState, data, tagList);
        }

        public EditTimeEntryVM (AppState appState, ITimeEntryData timeEntryData, List<string> tagList)
        {
            Init (appState, new TimeEntryData (timeEntryData), tagList);
        }

        public void Dispose ()
        {
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
        public string Description { get { return richData.Data.Description ?? ""; } }
        public bool IsBillable { get { return richData.Data.IsBillable; } }
        public bool IsPremium { get { return richData.Info.WorkspaceData.IsPremium; } }
        public Guid WorkspaceId { get { return richData.Data.WorkspaceId; } }

        public string ProjectName { get { return richData.Info.ProjectData.Name ?? ""; } }
        public string TaskName { get { return richData.Info.TaskData.Name ?? ""; } }
        public string ClientName { get { return richData.Info.ClientData.Name ?? ""; } }
        public IReadOnlyList<TagData> TagList { get { return richData.Info.Tags; } }

        #endregion

        public void ChangeProjectAndTask (Guid projectId, Guid taskId)
        {
            if (projectId != richData.Data.ProjectId || taskId != richData.Data.TaskId) {
                UpdateView (x => {
                    x.ProjectId = projectId;
                    x.TaskId = taskId;
                });
            }
        }

        public void ChangeTimeEntryDuration (TimeSpan newDuration)
        {
            UpdateView (x => x.SetDuration (newDuration));
            ServiceContainer.Resolve<ITracker> ().CurrentScreen = "Change Duration";
        }

        public void ChangeTimeEntryStart (TimeSpan diffTime)
        {
            UpdateView (x => x.StartTime += diffTime);
            ServiceContainer.Resolve<ITracker> ().CurrentScreen = "Change Start Time";
        }

        public void ChangeTimeEntryStop (TimeSpan diffTime)
        {
            UpdateView (x => {
                x.StopTime += diffTime;
                if (diffTime.TotalSeconds > 0) {
                    x.StartTime = x.StartTime.Truncate (TimeSpan.TicksPerMinute);
                    x.StopTime = ((DateTime)x.StopTime).Truncate (TimeSpan.TicksPerMinute);
                }
            });
            ServiceContainer.Resolve<ITracker> ().CurrentScreen = "Change Stop Time";
        }

        public void ChangeTagList (IEnumerable<string> newTags)
        {
            UpdateView (x => x.Tags = newTags.ToList (), nameof (TagList));
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
                // TODO: Would be more efficient to use structural equality here?
                bool hasChanged = previousData != richData;

                if (hasChanged) {
                    RxChain.Send (new DataMsg.TimeEntryPut (richData.Data));
                    RxChain.Send (new DataMsg.TagsPut (TagList));
                    previousData = richData;
                }
            }
        }

        public void Delete ()
        {
            RxChain.Send (new DataMsg.TimeEntryDelete (richData.Data));
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

                richData = new RichTimeEntry (
                    appState,
                    richData.Data.With (updater)
                );

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
                        appState,
                    richData.Data.With (x => {
                        x.WorkspaceId = workspace.Id;
                        x.IsBillable = workspace.IsPremium && x.IsBillable;
                        x.Tags = UpdateTagsWithWorkspace (appState, x.Id, workspace.Id, TagList)
                                 .Select (t => t.Name).ToList ();
                    })
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

        private static List<TagData> GetDefaultTagList (AppState appState, Guid workspaceId)
        {
            if (!ServiceContainer.Resolve<Data.ISettingsStore> ().UseDefaultTag) {
                return new List<TagData> ();
            }

            var defaultTagList = appState.Tags.Values.Where (
                                     r => r.Name == DefaultTag && r.WorkspaceId == workspaceId).ToList ();

            if (defaultTagList.Count == 0) {
                defaultTagList = new List<TagData> { new TagData (workspaceId, DefaultTag) };
                RxChain.Send (new DataMsg.TagsPut (defaultTagList));
            }
            return defaultTagList;
        }

        private static List<TagData> UpdateTagsWithWorkspace (AppState appState, Guid timeEntryId, Guid workspaceId, IEnumerable<TagData> oldTagList)
        {
            // Get new workspace tag list.
            var tagList = appState.Tags.Values.Where (r => r.WorkspaceId == workspaceId).ToList ();

            // Get new tags to create and existing tags from previous workspace.
            var tagsToCreate = new List<TagData> (oldTagList.Where (t => tagList.IndexOf (n => n.Name.Equals (t.Name)) == -1));
            var commonTags = new List<TagData> (tagList.Where (t => oldTagList.IndexOf (n => n.Name.Equals (t.Name)) != -1));

            // Create new tags
            var newTags = tagsToCreate.Select (x => new TagData {
                WorkspaceId = workspaceId,
                Name = x.Name
            }).ToList ();
            RxChain.Send (new DataMsg.TagsPut (newTags));

            // Create new tags and concat both lists
            return commonTags.Concat (tagsToCreate).ToList ();
        }
    }
}
