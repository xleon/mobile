using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using Toggl.Phoebe.Data.DataObjects;
using Toggl.Phoebe.Data.Utils;

namespace Toggl.Phoebe.Data.Views
{
    /// <summary>
    /// This view combines IDataStore data and data from ITogglClient for time views. It tries to load data from
    /// web, but always falls back to data from the local store.
    /// </summary>
    public class GroupedTimeEntriesView : TimeEntriesCollectionView
    {
        private readonly List<DateGroup> dateGroups = new List<DateGroup> ();
        private static readonly string Tag = "GroupedTimeEntriesView";

        protected override void AddOrUpdateEntry (TimeEntryData entry)
        {
            base.AddOrUpdateEntry (entry);

            TimeEntryGroup entryGroup;
            DateGroup dateGroup;
            TimeEntryData existingEntry;
            NotifyCollectionChangedAction entryAction;

            bool isNewEntryGroup;
            bool isNewDateGroup = false;
            int newIndex;
            int groupIndex;
            int oldIndex = -1;

            if (FindExistingEntry (entry, out dateGroup, out entryGroup, out existingEntry)) {
                if (entry.StartTime != existingEntry.StartTime) {
                    var date = entry.StartTime.ToLocalTime ().Date;
                    oldIndex = GetEntryGroupIndex (entryGroup);

                    // Move TimeEntryGroup to another DateGroup
                    if (dateGroup.Date != date) {

                        // Remove from containers.
                        entryGroup.Remove (existingEntry);
                        if (entryGroup.Count == 0) {
                            entryGroup.Dispose ();
                            dateGroup.Remove (entryGroup);
                            DispatchCollectionEvent (entryGroup, CollectionEventBuilder.GetEvent (NotifyCollectionChangedAction.Remove, oldIndex, -1));
                        } else {
                            DispatchCollectionEvent (entryGroup, CollectionEventBuilder.GetEvent (NotifyCollectionChangedAction.Replace, oldIndex, -1));
                        }

                        // Update old group
                        DispatchCollectionEvent (dateGroup, CollectionEventBuilder.GetEvent (NotifyCollectionChangedAction.Replace, GetDateGroupIndex (dateGroup), -1));

                        dateGroup = GetDateGroupFor (entry, out isNewDateGroup);
                        entryGroup = GetSuitableEntryGroupFor (dateGroup, entry, out isNewEntryGroup);

                        // In case of new container group, entry is added at creation.
                        if (!isNewEntryGroup) {
                            entryGroup.Add (entry);
                        }

                        Sort ();

                        entryAction = (isNewEntryGroup) ? NotifyCollectionChangedAction.Add : NotifyCollectionChangedAction.Replace;
                    } else {
                        entryGroup.Update (entry);
                        dateGroup.Update (entryGroup);
                        Sort ();
                        entryAction = NotifyCollectionChangedAction.Replace;
                    }
                } else {
                    entryGroup.Update (entry);
                    dateGroup.Update (entryGroup);
                    entryAction = NotifyCollectionChangedAction.Replace;
                }
            } else {
                dateGroup = GetDateGroupFor (entry, out isNewDateGroup);
                entryGroup = GetSuitableEntryGroupFor (dateGroup, entry, out isNewEntryGroup);

                // In case of new container group, entry is added at creation.
                if (!isNewEntryGroup) {
                    oldIndex = GetEntryGroupIndex (entryGroup);
                    entryGroup.Add (entry);
                    entryAction = NotifyCollectionChangedAction.Replace;
                } else {
                    entryAction = NotifyCollectionChangedAction.Add;
                }

                Sort ();
            }

            // Update group.
            groupIndex = GetDateGroupIndex (dateGroup);
            var groupAction = isNewDateGroup ? NotifyCollectionChangedAction.Add : NotifyCollectionChangedAction.Replace;
            DispatchCollectionEvent (dateGroup, CollectionEventBuilder.GetEvent (groupAction, groupIndex, oldIndex));

            // Updated entry.
            newIndex = GetEntryGroupIndex (entryGroup);
            if (entryAction == NotifyCollectionChangedAction.Replace && oldIndex != -1 && oldIndex != newIndex) {
                DispatchCollectionEvent (entryGroup, CollectionEventBuilder.GetEvent (NotifyCollectionChangedAction.Move, newIndex, oldIndex));
                DispatchCollectionEvent (entryGroup, CollectionEventBuilder.GetEvent (NotifyCollectionChangedAction.Replace, newIndex, -1));
            } else {
                DispatchCollectionEvent (entryGroup, CollectionEventBuilder.GetEvent (entryAction, newIndex, oldIndex));
            }
        }

        protected override void RemoveEntry (TimeEntryData entry)
        {
            base.RemoveEntry (entry);

            TimeEntryGroup entryGroup;
            DateGroup dateGroup;
            TimeEntryData existingEntry;

            int groupIndex;
            int entryIndex;
            int oldIndex;

            if (FindExistingEntry (entry, out dateGroup, out entryGroup, out existingEntry)) {
                groupIndex = GetDateGroupIndex (dateGroup);
                oldIndex = GetEntryGroupIndex (entryGroup);

                // Remove entry from group
                entryGroup.Remove (existingEntry);

                // If group is empty, remove it.
                if (entryGroup.Count == 0) {
                    dateGroup.Remove (entryGroup);
                    DispatchCollectionEvent (entryGroup, CollectionEventBuilder.GetEvent (NotifyCollectionChangedAction.Remove, oldIndex, -1));
                    entryGroup.Dispose ();

                    // If container DateGroup is empty, remove it too.
                    if (dateGroup.TimeEntryGroupList.Count == 0) {
                        dateGroups.Remove (dateGroup);
                        DispatchCollectionEvent (dateGroup, CollectionEventBuilder.GetEvent (NotifyCollectionChangedAction.Remove, groupIndex, -1));
                    } else {
                        DispatchCollectionEvent (dateGroup, CollectionEventBuilder.GetEvent (NotifyCollectionChangedAction.Replace, groupIndex, -1));
                    }
                } else {
                    // If no item is removed, just sort the list.
                    Sort ();
                    entryIndex = GetEntryGroupIndex (entryGroup);
                    if (entryIndex != oldIndex) {
                        DispatchCollectionEvent (entryGroup, CollectionEventBuilder.GetEvent (NotifyCollectionChangedAction.Move, entryIndex, oldIndex));
                    }

                    // Update both items (header and item)
                    DispatchCollectionEvent (entryGroup, CollectionEventBuilder.GetEvent (NotifyCollectionChangedAction.Replace, entryIndex, -1));
                    DispatchCollectionEvent (dateGroup, CollectionEventBuilder.GetEvent (NotifyCollectionChangedAction.Replace, groupIndex, -1));
                }
            }
        }

        protected override IList<IDateGroup> DateGroups
        {
            get { return dateGroups.ToList<IDateGroup> (); }
        }

        #region Undo
        protected override void AddTimeEntryHolder (TimeEntryHolder holder)
        {
            var entryGroup = new TimeEntryGroup (holder.TimeEntryDataList);

            bool isNewGroup;
            DateGroup grp = GetDateGroupFor (entryGroup.Data, out isNewGroup);
            grp.Add (entryGroup);
            Sort ();

            // Update Date group.
            var groupIndex = GetDateGroupIndex (grp);
            var groupAction = isNewGroup ? NotifyCollectionChangedAction.Add : NotifyCollectionChangedAction.Replace;
            DispatchCollectionEvent (grp, CollectionEventBuilder.GetEvent (groupAction, groupIndex, -1));

            // Add time entry group.
            var newIndex = GetEntryGroupIndex (entryGroup);
            DispatchCollectionEvent (entryGroup, CollectionEventBuilder.GetEvent (NotifyCollectionChangedAction.Add, newIndex, -1));
        }

        protected override void RemoveTimeEntryHolder (TimeEntryHolder holder)
        {
            DateGroup dateGroup;
            TimeEntryGroup timeEntryGroup;
            TimeEntryData timeEntry;

            if (FindExistingEntry (holder.TimeEntryData, out dateGroup, out timeEntryGroup, out timeEntry)) {

                // Get items indexes.
                var entryGroupIndex = GetEntryGroupIndex (timeEntryGroup);
                var dateGroupIndex = GetDateGroupIndex (dateGroup);
                dateGroup.Remove (timeEntryGroup);

                // Notify removed entry group.
                DispatchCollectionEvent (timeEntryGroup, CollectionEventBuilder.GetEvent (NotifyCollectionChangedAction.Remove, entryGroupIndex, -1));

                // Notify or update Date group.
                if (dateGroup.TimeEntryGroupList.Count == 0) {
                    dateGroups.Remove (dateGroup);
                    DispatchCollectionEvent (dateGroup, CollectionEventBuilder.GetEvent (NotifyCollectionChangedAction.Remove, dateGroupIndex, -1));
                } else {
                    DispatchCollectionEvent (dateGroup, CollectionEventBuilder.GetEvent (NotifyCollectionChangedAction.Replace, dateGroupIndex, -1));
                }
            }
        }
        #endregion

        #region Utils
        private bool FindExistingEntry (TimeEntryData dataObject, out DateGroup dateGroup, out TimeEntryGroup existingGroup, out TimeEntryData existingEntry)
        {
            foreach (var grp in dateGroups) {
                foreach (var obj in grp.TimeEntryGroupList) {
                    TimeEntryData entry;
                    if (obj.Contains (dataObject, out entry)) {
                        dateGroup = grp;
                        existingGroup = obj;
                        existingEntry = entry;
                        return true;
                    }
                }
            }

            existingEntry = null;
            dateGroup = null;
            existingGroup = null;
            return false;
        }

        private DateGroup GetDateGroupFor (TimeEntryData dataObject, out bool isNewDateGroup)
        {
            isNewDateGroup = false;
            var date = dataObject.StartTime.ToLocalTime ().Date;
            var dateGroup = dateGroups.FirstOrDefault (g => g.Date == date);
            if (dateGroup == null) {
                dateGroup = new DateGroup (date);
                dateGroups.Add (dateGroup);
                isNewDateGroup = true;
            }
            return dateGroup;
        }

        private TimeEntryGroup GetExistingEntryGroupFor (DateGroup dateGroup, TimeEntryData dataObject, out bool isNewEntryGroup)
        {
            isNewEntryGroup = false;

            foreach (var grp in dateGroup.TimeEntryGroupList) {
                TimeEntryData entryData;
                if (grp.Contains (dataObject, out entryData)) {
                    return grp;
                }
            }

            var entryGroup = new TimeEntryGroup (dataObject);
            dateGroup.Add (entryGroup);
            isNewEntryGroup = true;

            return entryGroup;
        }

        private TimeEntryGroup GetSuitableEntryGroupFor (DateGroup dateGroup, TimeEntryData dataObject, out bool isNewEntryGroup)
        {
            isNewEntryGroup = false;

            foreach (var grp in dateGroup.TimeEntryGroupList) {
                if (grp.CanContain (dataObject)) {
                    return grp;
                }
            }

            var entryGroup = new TimeEntryGroup (dataObject);
            dateGroup.Add (entryGroup);
            isNewEntryGroup = true;

            return entryGroup;
        }

        private int GetEntryGroupIndex (TimeEntryGroup entryGroup)
        {
            int count = 0;
            foreach (var grp in dateGroups) {
                count++;
                // Iterate by entry list.
                foreach (var obj in grp.TimeEntryGroupList) {
                    if (entryGroup.Data.Matches (obj.Data)) {
                        return count;
                    }
                    count++;
                }
            }
            return -1;
        }

        private int GetDateGroupIndex (DateGroup dateGroup)
        {
            var count = 0;
            foreach (var grp in dateGroups) {
                if (grp.Date == dateGroup.Date) {
                    return count;
                }
                count += grp.TimeEntryGroupList.Count + 1;
            }
            return -1;
        }

        private void Sort ()
        {
            foreach (var grp in dateGroups) {
                grp.Sort ();
            }

            dateGroups.Sort ((a, b) => b.Date.CompareTo (a.Date));
        }
        #endregion

        public class DateGroup : IDateGroup
        {
            private readonly DateTime date;
            private readonly List<TimeEntryGroup> dataObjects = new List<TimeEntryGroup>();

            public DateGroup (DateTime date)
            {
                this.date = date.Date;
            }

            public DateTime Date
            {
                get { return date; }
            }

            public TimeSpan TotalDuration
            {
                get {
                    TimeSpan totalDuration = TimeSpan.Zero;
                    foreach (var item in dataObjects) {
                        totalDuration += item.Duration;
                    }
                    return totalDuration;
                }
            }

            public bool IsRunning
            {
                get {
                    return dataObjects.Any (g => g.State == TimeEntryState.Running);
                }
            }

            public IEnumerable<object> DataObjects
            {
                get {
                    return dataObjects;
                }
            }


            public List<TimeEntryGroup> TimeEntryGroupList
            {
                get {
                    return dataObjects;
                }
            }

            public void Add (TimeEntryGroup entryGroup)
            {
                dataObjects.Add (entryGroup);
            }

            public void Update (TimeEntryGroup entryGroup)
            {
                for (int i = 0; i < dataObjects.Count; i++) {
                    if (dataObjects[i].Data.Matches (entryGroup.Data)) {
                        dataObjects [i] = entryGroup;
                    }
                }
            }

            public void Remove (TimeEntryGroup entryGroup)
            {
                entryGroup.Dispose();
                dataObjects.Remove (entryGroup);
            }

            public void Sort ()
            {
                foreach (var item in dataObjects) {
                    item.Sort();
                }
                dataObjects.Sort ((a, b) => b.LastStartTime.CompareTo (a.LastStartTime));
            }
        }
    }
}
