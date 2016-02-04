using System;
using System.Linq;
using System.Collections.Generic;
using Toggl.Phoebe._Data;
using Toggl.Phoebe._Data.Models;
using XPlatUtils;
using Toggl.Phoebe.Logging;

namespace Toggl.Phoebe._Reactive
{
    public class CompositeUpdater<T>
    {
        readonly List<Tuple<Delegate, Delegate>> updaters = new List<Tuple<Delegate, Delegate>> ();

        public CompositeUpdater<T> Add<U> (Func<T,U> selector, Action<U, IDataMsg> updater)
        {
            updaters.Add (Tuple.Create<Delegate, Delegate>(selector, updater));
            return this;
        }

        public void Update (T state, IDataMsg msg)
        {
            try {
                foreach (var tuple in updaters) {
                    var selection = tuple.Item1.DynamicInvoke (state);
                    tuple.Item2.DynamicInvoke (selection, msg);
                }
            }
            catch (Exception ex) {
                var log = ServiceContainer.Resolve<ILogger> ();
                log.Error (typeof(CompositeUpdater<T>).Name, ex, "Failed to update estate");
            }
        }
    }

    public interface IAppState
    {
        ITimerState TimerState { get; }
    }

    public class AppState : IAppState
    {
        public TimerState TimerState { get; set; } = new TimerState ();
        ITimerState IAppState.TimerState => TimerState;
    }

    public class RichTimeEntry
    {
        public TimeEntryInfo Info { get; private set; }
        public TimeEntryData Data { get; private set; }

        public RichTimeEntry (TimeEntryData data, TimeEntryInfo info)
        {
            Info = info;
            Data = data;
        }
    }

    public interface ITimerState
    {
        UserData User { get; }
        DateTime LowerLimit { get; }

        IReadOnlyDictionary<Guid, WorkspaceData> Workspaces { get; }
        IReadOnlyDictionary<Guid, ProjectData> Projects { get; }
        IReadOnlyDictionary<Guid, ClientData> Clients { get; }
        IReadOnlyDictionary<Guid, TaskData> Tasks { get; }
        IReadOnlyDictionary<Guid, RichTimeEntry> TimeEntries { get; }

        // TODO: Tags
    }


    public class TimerState : ITimerState
    {
        public UserData User { get; set; } = new UserData ();
        public DateTime LowerLimit { get; set; } = DateTime.MinValue;

        public Dictionary<Guid, WorkspaceData> Workspaces { get; set; } = new Dictionary<Guid, WorkspaceData> ();
        public Dictionary<Guid, ProjectData> Projects { get; set; } = new Dictionary<Guid, ProjectData> ();
        public Dictionary<Guid, ClientData> Clients { get; set; } = new Dictionary<Guid, ClientData> ();
        public Dictionary<Guid, TaskData> Tasks { get; set; } = new Dictionary<Guid, TaskData> ();
        public Dictionary<Guid, RichTimeEntry> TimeEntries { get; set; } = new Dictionary<Guid, RichTimeEntry> ();

        IReadOnlyDictionary<Guid, WorkspaceData> ITimerState.Workspaces => Workspaces;
        IReadOnlyDictionary<Guid, ProjectData> ITimerState.Projects => Projects;
        IReadOnlyDictionary<Guid, ClientData> ITimerState.Clients => Clients;
        IReadOnlyDictionary<Guid, TaskData> ITimerState.Tasks => Tasks;
        IReadOnlyDictionary<Guid, RichTimeEntry> ITimerState.TimeEntries => TimeEntries;

        // TODO: Tags
    }
}

