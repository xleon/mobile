using System;
using System.Collections.Generic;
using System.Linq;
using GalaSoft.MvvmLight;
using PropertyChanged;
using Toggl.Phoebe.Analytics;
using Toggl.Phoebe._Data;
using Toggl.Phoebe._Data.Models;
using Toggl.Phoebe._Helpers;
using Toggl.Phoebe._Reactive;
using XPlatUtils;

namespace Toggl.Phoebe._ViewModels
{
    [ImplementPropertyChanged]
    public class EditTimeEntryVM : ViewModelBase, IDisposable
    {
        internal static readonly string DefaultTag = "mobile";

        private TimerState timerState;
        private RichTimeEntry richData;
        private RichTimeEntry initialData;
        private System.Timers.Timer durationTimer;

        private void Init (TimerState timerState, TimeEntryData timeData, List<TagData> tagList)
        {
            durationTimer = new System.Timers.Timer ();
            durationTimer.Elapsed += DurationTimerCallback;

            richData = (IsManual = timeData.Id == Guid.Empty)
                ? new RichTimeEntry (
                    timerState,
                    timeData.With (x => {
                        x.StartTime = Time.UtcNow.AddMinutes (-5);
                        x.StopTime = Time.UtcNow;
                        x.State = TimeEntryState.Finished;
                        x.Tags = tagList.Select (t => t.Name).ToList ();

                        // TODO: Do we need to reset tags in initial data here?
                        //initialTagList = new List<TagData> ();
                    }))
                : new RichTimeEntry (timerState, timeData.With (
                    x => x.Tags = tagList.Select (t => t.Name).ToList ()));
			
			// Save previous state.
			initialData = new RichTimeEntry (richData.Data, richData.Info);

            UpdateView ();
            UpdateRelationships ();

            ServiceContainer.Resolve<ITracker> ().CurrentScreen = "Edit Time Entry";
        }

        public EditTimeEntryVM (TimerState timerState, Guid timeEntryId)
        {
            TimeEntryData data;
            List<TagData> tagList;

            if (timeEntryId == Guid.Empty) {
                data = timerState.GetTimeEntryDraft ();
                tagList = GetDefaultTagList (timerState, data.WorkspaceId);
            } else {
                var richTe = timerState.TimeEntries[timeEntryId];
                data = new TimeEntryData (richTe.Data);
                tagList = new List<TagData> (richTe.Info.Tags);
            }

            Init (timerState, data, tagList);
        }

        public EditTimeEntryVM (TimerState timerState, ITimeEntryData timeEntryData, List<TagData> tagList)
        {
            Init (timerState, new TimeEntryData (timeEntryData), tagList);
        }

        public void Dispose ()
        {
            durationTimer.Elapsed -= DurationTimerCallback;
            durationTimer.Dispose ();
        }

        #region viewModel State properties
        public bool IsManual { get; private set; }
        public bool SyncError { get; private set; }

        public string Duration {
            get {
                // TODO: check substring function for long times
                return TimeSpan.FromSeconds (richData.Data.GetDuration ().TotalSeconds)
                               .ToString ().Substring (0, 8);
            }
        }

        public DateTime StartData {
            get {
                return richData.Data.StartTime == DateTime.MinValue
                               ? DateTime.UtcNow.AddMinutes (-1).ToLocalTime ()
                               : richData.Data.StartTime.ToLocalTime ();
            }
        }

        public DateTime StopDate {
            get {
                return richData.Data.StopTime.HasValue
                           ? richData.Data.StopTime.Value.ToLocalTime ()
                           : DateTime.UtcNow.ToLocalTime ();
            }
        }

        public string ProjectColorHex {
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

        // TODO: Unify Change... methods: Must all be in same thread? Why some call UpdateView and others don't?
        public void ChangeProjectAndTask (Guid workspaceId, Guid projectId, Guid taskId)
        {
            if (projectId != richData.Data.ProjectId || taskId != richData.Data.TaskId) {
                richData = new RichTimeEntry (
                    timerState,
                    richData.Data.With (x => {
                        x.ProjectId = projectId;
                        x.TaskId = taskId;
                    })
                );
                UpdateRelationships ();
            }
        }

        public void ChangeTimeEntryDuration (TimeSpan newDuration)
        {
            richData = new RichTimeEntry (
                timerState,
                richData.Data.With (x => x.SetDuration (newDuration))
            );
            UpdateView ();
            ServiceContainer.Resolve<ITracker> ().CurrentScreen = "Change Duration";
        }

        public void ChangeTimeEntryStart (TimeSpan diffTime)
        {
            richData = new RichTimeEntry (
                timerState,
                richData.Data.With (x => x.StartTime += diffTime)
            );
            UpdateView ();
            ServiceContainer.Resolve<ITracker> ().CurrentScreen = "Change Start Time";
        }

        public void ChangeTimeEntryStop (TimeSpan diffTime)
        {
            richData = new RichTimeEntry (
                timerState,
                richData.Data.With (x => {
                    x.StopTime += diffTime;
					if (diffTime.TotalSeconds > 0) {
						x.StartTime = x.StartTime.Truncate (TimeSpan.TicksPerMinute);
						x.StopTime = ((DateTime)x.StopTime).Truncate (TimeSpan.TicksPerMinute);
					}
                })
            );
            UpdateView ();
            ServiceContainer.Resolve<ITracker> ().CurrentScreen = "Change Stop Time";
        }

        // TODO: This method can accept just a list of strings
        public void ChangeTagList (IEnumerable<TagData> newTagList)
        {
            richData = new RichTimeEntry (
                timerState,
                richData.Data.With (x => x.Tags = newTagList.Select (t => t.Name).ToList ())
            );
            RaisePropertyChanged (() => TagList);
        }

        public void ChangeDescription (string description)
        {
            richData = new RichTimeEntry (
                timerState,
                richData.Data.With (x => x.Description = description)
            );
        }

        public void ChangeBillable (bool billable)
        {
            richData = new RichTimeEntry (
                timerState,
                richData.Data.With (x => x.IsBillable = billable)
            );
        }

        // TODO: Unify this method with ChangeTags?
        public void AddTag (TagData tagData)
        {
            ChangeTagList (TagList.Append (tagData));
        }

        public void Save ()
        {
            if (IsManual) {
                return;
            }

            // TODO TODO TODO: Unify both conditions by just comparing initialData and richData
    //        if (!data.PublicInstancePropertiesEqual (initialState)) {
    //            data = await TimeEntryModel.PrepareForSync (data);
    //            RxChain.Send (new DataMsg.TimeEntryPut (data));
				//RxChain.Send (new DataMsg.TagsPut (TagList));
    //        }

            if (!initialData.Info.Tags.SequenceEqual (TagList, (arg1, arg2) => arg1.Id == arg2.Id)) {
                RxChain.Send (new DataMsg.TimeEntryPut (richData.Data));
                RxChain.Send (new DataMsg.TagsPut (TagList));
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

        private void UpdateView ()
        {
            // Ensure that this content runs in UI thread
            ServiceContainer.Resolve<IPlatformUtils> ().DispatchOnUIThread (() => {

                if (richData.Data.State == TimeEntryState.Running && !durationTimer.Enabled) {
                    durationTimer.Start ();
                } else if (richData.Data.State != TimeEntryState.Running) {
                    durationTimer.Stop ();
                }

                SyncError = (richData.Data.RemoteRejected || !richData.Data.RemoteId.HasValue);
            });
        }

        private void UpdateRelationships ()
        {
            // Ensure that this content runs in UI thread
            ServiceContainer.Resolve<IPlatformUtils> ().DispatchOnUIThread (() => {

                // TODO: Workspace, Billable and Tags should change!?
                if (richData.Data.ProjectId != Guid.Empty) {
                    var project = timerState.Projects[richData.Data.ProjectId];

                    if (richData.Data.WorkspaceId != project.WorkspaceId) {
                        var workspace = timerState.Workspaces[project.WorkspaceId];

                        richData = new RichTimeEntry (
                            timerState,
                            richData.Data.With (x => {
                                x.WorkspaceId = workspace.Id;
                                x.IsBillable = workspace.IsPremium && x.IsBillable;
                                x.Tags = UpdateTagsWithWorkspace (timerState, x.Id, workspace.Id, TagList)
                                    .Select (t => t.Name).ToList ();
                            })
                        );
                    }
                }
            });
        }

        private void DurationTimerCallback (object sender, System.Timers.ElapsedEventArgs e)
        {
            var duration = richData.Data.GetDuration ();
            durationTimer.Interval = 1000 - duration.Milliseconds;

            // Update on UI Thread
            ServiceContainer.Resolve<IPlatformUtils> ().DispatchOnUIThread (() => {
                // TODO: Can Duration be different from the TimeEntry duration?
                //Duration = TimeSpan.FromSeconds (duration.TotalSeconds).ToString ().Substring (0, 8);
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

        private static List<TagData> UpdateTagsWithWorkspace (TimerState timerState, Guid timeEntryId, Guid workspaceId, IEnumerable<TagData> oldTagList)
        {
            // Get new workspace tag list.
            var tagList = timerState.Tags.Values.Where (r => r.WorkspaceId == workspaceId).ToList ();

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
