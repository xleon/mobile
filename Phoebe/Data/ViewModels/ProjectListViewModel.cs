using System;
using System.Threading.Tasks;
using PropertyChanged;
using Toggl.Phoebe.Analytics;
using Toggl.Phoebe.Data.Models;
using Toggl.Phoebe.Data.ViewModels;
using Toggl.Phoebe.Data.Views;
using XPlatUtils;

namespace Toggl.Phoebe.Data.ViewModels
{
    [ImplementPropertyChanged]
    public class ProjectListViewModel : IDisposable
    {
        ProjectListViewModel (Guid workspaceId, WorkspaceProjectsView projectList)
        {
            CurrentWorkspaceId = workspaceId;
            ProjectList = projectList;
            ServiceContainer.Resolve<ITracker> ().CurrentScreen = "Select Project";
        }

        public static async Task<ProjectListViewModel> Init (Guid workspaceId)
        {
            var projectList = await WorkspaceProjectsView.Init ();
            return new ProjectListViewModel (workspaceId, projectList);
        }

        public void Dispose ()
        {
            ProjectList.Dispose ();
        }

        public WorkspaceProjectsView ProjectList { get; set; }

        public Guid CurrentProjectId { get; set; }

        public Guid CurrentWorkspaceId { get; set; }

        public Guid CurrentTaskId { get; set; }
    }
}
