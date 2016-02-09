using System;
using System.Linq;
using System.Collections.Generic;
using Toggl.Phoebe._Data;
using Toggl.Phoebe._Data.Models;
using XPlatUtils;
using Toggl.Phoebe.Logging;

namespace Toggl.Phoebe._Reactive
{
    public interface IUpdater
    {
        object Select (object source);
        void Update (object state, IDataMsg msg);
    }

    public class Updater<T,TSource> : IUpdater 
    {
        readonly Func<TSource,T> selector;
        readonly Action<T,IDataMsg> updater;

        public T Select (TSource source) => selector (source);
        public void Update (T state, IDataMsg msg) => updater (state, msg);

        object IUpdater.Select (object source) => selector ((TSource)source);
        void IUpdater.Update (object state, IDataMsg msg) => updater ((T)state, msg);

        public Updater (Func<TSource,T> selector, Action<T,IDataMsg> updater)
        {
            this.selector = selector;
            this.updater = updater;
        }
    }

    public class CompositeUpdater<T>
    {
        readonly List<IUpdater> updaters = new List<IUpdater> ();

        public CompositeUpdater<T> Add<TPart> (Func<T,TPart> selector, Action<TPart,IDataMsg> updater)
        {
            updaters.Add (new Updater<TPart,T> (selector, updater));
            return this;
        }

        public void Update (T state, IDataMsg msg)
        {
            foreach (var updater in updaters) {
                var selection = updater.Select (state);
                updater.Update (selection, msg);
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
        public ITimeEntryData Data { get; private set; }

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

