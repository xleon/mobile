using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using PropertyChanged;
using Toggl.Phoebe.Analytics;
using Toggl.Phoebe.Data.DataObjects;
using Toggl.Phoebe.Data.Models;
using Toggl.Phoebe.Data.Utils;
using Toggl.Phoebe.Data.ViewModels;
using Toggl.Phoebe.Data.Views;
using XPlatUtils;

namespace Toggl.Phoebe.Data.ViewModels
{
    [ImplementPropertyChanged]
    public class ProjectListViewModel : IVModel<ITimeEntryModel>
    {
        private ITimeEntryModel model;
        private IList<string> timeEntryIds;

        public ProjectListViewModel (IList<TimeEntryData> timeEntryList)
        {
            TimeEntryList = timeEntryList;
            ServiceContainer.Resolve<ITracker> ().CurrentScreen = "Select Project";
        }

        public ProjectListViewModel (IList<string> timeEntryIds)
        {
            this.timeEntryIds = timeEntryIds;
            ServiceContainer.Resolve<ITracker> ().CurrentScreen = "Select Project";
        }

        public async Task Init ()
        {
            IsLoading = true;

            if (TimeEntryList == null) {
                TimeEntryList = await TimeEntryGroup.GetTimeEntryDataList (timeEntryIds);
            }

            // Create model.
            if (TimeEntryList.Count > 1) {
                model = new TimeEntryGroup (TimeEntryList);
            } else if (TimeEntryList.Count == 1) {
                model = new TimeEntryModel (TimeEntryList [0]);
            }

            await model.LoadAsync ();

            ProjectList = new WorkspaceProjectsView ();
            await ProjectList.ReloadAsync ();

            if (model.Workspace == null || model.Workspace.Id == Guid.Empty) {
                model = null;
            }

            IsLoading = false;
        }

        public void Dispose ()
        {
            ProjectList.Dispose ();
            model = null;
        }

        public WorkspaceProjectsView ProjectList { get; set; }

        public IList<TimeEntryData> TimeEntryList { get; set; }

        public bool IsLoading { get; set; }

        public async Task SaveModelAsync (Guid projectId, Guid workspaceId, TaskData task = null)
        {
            model.Project = new ProjectModel (projectId);
            model.Workspace = new WorkspaceModel (workspaceId);

            if (task != null) {
                model.Task = new TaskModel (task);
            }
            await model.SaveAsync ();
        }
    }
}
