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
using System.Threading.Tasks;

namespace Toggl.Phoebe.ViewModels
{
    public class TimeEntryCollectionVM : ObservableRangeCollection<IHolder>, ICollectionData<IHolder>
    {
        public class LoadFinishedArgs : EventArgs
        {
            public bool HasMore { get; set; }
            public bool HasErrors { get; set; }
        }

        IDisposable disposable;
        ITimeEntryHolder lastRemovedItem;
        TimeEntryGrouper grouper;
        System.Timers.Timer undoTimer = new System.Timers.Timer ();

        public event EventHandler<LoadFinishedArgs> LoadFinished;

        public IEnumerable<IHolder> Data
        {
            get { return Items; }
        }

        public TimeEntryCollectionVM (TimeEntryGroupMethod groupMethod, int bufferMilliseconds = 500)
        {
            this.grouper = new TimeEntryGrouper (groupMethod);
            disposable = Store.Singleton
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

        private async void HandleStoreResults (IList<DataMsg<TimeEntryMsg>> results)
        {
            var resultsGroup = results.Select (x => x.Data).Split ();

            if (resultsGroup.Left.Count > 0) {
                await UpdateItems (resultsGroup.Left.SelectMany (x => x));

                // If we've received non-empty messages from server (DataDir.Incoming)
                // this means there're more entries available
                var hasMore = resultsGroup.Left.Any (
                        x => x.Dir == DataDir.Incoming && x.Count > 0);

                LoadFinished.SafeInvoke (this, new LoadFinishedArgs { HasMore = hasMore });
            } else if (resultsGroup.Right.Count > 0) {
                LoadFinished.SafeInvoke (this, new LoadFinishedArgs { HasErrors = true });
            }
        }

        private async Task UpdateItems (IEnumerable<Tuple<DataVerb, TimeEntryData>> msgs)
        {
            try {
                // 1. Get only TimeEntryHolders from current collection
                var timeHolders = grouper.Ungroup (Items.OfType<ITimeEntryHolder> ()).ToList ();

                // 2. Remove, replace or add items from messages
                foreach (var msg in msgs) {
                    UpdateTimeHolders (timeHolders, msg.Item1, msg.Item2);
                }

                // TODO: Temporary, every access to the database should be done in the Store component
                foreach (var holder in timeHolders) {
                    if (holder.Info == null) {
                        holder.Info = await StoreRegister.LoadTimeEntryInfoAsync (holder.Data);
                    }
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
            } catch (Exception ex) {
                var log = ServiceContainer.Resolve<ILogger> ();
                log.Error (GetType ().Name, ex, "Failed to update collection");
            }
        }

        private void UpdateTimeHolders (IList<TimeEntryHolder> timeHolders, DataVerb action, TimeEntryData entry)
        {
            for (var i = 0; i < timeHolders.Count; i++) {
                if (entry.Id == timeHolders [i].Data.Id) {
                    if (action == DataVerb.Put) {
                        timeHolders [i] = new TimeEntryHolder (entry); // Replace
                    } else if (action == DataVerb.Delete) {
                        timeHolders.RemoveAt (i); // Remove
                    }
                    return;
                }
            }

            if (action == DataVerb.Put || action == DataVerb.Post) {
                timeHolders.Add (new TimeEntryHolder (entry)); // Add
            }
        }

        public IList<IHolder> CreateItemCollection (IEnumerable<TimeEntryHolder> timeHolders)
        {
            return grouper.Group (timeHolders)
                   .OrderByDescending (x => x.GetStartTime ())
                   .GroupBy (x => x.GetStartTime ().ToLocalTime().Date)
                   .SelectMany (gr => gr.Cast<IHolder>().Prepend (new DateHolder (gr.Key, gr)))
                   .ToList ();
        }

        public void RestoreTimeEntryFromUndo ()
        {
            var msg = new TimeEntryMsg (DataDir.None, DataVerb.Put, lastRemovedItem.Data);
            Dispatcher.Singleton.Send (DataTag.TimeEntryRestoreFromUndo, msg);
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

                var msg = new TimeEntryMsg (DataDir.Outcoming, entries.Select (
                    x => Tuple.Create (DataVerb.Delete, x)));
                Dispatcher.Singleton.Send (DataTag.TimeEntryRemove, msg);
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
                var msg = new TimeEntryMsg (DataDir.Outcoming, DataVerb.Put, timeEntryHolder.Data);
                Dispatcher.Singleton.Send (DataTag.TimeEntryStop, msg);
            }
            lastRemovedItem = timeEntryHolder;

            // Remove item only from list
            var rmMsg = new TimeEntryMsg (DataDir.None, DataVerb.Delete, timeEntryHolder.Data);
            Dispatcher.Singleton.Send (DataTag.TimeEntryRemoveWithUndo, rmMsg);

            // Create Undo timer
            if (undoTimer != null) {
                undoTimer.Elapsed += undoTimerFinished;
                undoTimer.Close();
            }
            // Using the correct timer.
            undoTimer = new System.Timers.Timer ((Literals.TimeEntryRemoveUndoSeconds + 1) * 1000);
            undoTimer.AutoReset = false;
            undoTimer.Elapsed += undoTimerFinished;
            undoTimer.Start();
        }
    }
}
