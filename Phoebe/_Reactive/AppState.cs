using System;
using System.Linq;
using System.Collections.Generic;
using Toggl.Phoebe._Data;
using Toggl.Phoebe._Data.Models;
using XPlatUtils;
using Toggl.Phoebe.Logging;
using System.Reflection;
using System.Linq.Expressions;

namespace Toggl.Phoebe._Reactive
{
    public interface IWithDictionary<T>
    {
        T WithDictionary (IReadOnlyDictionary<string, object> dic);
    }

    public interface IReducer
    {
        object Reduce (object state, IDataMsg msg);
    }

    public class Reducer<T> : IReducer 
    {
        readonly Func<T,IDataMsg,T> reducer;

        public virtual T Reduce (T state, IDataMsg msg) => reducer (state, msg);
        object IReducer.Reduce (object state, IDataMsg msg) => reducer ((T)state, msg);

        protected Reducer () { }

        public Reducer (Func<T,IDataMsg,T> reducer)
        {
            this.reducer = reducer;
        }
    }

    public class TagCompositeReducer<T> : Reducer<T>
    {
        readonly Dictionary<DataTag,Reducer<T>> reducers = new Dictionary<DataTag,Reducer<T>> ();

        public TagCompositeReducer<T> Add (DataTag tag, Func<T,IDataMsg,T> reducer)
        {
            return Add (tag, new Reducer<T> (reducer));
        }

        public TagCompositeReducer<T> Add (DataTag tag, Reducer<T> reducer)
        {
            reducers.Add (tag, reducer);
            return this;
        }

        public override T Reduce (T state, IDataMsg msg)
        {
            Reducer<T> reducer;
            if (reducers.TryGetValue (msg.Tag, out reducer)) {
                return reducer.Reduce (state, msg);
            }
            else {
                return state;
            }
        }
    }

    public class FieldCompositeReducer<T> : IReducer
        where T : IWithDictionary<T>
    {
        readonly List<Tuple<FieldInfo,IReducer>> reducers = new List<Tuple<FieldInfo,IReducer>> ();

        public FieldCompositeReducer<T> Add<TPart> (Expression<Func<T,TPart>> selector, Func<TPart,IDataMsg,TPart> reducer)
        {
            return Add (selector, new Reducer<TPart> (reducer));
        }

        public FieldCompositeReducer<T> Add<TPart> (Expression<Func<T,TPart>> selector, Reducer<TPart> reducer)
        {
            var memberExpr = selector.Body as MemberExpression;
            var member = (FieldInfo) memberExpr.Member;

            if (memberExpr == null)
                throw new ArgumentException(string.Format(
                    "Expression '{0}' should be a field.",
                    selector.ToString()));
            if (member == null)
                throw new ArgumentException(string.Format(
                    "Expression '{0}' should be a constant expression",
                    selector.ToString()));

            reducers.Add(Tuple.Create(member, (IReducer)reducer));
            return this;
        }

        public T Reduce (T state, IDataMsg msg)
        {
            var dic = new Dictionary<string, object> ();
            foreach (var reducer in reducers) {
                var field = reducer.Item1.GetValue (state);
                dic.Add (reducer.Item1.Name, reducer.Item2.Reduce (field, msg));
            }
            return state.WithDictionary (dic);
        }

        object IReducer.Reduce (object state, IDataMsg msg) => Reduce ((T)state, msg);
    }

    public class AppState : IWithDictionary<AppState>
    {
        public TimerState TimerState { get; private set; }

        public AppState (
            TimerState timerState)
        {
            TimerState = timerState;
        }

        public AppState WithDictionary (IReadOnlyDictionary<string, object> dic)
        {
            return new AppState (
                dic.ContainsKey (nameof(TimerState)) ? (TimerState)dic[nameof(TimerState)] : this.TimerState);
        }
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

    public class TimerState
    {
        public UserData User { get; private set; }
        public IReadOnlyDictionary<Guid, WorkspaceData> Workspaces { get; private set; }
        public IReadOnlyDictionary<Guid, ProjectData> Projects { get; private set; }
        public IReadOnlyDictionary<Guid, ClientData> Clients { get; private set; }
        public IReadOnlyDictionary<Guid, TaskData> Tasks { get; private set; }
        public IReadOnlyDictionary<Guid, TagData> Tags { get; private set; }
        public IReadOnlyDictionary<Guid, RichTimeEntry> TimeEntries { get; private set; }

        public TimerState (
            UserData user,
            IReadOnlyDictionary<Guid, WorkspaceData> workspaces, 
            IReadOnlyDictionary<Guid, ProjectData> projects,
            IReadOnlyDictionary<Guid, ClientData> clients,
            IReadOnlyDictionary<Guid, TaskData> tasks,
            IReadOnlyDictionary<Guid, TagData> tags,
            IReadOnlyDictionary<Guid, RichTimeEntry> timeEntries)
        {
            User = user;
            Workspaces = workspaces;
            Projects = projects;
            Clients = clients;
            Tasks = tasks;
            Tags = tags;
            TimeEntries = timeEntries;
        }

        TimerState With (
            UserData user = null,
            IReadOnlyDictionary<Guid, WorkspaceData> workspaces = null,
            IReadOnlyDictionary<Guid, ProjectData> projects = null,
            IReadOnlyDictionary<Guid, ClientData> clients = null,
            IReadOnlyDictionary<Guid, TaskData> tasks = null,
            IReadOnlyDictionary<Guid, TagData> tags = null,
            IReadOnlyDictionary<Guid, RichTimeEntry> timeEntries = null)
        {
            return new TimerState (
                user ?? this.User,
                workspaces ?? this.Workspaces,
                projects ?? this.Projects,
                clients ?? this.Clients,
                tasks ?? this.Tasks,
                tags ?? this.Tags,
                timeEntries ?? this.TimeEntries);
        }
    }
}

