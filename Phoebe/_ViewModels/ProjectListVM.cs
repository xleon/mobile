using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using GalaSoft.MvvmLight;
using PropertyChanged;
using Toggl.Phoebe.Analytics;
using Toggl.Phoebe.Logging;
using Toggl.Phoebe._Data.Models;
using Toggl.Phoebe._Helpers;
using Toggl.Phoebe._Reactive;
using XPlatUtils;

namespace Toggl.Phoebe._ViewModels
{
    [ImplementPropertyChanged]
    public class ProjectListVM : ViewModelBase, IDisposable
    {
        public ProjectListVM (TimerState timerState, Guid workspaceId)
        {
			CurrentWorkspaceId = workspaceId;
			ServiceContainer.Resolve<ITracker> ().CurrentScreen = "Select Project";

            // TODO: Change settings for a better library like James Montemagno version
            // and define default values to avoid this code.
			// TODO TODO TODO: Danger! Mutating a property from a service
			var settingsStore = ServiceContainer.Resolve<Data.ISettingsStore> ();
            if (string.IsNullOrEmpty (settingsStore.SortProjectsBy)) {
                settingsStore.SortProjectsBy = ProjectsCollectionVM.SortProjectsBy.Clients.ToString ();
            }
            var savedSort = Enum.Parse (typeof (ProjectsCollectionVM.SortProjectsBy), settingsStore.SortProjectsBy);

            ProjectList = new ProjectsCollectionVM (
                timerState, (ProjectsCollectionVM.SortProjectsBy)savedSort, workspaceId);
            
            WorkspaceList = timerState.Workspaces.Values.OrderBy (r => r.Name).ToList ();
            CurrentWorkspaceIndex = WorkspaceList.IndexOf (p => p.Id == workspaceId);

            // Search stream
            Observable.FromEventPattern<string> (ev => onSearch += ev, ev => onSearch -= ev)
                      .Throttle (TimeSpan.FromMilliseconds (300))
                      .DistinctUntilChanged ()
                      .Subscribe (
                          p => ServiceContainer.Resolve<IPlatformUtils> ().DispatchOnUIThread (
                              () => { ProjectList.ProjectNameFilter = p.EventArgs; }),
                          ex => ServiceContainer.Resolve<ILogger> ().Error ("Search", ex, null));
        }

        public void Dispose ()
        {
            ProjectList.Dispose ();
        }

        #region Observable properties
        public List<WorkspaceData> WorkspaceList { get; private set; }

        public ProjectsCollectionVM ProjectList { get; private set; }

        public int CurrentWorkspaceIndex { get; private set; }

        public Guid CurrentWorkspaceId { get; private set; }
        #endregion

        private event EventHandler<string> onSearch;

        public void SearchByProjectName (string token)
        {
            onSearch.Invoke (this, token);
        }

        public void ChangeListSorting (ProjectsCollectionVM.SortProjectsBy sortBy)
        {
            // TODO TODO TODO: Danger! Mutating a property from a service
            ProjectList.SortBy = sortBy;
            var settingsStore = ServiceContainer.Resolve<Data.ISettingsStore> ();
            settingsStore.SortProjectsBy = sortBy.ToString ();
        }

        public void ChangeWorkspaceByIndex (int newIndex)
        {
            CurrentWorkspaceId = WorkspaceList [newIndex].Id;
            ProjectList.WorkspaceId = WorkspaceList [newIndex].Id;
            CurrentWorkspaceIndex = newIndex;
        }
    }
}
