using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Threading.Tasks;
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

        protected async override Task AddOrUpdateEntryAsync (TimeEntryData entry)
        {
            // Avoid a removed item (Undoable)
            // been added again.
            if (LastRemovedItem != null && LastRemovedItem.TimeEntryData.Matches (entry)) {
                return;
            }

            TimeEntryGroup timeEntryGroup;
            DateGroup dateGroup;
            TimeEntryData existingTimeEntry;

            bool isNewTimeEntryGroup;
            bool isNewDateGroup;
            int newTimeEntryGroupIndex;
            int groupIndex;
            int oldTimeEntryGroupIndex;

            var entryExists = FindExistingEntry (entry, out dateGroup, out timeEntryGroup, out existingTimeEntry);
            bool entryBelongsToSameGroup = false;
            if (entryExists) {
                entryBelongsToSameGroup = timeEntryGroup.CanContain (entry);
            }

            if (entryBelongsToSameGroup) {
                if (entry.StartTime != existingTimeEntry.StartTime) {

                    var date = entry.StartTime.ToLocalTime ().Date;
                    oldTimeEntryGroupIndex = GetEntryGroupIndex (timeEntryGroup);

                    // Move TimeEntryGroup to another DateGroup container.
                    if (dateGroup.Date != date) {

                        // Remove time entry from previous group.
                        timeEntryGroup.Remove (existingTimeEntry);

                        // Update or Delete old container Date group.
                        if (timeEntryGroup.Count == 0) {
                            timeEntryGroup.Dispose ();
                            dateGroup.Remove (timeEntryGroup);
                            await UpdateCollectionAsync (timeEntryGroup, CollectionEventBuilder.GetEvent (NotifyCollectionChangedAction.Remove, oldTimeEntryGroupIndex, -1)).ConfigureAwait (false);
                        } else {
                            oldTimeEntryGroupIndex = GetEntryGroupIndex (timeEntryGroup);
                            Sort ();
                            newTimeEntryGroupIndex = GetEntryGroupIndex (timeEntryGroup);

                            // Move if needed
                            if (newTimeEntryGroupIndex != oldTimeEntryGroupIndex) {
                                await UpdateCollectionAsync (timeEntryGroup, CollectionEventBuilder.GetEvent (NotifyCollectionChangedAction.Move, newTimeEntryGroupIndex, oldTimeEntryGroupIndex)).ConfigureAwait (false);
                            }
                            await UpdateCollectionAsync (timeEntryGroup, CollectionEventBuilder.GetEvent (NotifyCollectionChangedAction.Replace, newTimeEntryGroupIndex, -1)).ConfigureAwait (false);
                        }

                        // Update or Delete old container Date group.
                        if (dateGroup.TimeEntryGroupList.Count == 0) {
                            await  UpdateCollectionAsync (dateGroup, CollectionEventBuilder.GetEvent (NotifyCollectionChangedAction.Remove, GetDateGroupIndex (dateGroup), -1)).ConfigureAwait (false);
                            dateGroups.Remove (dateGroup);
                        } else {
                            await UpdateCollectionAsync (dateGroup, CollectionEventBuilder.GetEvent (NotifyCollectionChangedAction.Replace, GetDateGroupIndex (dateGroup), -1)).ConfigureAwait (false);
                        }

                        // Get or create containers.
                        dateGroup = GetDateGroupFor (entry, out isNewDateGroup);
                        timeEntryGroup = GetSuitableEntryGroupFor (dateGroup, entry, out isNewTimeEntryGroup);
                        if (!isNewTimeEntryGroup) {
                            // In case of new container group, entry is added at creation.
                            timeEntryGroup.Add (entry);
                        }
                        // Get old time entry group index.
                        oldTimeEntryGroupIndex = GetEntryGroupIndex (timeEntryGroup);

                        Sort ();

                        // Get new index.
                        newTimeEntryGroupIndex = GetEntryGroupIndex (timeEntryGroup);
                        var newDateGroupIndex = GetDateGroupIndex (dateGroup);

                        // Add or Update corresponding header.
                        if (isNewDateGroup) {
                            await UpdateCollectionAsync (dateGroup, CollectionEventBuilder.GetEvent (NotifyCollectionChangedAction.Add, newDateGroupIndex, -1)).ConfigureAwait (false);
                        } else {
                            await UpdateCollectionAsync (dateGroup, CollectionEventBuilder.GetEvent (NotifyCollectionChangedAction.Replace, newDateGroupIndex, -1)).ConfigureAwait (false);
                        }

                        // Updated or add time entry group.
                        if (isNewTimeEntryGroup) {
                            await UpdateCollectionAsync (timeEntryGroup, CollectionEventBuilder.GetEvent (NotifyCollectionChangedAction.Add, newTimeEntryGroupIndex, -1)).ConfigureAwait (false);
                        } else {
                            // Move if needed
                            if (newTimeEntryGroupIndex != oldTimeEntryGroupIndex) {
                                await UpdateCollectionAsync (timeEntryGroup, CollectionEventBuilder.GetEvent (NotifyCollectionChangedAction.Move, newTimeEntryGroupIndex, oldTimeEntryGroupIndex)).ConfigureAwait (false);
                            }
                            await UpdateCollectionAsync (timeEntryGroup, CollectionEventBuilder.GetEvent (NotifyCollectionChangedAction.Replace, newTimeEntryGroupIndex, -1)).ConfigureAwait (false);
                        }

                        return;
                    }

                    // Update containers.
                    timeEntryGroup.Update (entry);
                    dateGroup.Update (timeEntryGroup);
                    Sort ();

                    // Update corresponding header.
                    await UpdateCollectionAsync (dateGroup, CollectionEventBuilder.GetEvent (NotifyCollectionChangedAction.Replace, GetDateGroupIndex (dateGroup), -1)).ConfigureAwait (false);

                    newTimeEntryGroupIndex = GetEntryGroupIndex (timeEntryGroup);
                    if (newTimeEntryGroupIndex != oldTimeEntryGroupIndex) {
                        // Move if needed. Update in any case.
                        await UpdateCollectionAsync (timeEntryGroup, CollectionEventBuilder.GetEvent (NotifyCollectionChangedAction.Move, newTimeEntryGroupIndex, oldTimeEntryGroupIndex)).ConfigureAwait (false);
                    }
                    await UpdateCollectionAsync (timeEntryGroup, CollectionEventBuilder.GetEvent (NotifyCollectionChangedAction.Replace, newTimeEntryGroupIndex, -1)).ConfigureAwait (false);

                } else {
                    // Update group container only.
                    timeEntryGroup.Update (entry);
                    newTimeEntryGroupIndex = GetEntryGroupIndex (timeEntryGroup);
                    await UpdateCollectionAsync (timeEntryGroup, CollectionEventBuilder.GetEvent (NotifyCollectionChangedAction.Replace, newTimeEntryGroupIndex, -1)).ConfigureAwait (false);
                }
            } else {

                // Remove from previous if exists.
                if (entryExists) {
                    oldTimeEntryGroupIndex = GetEntryGroupIndex (timeEntryGroup);
                    timeEntryGroup.Remove (existingTimeEntry);
                    if (timeEntryGroup.Count == 0) {
                        timeEntryGroup.Dispose ();
                        dateGroup.Remove (timeEntryGroup);
                        await UpdateCollectionAsync (timeEntryGroup, CollectionEventBuilder.GetEvent (NotifyCollectionChangedAction.Remove, oldTimeEntryGroupIndex, -1)).ConfigureAwait (false);
                    }  else {
                        Sort ();
                        newTimeEntryGroupIndex = GetEntryGroupIndex (timeEntryGroup);
                        // Move if needed
                        if (newTimeEntryGroupIndex != oldTimeEntryGroupIndex) {
                            await UpdateCollectionAsync (timeEntryGroup, CollectionEventBuilder.GetEvent (NotifyCollectionChangedAction.Move, newTimeEntryGroupIndex, oldTimeEntryGroupIndex)).ConfigureAwait (false);
                        }
                        await UpdateCollectionAsync (timeEntryGroup, CollectionEventBuilder.GetEvent (NotifyCollectionChangedAction.Replace, newTimeEntryGroupIndex, -1)).ConfigureAwait (false);
                    }
                }

                // Get or create containers.
                dateGroup = GetDateGroupFor (entry, out isNewDateGroup);
                timeEntryGroup = GetSuitableEntryGroupFor (dateGroup, entry, out isNewTimeEntryGroup);
                oldTimeEntryGroupIndex = GetEntryGroupIndex (timeEntryGroup);

                if (!isNewTimeEntryGroup) {
                    // In case of existing container group, we should add the entry.
                    timeEntryGroup.Add (entry);
                }
                Sort ();

                // Update group.
                groupIndex = GetDateGroupIndex (dateGroup);
                var groupAction = isNewDateGroup ? NotifyCollectionChangedAction.Add : NotifyCollectionChangedAction.Replace;
                await UpdateCollectionAsync (dateGroup, CollectionEventBuilder.GetEvent (groupAction, groupIndex, -1)).ConfigureAwait (false);

                // Updated or add time entry group.
                newTimeEntryGroupIndex = GetEntryGroupIndex (timeEntryGroup);
                if (isNewTimeEntryGroup) {
                    await UpdateCollectionAsync (timeEntryGroup, CollectionEventBuilder.GetEvent (NotifyCollectionChangedAction.Add, newTimeEntryGroupIndex, -1)).ConfigureAwait (false);
                } else {
                    // Move if needed
                    if (newTimeEntryGroupIndex != oldTimeEntryGroupIndex) {
                        await UpdateCollectionAsync (timeEntryGroup, CollectionEventBuilder.GetEvent (NotifyCollectionChangedAction.Move, newTimeEntryGroupIndex, oldTimeEntryGroupIndex)).ConfigureAwait (false);
                    }
                    await UpdateCollectionAsync (timeEntryGroup, CollectionEventBuilder.GetEvent (NotifyCollectionChangedAction.Replace, newTimeEntryGroupIndex, -1)).ConfigureAwait (false);
                }
            }
        }

        protected async override Task RemoveEntryAsync (TimeEntryData entry)
        {
            TimeEntryGroup entryGroup;
            DateGroup dateGroup;
            TimeEntryData existingEntry;

            int entryIndex;
            int oldIndex;

            if (FindExistingEntry (entry, out dateGroup, out entryGroup, out existingEntry)) {
                oldIndex = GetEntryGroupIndex (entryGroup);

                // Remove entry from group
                entryGroup.Remove (existingEntry);

                // Update or Delete old container Date group.
                if (entryGroup.Count == 0) {
                    entryGroup.Dispose ();
                    dateGroup.Remove (entryGroup);
                    await UpdateCollectionAsync (entryGroup, CollectionEventBuilder.GetEvent (NotifyCollectionChangedAction.Remove, oldIndex, -1)).ConfigureAwait (false);
                } else {
                    Sort ();
                    entryIndex = GetEntryGroupIndex (entryGroup);

                    // Move if needed
                    if (entryIndex != oldIndex) {
                        await UpdateCollectionAsync (entryGroup, CollectionEventBuilder.GetEvent (NotifyCollectionChangedAction.Move, entryIndex, oldIndex)).ConfigureAwait (false);
                    }
                    await UpdateCollectionAsync (entryGroup, CollectionEventBuilder.GetEvent (NotifyCollectionChangedAction.Replace, entryIndex, -1)).ConfigureAwait (false);
                }

                // Update or Delete old container Date group.
                var dateGroupIndex = GetDateGroupIndex (dateGroup);
                if (dateGroup.TimeEntryGroupList.Count == 0) {
                    await UpdateCollectionAsync (dateGroup, CollectionEventBuilder.GetEvent (NotifyCollectionChangedAction.Remove, dateGroupIndex, -1)).ConfigureAwait (false);
                    dateGroups.Remove (dateGroup);
                } else {
                    await UpdateCollectionAsync (dateGroup, CollectionEventBuilder.GetEvent (NotifyCollectionChangedAction.Replace, dateGroupIndex, -1)).ConfigureAwait (false);
                }
            }
        }

        protected override IList<IDateGroup> DateGroups
        {
            get { return dateGroups.ToList<IDateGroup> (); }
        }

        #region Undo
        protected async override void AddTimeEntryHolder (TimeEntryHolder holder)
        {
            var entryGroup = new TimeEntryGroup (holder.TimeEntryDataList);

            bool isNewGroup;
            DateGroup grp = GetDateGroupFor (entryGroup.Data, out isNewGroup);
            grp.Add (entryGroup);
            Sort ();

            // Update Date group.
            var groupIndex = GetDateGroupIndex (grp);
            var groupAction = isNewGroup ? NotifyCollectionChangedAction.Add : NotifyCollectionChangedAction.Replace;
            await UpdateCollectionAsync (grp, CollectionEventBuilder.GetEvent (groupAction, groupIndex, -1)).ConfigureAwait (false);

            // Add time entry group.
            var newIndex = GetEntryGroupIndex (entryGroup);
            await UpdateCollectionAsync (entryGroup, CollectionEventBuilder.GetEvent (NotifyCollectionChangedAction.Add, newIndex, -1)).ConfigureAwait (false);
        }

        protected async override void RemoveTimeEntryHolder (TimeEntryHolder holder)
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
                await UpdateCollectionAsync (timeEntryGroup, CollectionEventBuilder.GetEvent (NotifyCollectionChangedAction.Remove, entryGroupIndex, -1)).ConfigureAwait (false);

                // Notify or update Date group.
                if (dateGroup.TimeEntryGroupList.Count == 0) {
                    dateGroups.Remove (dateGroup);
                    await UpdateCollectionAsync (dateGroup, CollectionEventBuilder.GetEvent (NotifyCollectionChangedAction.Remove, dateGroupIndex, -1)).ConfigureAwait (false);
                } else {
                    await UpdateCollectionAsync (dateGroup, CollectionEventBuilder.GetEvent (NotifyCollectionChangedAction.Replace, dateGroupIndex, -1)).ConfigureAwait (false);
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
