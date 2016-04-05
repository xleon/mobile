using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using GalaSoft.MvvmLight;
using PropertyChanged;
using Toggl.Phoebe.Analytics;
using Toggl.Phoebe.Logging;
using Toggl.Phoebe.Data.Models;
using Toggl.Phoebe.Helpers;
using Toggl.Phoebe.Reactive;
using XPlatUtils;
using Toggl.Phoebe.Data;
using System.Threading;

namespace Toggl.Phoebe.ViewModels
{
    public interface IOnProjectSelectedHandler
    {
        void OnProjectSelected(Guid projectId, Guid taskId);
    }

    [ImplementPropertyChanged]
    public class ProjectListVM : ViewModelBase, IDisposable
    {
        private IDisposable searchObservable;

        public ProjectListVM(AppState appState, Guid workspaceId)
        {
            ServiceContainer.Resolve<ITracker> ().CurrentScreen = "Select Project";

            CurrentWorkspaceId = workspaceId;
            var savedSort = Enum.Parse (typeof (ProjectsCollectionVM.SortProjectsBy), appState.Settings.ProjectSort);
            ProjectList = new ProjectsCollectionVM (appState, (ProjectsCollectionVM.SortProjectsBy)savedSort, workspaceId);
            WorkspaceList = appState.Workspaces.Values.OrderBy (r => r.Name).ToList ();
            CurrentWorkspaceIndex = WorkspaceList.IndexOf (p => p.Id == workspaceId);

            // Search stream
            searchObservable = Observable.FromEventPattern<string> (ev => onSearch += ev, ev => onSearch -= ev)
                               .Throttle (TimeSpan.FromMilliseconds (300))
                               .DistinctUntilChanged ()
                               .ObserveOn (SynchronizationContext.Current)
                               .Subscribe (p => ProjectList.ProjectNameFilter = p.EventArgs,
                                           ex => ServiceContainer.Resolve<ILogger> ().Error ("Search", ex, null));
        }

        public void Dispose()
        {
            searchObservable.Dispose();
            ProjectList.Dispose();
        }

        #region Observable properties
        public List<IWorkspaceData> WorkspaceList { get; private set; }
        public ProjectsCollectionVM ProjectList { get; private set; }
        public int CurrentWorkspaceIndex { get; private set; }
        public Guid CurrentWorkspaceId { get; private set; }
        #endregion

        private event EventHandler<string> onSearch;

        public void SearchByProjectName(string token)
        {
            onSearch.Invoke(this, token);
        }

        public void ChangeListSorting(ProjectsCollectionVM.SortProjectsBy sortBy)
        {
            // TODO RX: TODO TODO TODO: Danger! Mutating a property from a service
            ProjectList.SortBy = sortBy;
            RxChain.Send(new DataMsg.UpdateSetting(nameof(SettingsState.ProjectSort), sortBy.ToString()));
        }

        public void ChangeWorkspaceByIndex(int newIndex)
        {
            CurrentWorkspaceId = WorkspaceList [newIndex].Id;
            ProjectList.WorkspaceId = WorkspaceList [newIndex].Id;
            CurrentWorkspaceIndex = newIndex;
        }
    }
}
