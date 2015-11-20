using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Threading.Tasks;
using Toggl.Phoebe.Data.DataObjects;
using Toggl.Phoebe.Data.Utils;
using Toggl.Phoebe.Data.Models;

namespace Toggl.Phoebe.Data.Views
{
    /// <summary>
    /// </summary>
    public class LogTimeEntriesView : TimeEntriesCollectionView
    {
        private readonly List<TEDataGroup> dateGroups = new List<TEDataGroup> ();

        public LogTimeEntriesView ()
        {
            Tag = "LogTimeEntriesView";
        }

        protected override IList<object> CreateItemCollection(IList<TimeEntryHolder> holders)
        {
            return holders
                .GroupBy(x => x.TimeEntryData.StartTime.ToLocalTime().Date)
                .SelectMany(gr => {
                    var timeEntries = gr.Select(x => x.TimeEntryData).ToList();
                    return gr.Cast<object>().Prepend(new TEDataGroup(gr.Key, timeEntries));
                })
                .ToList();
        }

        protected override async Task<TimeEntryHolder> CreateTimeEntryHolder(TimeEntryData entry, TimeEntryHolder previousHolder = null)
        {
            // Ignore previousHolder
            var holder = new TimeEntryHolder(new List<TimeEntryData>() { entry });
            await holder.LoadAsync();
            return holder;
        }

        protected override int GetTimeEntryHolderIndex(IList<TimeEntryHolder> holders, TimeEntryData entry)
        {
            for (var i = 0; i < holders.Count; i++) {
                if (holders[i].TimeEntryData.Id == entry.Id)
                    return i;
            }
            return -1;
        }

        protected async override Task AddOrUpdateEntryAsync (TimeEntryData entry)
        {
            int groupIndex;
            int newIndex;
            NotifyCollectionChangedAction groupAction;

            TimeEntryData existingEntry;
            TEDataGroup grp;
            bool isNewGroup;

            if (FindExistingEntry (entry, out grp, out existingEntry)) {
                if (entry.StartTime != existingEntry.StartTime) {
                    var date = entry.StartTime.ToLocalTime ().Date;
                    var oldIndex = GetTimeEntryIndex (existingEntry);

                    // Move TimeEntry to another TEDataGroup
                    if (grp.Date != date) {

                        // Remove entry from previous TEDataGroup: //TODO: remove dateGroup too?
                        grp.Remove (existingEntry);
                        groupIndex = GetDateGroupIndex (grp);
                        await UpdateCollectionAsync (grp, NotifyCollectionChangedAction.Replace, groupIndex);

                        // Move entry to new TEDataGroup
                        grp = GetGroupFor (entry, out isNewGroup);
                        grp.Add (entry);
                        Sort ();

                        newIndex = GetTimeEntryIndex (entry);
                        await UpdateCollectionAsync (entry, NotifyCollectionChangedAction.Move, newIndex, oldIndex);

                        // Update new container TEDataGroup
                        groupIndex = GetDateGroupIndex (grp);
                        groupAction = isNewGroup ? NotifyCollectionChangedAction.Add : NotifyCollectionChangedAction.Replace;
                        await UpdateCollectionAsync (grp, groupAction, groupIndex);

                        return;
                    }

                    // Move TimeEntry inside TEDataGroup
                    grp.TimeEntryList.UpdateData (entry);
                    Sort ();

                    // Update group
                    groupIndex = GetDateGroupIndex (grp);
                    await UpdateCollectionAsync (grp, NotifyCollectionChangedAction.Replace, groupIndex);

                    newIndex = GetTimeEntryIndex (entry);
                    if (newIndex != oldIndex) {
                        // Move if index is differente.
                        await UpdateCollectionAsync (entry, NotifyCollectionChangedAction.Move, newIndex, oldIndex);
                    }

                    // Update in any condition
                    await UpdateCollectionAsync (entry, NotifyCollectionChangedAction.Replace, newIndex);

                } else {
                    // Update TimeEntry only
                    grp.TimeEntryList.UpdateData (entry);

                    // Update entry
                    newIndex = GetTimeEntryIndex (entry);
                    await UpdateCollectionAsync (entry, NotifyCollectionChangedAction.Replace, newIndex);
                }
            } else {

                // Add new TimeEntry
                grp = GetGroupFor (entry, out isNewGroup);
                grp.Add (entry);
                Sort ();

                // Update group
                groupIndex = GetDateGroupIndex (grp);
                groupAction = isNewGroup ? NotifyCollectionChangedAction.Add : NotifyCollectionChangedAction.Replace;
                await UpdateCollectionAsync (grp, groupAction, groupIndex);

                // Add new TimeEntry
                newIndex = GetTimeEntryIndex (entry);
                await UpdateCollectionAsync (entry, NotifyCollectionChangedAction.Add, newIndex);
            }
        }

        protected async override Task RemoveEntryAsync (TimeEntryData entry)
        {
            int groupIndex;
            int entryIndex;
            NotifyCollectionChangedAction groupAction = NotifyCollectionChangedAction.Replace;
            TEDataGroup grp;
            TimeEntryData oldEntry;

            if (FindExistingEntry (entry, out grp, out oldEntry)) {
                entryIndex = GetTimeEntryIndex (oldEntry);
                groupIndex = GetDateGroupIndex (grp);
                grp.Remove (oldEntry);
                if (grp.TimeEntryList.Count == 0) {
                    dateGroups.Remove (grp);
                    groupAction = NotifyCollectionChangedAction.Remove;
                }

                // The order affects how the collection is updated.
                await UpdateCollectionAsync (entry, NotifyCollectionChangedAction.Remove, entryIndex);
                await UpdateCollectionAsync (grp, groupAction, groupIndex);
            }
        }

        protected override IList<DateGroup> DateGroups
        {
            get { return dateGroups.ToList<DateGroup> (); }
        }

        #region Undo
        protected async override Task AddTimeEntryHolderAsync (TimeEntryHolder holder)
        {
            await AddOrUpdateEntryAsync (holder.TimeEntryData);
        }

        protected async override Task RemoveTimeEntryHolderAsync (TimeEntryHolder holder)
        {
            await RemoveEntryAsync (holder.TimeEntryData);
        }
        #endregion

        #region Utils
        private bool FindExistingEntry (TimeEntryData dataObject, out TEDataGroup dateGroup, out TimeEntryData existingDataObject)
        {
            foreach (var grp in dateGroups) {
                foreach (var obj in grp.TimeEntryList) {
                    if (dataObject.Matches (obj)) {
                        dateGroup = grp;
                        existingDataObject = obj;
                        return true;
                    }
                }
            }

            dateGroup = null;
            existingDataObject = null;
            return false;
        }

        private int GetTimeEntryIndex (TimeEntryData dataObject)
        {
            int count = 0;
            foreach (var grp in dateGroups) {
                count++;
                // Iterate by entry list.
                foreach (var obj in grp.DataObjects) {
                    if (dataObject.Matches (obj)) {
                        return count;
                    }
                    count++;
                }
            }
            return -1;
        }

        private int GetDateGroupIndex (TEDataGroup dateGroup)
        {
            var count = 0;
            foreach (var grp in dateGroups) {
                if (grp.Date == dateGroup.Date) {
                    return count;
                }
                count += grp.TimeEntryList.Count + 1;
            }
            return -1;
        }

        private TEDataGroup GetGroupFor (TimeEntryData dataObject, out bool isNewGroup)
        {
            isNewGroup = false;
            var date = dataObject.StartTime.ToLocalTime ().Date;
            var grp = dateGroups.FirstOrDefault (g => g.Date == date);
            if (grp == null) {
                grp = new TEDataGroup (date);
                dateGroups.Add (grp);
                isNewGroup = true;
            }
            return grp;
        }

        private void Sort ()
        {
            foreach (var grp in dateGroups) {
                grp.Sort ();
            }
            dateGroups.Sort ((a, b) => b.Date.CompareTo (a.Date));
        }
        #endregion

        public class TEDataGroup : DateGroup
        {
            public readonly List<TimeEntryData> TimeEntryList;

            public TEDataGroup(DateTime date, List<TimeEntryData> dataObjects = null) : base(date)
            {
                TimeEntryList = dataObjects ?? new List<TimeEntryData>();
            }

            public override IEnumerable<ITimeEntryModelBase> DataObjects
            {
                get
                {
                    return TimeEntryList.Cast<ITimeEntryModelBase>();
                }
            }

            public void Add (TimeEntryData dataObject)
            {
                TimeEntryList.Add (dataObject);
                OnUpdated ();
            }

            public void Remove (TimeEntryData dataObject)
            {
                TimeEntryList.Remove (dataObject);
                OnUpdated ();
            }

            public void Sort ()
            {
                TimeEntryList.Sort ((a, b) => b.StartTime.CompareTo (a.StartTime));
                OnUpdated ();
            }

        }
    }
}
