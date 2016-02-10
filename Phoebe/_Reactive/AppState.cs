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
        DataSyncMsg<object> Reduce (object state, IDataMsg msg);
    }

    public class Reducer<T> : IReducer
    {
        readonly Func<T, IDataMsg, DataSyncMsg<T>> reducer;

        public virtual DataSyncMsg<T> Reduce (T state, IDataMsg msg)
        {
            return reducer (state, msg);
        }

        DataSyncMsg<object> IReducer.Reduce (object state, IDataMsg msg)
        {
            var res = Reduce ((T)state, msg);
            return DataSyncMsg.Create (res.Tag, (object)res.State, res.SyncData);
        }

        protected Reducer () { }

        public Reducer (Func<T, IDataMsg, DataSyncMsg<T>> reducer)
        {
            this.reducer = reducer;
        }
    }

    public class TagCompositeReducer<T> : Reducer<T>, IReducer
    {
        readonly Dictionary<DataTag, Reducer<T>> reducers = new Dictionary<DataTag, Reducer<T>> ();

        public TagCompositeReducer<T> Add (DataTag tag, Func<T, IDataMsg, DataSyncMsg<T>> reducer)
        {
            return Add (tag, new Reducer<T> (reducer));
        }

        public TagCompositeReducer<T> Add (DataTag tag, Reducer<T> reducer)
        {
            reducers.Add (tag, reducer);
            return this;
        }

        public override DataSyncMsg<T> Reduce (T state, IDataMsg msg)
        {
            Reducer<T> reducer;
            if (reducers.TryGetValue (msg.Tag, out reducer)) {
                return reducer.Reduce (state, msg);
            } else {
                return DataSyncMsg.Create (msg.Tag, state);
            }
        }

        DataSyncMsg<object> IReducer.Reduce (object state, IDataMsg msg)
        {
            var res = Reduce ((T)state, msg);
            return DataSyncMsg.Create (res.Tag, (object)res.State, res.SyncData);
        }
    }

    public class PropertyCompositeReducer<T> : Reducer<T>, IReducer
        where T : IWithDictionary<T>
    {
        readonly List<Tuple<PropertyInfo, IReducer>> reducers = new List<Tuple<PropertyInfo, IReducer>> ();

        public PropertyCompositeReducer<T> Add<TPart> (
            Expression<Func<T,TPart>> selector,
            Func<TPart, IDataMsg, DataSyncMsg<TPart>> reducer)
        {
            return Add (selector, new Reducer<TPart> (reducer));
        }

        public PropertyCompositeReducer<T> Add<TPart> (Expression<Func<T,TPart>> selector, Reducer<TPart> reducer)
        {
            var memberExpr = selector.Body as MemberExpression;
            var member = memberExpr.Member as PropertyInfo;

            if (memberExpr == null)
                throw new ArgumentException (string.Format (
                                                 "Expression '{0}' should be a property.",
                                                 selector.ToString()));
            if (member == null)
                throw new ArgumentException (string.Format (
                                                 "Expression '{0}' should be a constant expression",
                                                 selector.ToString()));

            reducers.Add (Tuple.Create (member, (IReducer)reducer));
            return this;
        }

        public override DataSyncMsg<T> Reduce (T state, IDataMsg msg)
        {
            var syncData = new List<ICommonData> ();
            var dic = new Dictionary<string, object> ();

            foreach (var reducer in reducers) {
                var propValue = reducer.Item1.GetValue (state);
                var res = reducer.Item2.Reduce (propValue, msg);

                dic.Add (reducer.Item1.Name, res.State);
                syncData.AddRange (res.SyncData);
            }

            return new DataSyncMsg<T> (msg.Tag, state.WithDictionary (dic), syncData);
        }

        DataSyncMsg<object> IReducer.Reduce (object state, IDataMsg msg)
        {
            var res = Reduce ((T)state, msg);
            return DataSyncMsg.Create (res.Tag, (object)res.State, res.SyncData);
        }
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
                       dic.ContainsKey (nameof (TimerState)) ? (TimerState)dic[nameof (TimerState)] : this.TimerState);
        }
    }

    public class RichTimeEntry
    {
        public TimeEntryInfo Info { get; private set; }
        public ITimeEntryData Data { get; private set; }

        public RichTimeEntry (ITimeEntryData data, TimeEntryInfo info)
        {
            Info = info;
            Data = (ITimeEntryData)data.Clone ();
        }
    }

    public class TimerState
    {
        // Set initial pagination Date to the beginning of the next day.
        // So, we can include all entries created -Today-.
        // PaginationDate = Time.UtcNow.Date.AddDays (1);

        public DateTime StartFrom { get; private set; }
        public DateTime PaginationDate { get; private set; }

        public UserData User { get; private set; }
        public IReadOnlyDictionary<Guid, WorkspaceData> Workspaces { get; private set; }
        public IReadOnlyDictionary<Guid, ProjectData> Projects { get; private set; }
        public IReadOnlyDictionary<Guid, ClientData> Clients { get; private set; }
        public IReadOnlyDictionary<Guid, TaskData> Tasks { get; private set; }
        public IReadOnlyDictionary<Guid, TagData> Tags { get; private set; }
        public IReadOnlyDictionary<Guid, RichTimeEntry> TimeEntries { get; private set; }

        public TimerState (
            DateTime startFrom,
            DateTime paginationDate,
            UserData user,
            IReadOnlyDictionary<Guid, WorkspaceData> workspaces,
            IReadOnlyDictionary<Guid, ProjectData> projects,
            IReadOnlyDictionary<Guid, ClientData> clients,
            IReadOnlyDictionary<Guid, TaskData> tasks,
            IReadOnlyDictionary<Guid, TagData> tags,
            IReadOnlyDictionary<Guid, RichTimeEntry> timeEntries)
        {
            StartFrom = startFrom;
            PaginationDate = paginationDate;
            User = user;
            Workspaces = workspaces;
            Projects = projects;
            Clients = clients;
            Tasks = tasks;
            Tags = tags;
            TimeEntries = timeEntries;
        }

        public TimerState With (
            DateTime startFrom = default (DateTime),
            DateTime paginationDate = default (DateTime),
            UserData user = null,
            IReadOnlyDictionary<Guid, WorkspaceData> workspaces = null,
            IReadOnlyDictionary<Guid, ProjectData> projects = null,
            IReadOnlyDictionary<Guid, ClientData> clients = null,
            IReadOnlyDictionary<Guid, TaskData> tasks = null,
            IReadOnlyDictionary<Guid, TagData> tags = null,
            IReadOnlyDictionary<Guid, RichTimeEntry> timeEntries = null)
        {
            return new TimerState (
                       startFrom == default (DateTime) ? this.StartFrom : startFrom,
                       paginationDate == default (DateTime) ? this.PaginationDate : paginationDate,
                       user ?? this.User,
                       workspaces ?? this.Workspaces,
                       projects ?? this.Projects,
                       clients ?? this.Clients,
                       tasks ?? this.Tasks,
                       tags ?? this.Tags,
                       timeEntries ?? this.TimeEntries);
        }

        /// <summary>
        /// This doesn't check ModifiedAt or DeletedAt, so call it
        /// always after putting items first in the database
        /// </summary>
        public IReadOnlyDictionary<Guid, T> Update<T> (
            IReadOnlyDictionary<Guid, T> oldItems, IEnumerable<ICommonData> newItems)
        where T : CommonData
        {
            var dic = oldItems.ToDictionary (x => x.Key, x => x.Value);
            foreach (var newItem in newItems.OfType<T> ()) {
                if (newItem.DeletedAt == null) {
                    if (dic.ContainsKey (newItem.Id)) {
                        dic [newItem.Id] = newItem;
                    } else {
                        dic.Add (newItem.Id, newItem);
                    }
                } else {
                    if (dic.ContainsKey (newItem.Id)) {
                        dic.Remove (newItem.Id);
                    }
                }
            }
            return dic;
        }

        /// <summary>
        /// This doesn't check ModifiedAt or DeletedAt, so call it
        /// always after putting items first in the database
        /// </summary>
        public IReadOnlyDictionary<Guid, RichTimeEntry> UpdateTimeEntries (
            IEnumerable<ICommonData> newItems)
        {
            var dic = TimeEntries.ToDictionary (x => x.Key, x => x.Value);
            foreach (var newItem in newItems.OfType<ITimeEntryData> ()) {
                if (newItem.DeletedAt == null) {
                    if (dic.ContainsKey (newItem.Id)) {
                        dic [newItem.Id] = new RichTimeEntry (
                            newItem, LoadTimeEntryInfo (newItem));
                    } else {
                        dic.Add (newItem.Id, new RichTimeEntry (
                            newItem, LoadTimeEntryInfo (newItem)));
                    }
                } else {
                    if (dic.ContainsKey (newItem.Id)) {
                        dic.Remove (newItem.Id);
                    }
                }
            }
            return dic;
        }

        public TimeEntryInfo LoadTimeEntryInfo (ITimeEntryData teData)
        {
            var projectData = teData.ProjectId != Guid.Empty
                              ? this.Projects[teData.ProjectId]
                              : new ProjectData ();
            var clientData = projectData.ClientId != Guid.Empty
                             ? this.Clients[projectData.ClientId]
                             : new ClientData ();
            var taskData = teData.TaskId != Guid.Empty
                           ? this.Tasks[teData.TaskId]
                           : new TaskData ();
            var color = (projectData.Id != Guid.Empty) ? projectData.Color : -1;

            return new TimeEntryInfo (
                       projectData,
                       clientData,
                       taskData,
                       color);
        }
    }
}

