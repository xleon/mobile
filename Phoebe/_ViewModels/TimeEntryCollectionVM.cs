using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using Toggl.Phoebe.Data;
using Toggl.Phoebe.Data.DataObjects;
using Toggl.Phoebe.Data.Utils;
using Toggl.Phoebe.Helpers;
using Toggl.Phoebe.Logging;
using Toggl.Phoebe.Models;
using XPlatUtils;

namespace Toggl.Phoebe.ViewModels
{
    public class TimeEntryCollectionVM : ObservableRangeCollection<IHolder>, ICollectionData<IHolder>
    {
        public const int UndoSecondsInterval = 5;

        IDisposable disposable;
        ITimeEntryHolder lastRemovedItem;
        TimeEntryGrouper grouper;
        System.Timers.Timer undoTimer = new System.Timers.Timer ();

        public event EventHandler<Either<DateTime, Exception>> LoadFinished;

        public IEnumerable<IHolder> Data
        {
            get { return Items; }
        }

        public TimeEntryCollectionVM (TimeEntryGroupMethod groupMethod, int bufferMilliseconds = 500)
        {
            this.grouper = new TimeEntryGrouper (groupMethod);
            disposable = Store
                         .Observe<TimeEntryMsg> ()
                         .TimedBuffer (bufferMilliseconds)
                         .Subscribe (HandleStoreResults);
        }

        public void Dispose ()
        {
            if (disposable != null) {
                disposable.Dispose ();
                disposable = null;
            }
        }

        private void HandleStoreResults (IList<DataMsg<TimeEntryMsg>> results)
        {
            var resultsGroup = results.Select (x => x.Data).Split ();

            var finalResult = resultsGroup.Left.Count == 0
                              ? Either<DateTime, Exception>.Right (resultsGroup.Right.LastOrDefault ())
                              : UpdateItems (TimeEntryMsg.Aggregate (resultsGroup.Left));

            if (LoadFinished != null) {
                LoadFinished (this, finalResult);
            }
        }

        private Either<DateTime, Exception> UpdateItems (TimeEntryMsg load)
        {
            try {
                // 1. Get only TimeEntryHolders from current collection
                var timeHolders = grouper.Ungroup (Items.OfType<ITimeEntryHolder> ()).ToList ();

                // 2. Remove, replace or add items from messages
                foreach (var msg in load.Messages) {
                    UpdateTimeHolders (timeHolders, msg.Item1, msg.Item2);
                }

                // 3. Create the new item collection from holders (sort and add headers...)
                var newItemCollection = CreateItemCollection (timeHolders);

                // 4. Check diffs, modify ItemCollection and notify changes
                var diffs = Diff.Calculate (Items, newItemCollection);

                // CollectionChanged events must be fired on UI thread
                ServiceContainer.Resolve<IPlatformUtils>().DispatchOnUIThread (() => {
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
                return Either<DateTime, Exception>.Left (DateTime.UtcNow);
            } catch (Exception ex) {
                ServiceContainer.Resolve<ILogger> ().Error (
                    GetType ().Name, ex, "Failed to update collection");
                return Either<DateTime, Exception>.Right (ex);
            }
        }

        private void UpdateTimeHolders (IList<TimeEntryHolder> timeHolders, TimeEntryData entry, DataAction action)
        {
            for (var i = 0; i < timeHolders.Count; i++) {
                if (entry.Id == timeHolders [i].Data.Id) {
                    if (action == DataAction.Put) {
                        timeHolders [i] = new TimeEntryHolder (entry); // Replace
                    } else {
                        timeHolders.RemoveAt (i); // Remove
                    }
                    return;
                }
            }

            if (action == DataAction.Put) {
                timeHolders.Add (new TimeEntryHolder (entry)); // Add
            }
        }

        public IList<IHolder> CreateItemCollection (IEnumerable<TimeEntryHolder> timeHolders)
        {
            return grouper.Group (timeHolders)
                   .OrderByDescending (x => x.GetStartTime ())
                   .GroupBy (x => x.GetStartTime ().ToLocalTime().Date)
                   .SelectMany (gr => gr.Cast<IHolder>().Prepend (new DateHolder (gr.Key, gr.Cast<ITimeEntryHolder> ())))
                   .ToList ();
        }

        public void RestoreTimeEntryFromUndo ()
        {
            Dispatcher.Send (DataTag.RestoreTimeEntryFromUndo, lastRemovedItem.Data);
        }

        public void RemoveTimeEntryWithUndo (ITimeEntryHolder timeEntryHolder)
        {
            if (timeEntryHolder == null) {
                return;
            }

            Action<ITimeEntryHolder> removeTimeEntryPermanently = holder => {
                IList<TimeEntryData> entries = null;
                var groupHolder = holder as TimeEntryGroup;
                if (groupHolder != null) {
                    entries = groupHolder.DataCollection;
                } else {
                    entries = new [] { holder.Data };
                }
                Dispatcher.Send (DataTag.RemoveTimeEntryPermanently, entries);
            };

            System.Timers.ElapsedEventHandler undoTimerFinished = (sender, e) => {
                removeTimeEntryPermanently (lastRemovedItem);
                lastRemovedItem = null;
            };

            // Remove previous if exists
            if (lastRemovedItem != null) {
                removeTimeEntryPermanently (lastRemovedItem);
            }

            if (timeEntryHolder.Data.State == TimeEntryState.Running) {
                Dispatcher.Send (DataTag.StopTimeEntry, timeEntryHolder.Data);
            }
            lastRemovedItem = timeEntryHolder;

            // Remove item only from list
            Dispatcher.Send (DataTag.RemoveTimeEntryWithUndo, timeEntryHolder.Data);

            // Create Undo timer
            if (undoTimer != null) {
                undoTimer.Elapsed += undoTimerFinished;
                undoTimer.Close();
            }
            // Using the correct timer.
            undoTimer = new System.Timers.Timer ((UndoSecondsInterval + 1) * 1000);
            undoTimer.AutoReset = false;
            undoTimer.Elapsed += undoTimerFinished;
            undoTimer.Start();
        }
    }
}
