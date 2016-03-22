using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using Toggl.Phoebe.Logging;
using Toggl.Phoebe._Data;
using Toggl.Phoebe._Data.Diff;
using Toggl.Phoebe._Data.Models;
using Toggl.Phoebe._Helpers;
using Toggl.Phoebe._Reactive;
using Toggl.Phoebe._ViewModels.Timer;
using XPlatUtils;

namespace Toggl.Phoebe._ViewModels
{
    public class TimeEntryCollectionVM : Data.Utils.ObservableRangeCollection<IHolder>, IDisposable
    {
        IDisposable disposable;
        ITimeEntryHolder lastRemovedItem;
        readonly TimeEntryGrouper grouper;
        System.Timers.Timer undoTimer = new System.Timers.Timer ();

        public IEnumerable<IHolder> Data
        {
            get { return Items; }
        }

        public TimeEntryCollectionVM (TimeEntryGroupMethod groupMethod)
        {
            grouper = new TimeEntryGrouper (groupMethod);
            disposable = StoreManager
                         .Singleton
                         .Observe (x => x.State.TimeEntries.Values)
                         // TODO: Recover buffer?
                         //.TimedBuffer (bufferMilliseconds)
                         .Scan (new List<IHolder> (), UpdateItems)
                         .Subscribe ();
        }

        public void Dispose ()
        {
            if (disposable != null) {
                disposable.Dispose ();
                disposable = null;
            }
        }

        private List<IHolder> UpdateItems (List<IHolder> currentHolders, IEnumerable<RichTimeEntry> entries)
        {
            try {
                var timeHolders = entries.Select (x => new TimeEntryHolder (x)).ToList ();

                // Create the new item collection from holders (sort and add headers...)
                var newItemCollection = CreateItemCollection (timeHolders);

                // Check diffs, modify ItemCollection and notify changes
                var diffs = Diff.Calculate (currentHolders, newItemCollection);

                // Swap remove events to delete normal items before headers.
                // iOS requierement.
                diffs = Diff.SortRemoveEvents<IHolder,DateHolder> (diffs);
                Console.WriteLine ("updates: " + diffs.Count);
                // CollectionChanged events must be fired on UI thread
                ServiceContainer.Resolve<IPlatformUtils> ().DispatchOnUIThread (() => {
                    foreach (var diff in diffs) {
                        switch (diff.Type) {
                        case DiffType.Add:
                            Insert (diff.NewIndex, diff.NewItem);
                            break;
                        case DiffType.Remove:
                            RemoveAt (diff.NewIndex);
                            break;
                        case DiffType.Replace:
                            this[diff.NewIndex] = diff.NewItem;
                            break;
                        case DiffType.Move:
                            Move (diff.OldIndex, diff.NewIndex, diff.NewItem);
                            break;
                        }
                    }
                });

                return newItemCollection;

            } catch (Exception ex) {
                var log = ServiceContainer.Resolve<ILogger> ();
                log.Error (GetType ().Name, ex, "Failed to update collection");
                return currentHolders;
            }
        }

        private List<IHolder> CreateItemCollection (IEnumerable<TimeEntryHolder> timeHolders)
        {
            return grouper.Group (timeHolders)
                   .OrderByDescending (x => x.GetStartTime ())
                   .GroupBy (x => x.GetStartTime ().ToLocalTime ().Date)
                   .SelectMany (gr => gr.Cast<IHolder>().Prepend (new DateHolder (gr.Key, gr.Cast<ITimeEntryHolder> ())))
                   .ToList ();
        }

        public void RestoreTimeEntryFromUndo ()
        {
            RxChain.Send (new DataMsg.TimeEntriesRestoreFromUndo (lastRemovedItem.EntryCollection.Select (x => x.Data)));
        }

        public void RemoveTimeEntryWithUndo (ITimeEntryHolder timeEntryHolder)
        {
            if (timeEntryHolder == null) {
                return;
            }

            Action<ITimeEntryHolder> removeTimeEntryPermanently =
                holder => RxChain.Send (new DataMsg.TimeEntriesRemovePermanently (holder.EntryCollection.Select (x => x.Data)));

            System.Timers.ElapsedEventHandler undoTimerFinished = (sender, e) => {
                removeTimeEntryPermanently (lastRemovedItem);
                lastRemovedItem = null;
            };

            // Remove previous if exists
            if (lastRemovedItem != null) {
                removeTimeEntryPermanently (lastRemovedItem);
            }

            if (timeEntryHolder.Entry.Data.State == TimeEntryState.Running) {
                RxChain.Send (new DataMsg.TimeEntryStop (timeEntryHolder.Entry.Data));
            }
            lastRemovedItem = timeEntryHolder;

            RxChain.Send (new DataMsg.TimeEntriesRemoveWithUndo (timeEntryHolder.EntryCollection.Select (x => x.Data)));

            // Create Undo timer
            if (undoTimer != null) {
                undoTimer.Elapsed += undoTimerFinished;
                undoTimer.Close ();
            }
            // Using the correct timer.
            undoTimer = new System.Timers.Timer ((Literals.TimeEntryRemoveUndoSeconds + 1) * 1000);
            undoTimer.AutoReset = false;
            undoTimer.Elapsed += undoTimerFinished;
            undoTimer.Start ();
        }
    }
}
