using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using Toggl.Phoebe.Data.Utils;
using Toggl.Phoebe.Helpers;
using Toggl.Phoebe.Logging;
using Toggl.Phoebe.Models;
using XPlatUtils;
using Toggl.Phoebe.Data;

namespace Toggl.Phoebe.ViewModels
{
    public class TimeEntriesCollectionVM : ObservableRangeCollection<IHolder>, ICollectionData<IHolder>
    {   
        private IDisposable disposable;
        private readonly bool isGrouped;

        public event EventHandler<Either<Unit, string>> LoadFinished;

        public IEnumerable<IHolder> Data
        {
            get { return Items; }
        }

        public TimeEntriesCollectionVM (bool isGrouped, int bufferMilliseconds = 500)
        {
            this.isGrouped = isGrouped;

            disposable = Store
                .Observe<TimeEntry> ()
                .TimedBuffer (bufferMilliseconds)
                .Select (x => HandleStoreResults (x))
                .Subscribe ();
        }

        public void Dispose ()
        {
            if (disposable != null) {
                disposable.Dispose ();
                disposable = null;
            }
        }

        private void HandleStoreResults (IList<StoreResult<TimeEntry>> results)
        {
            var resultsGroup = results.Select (x => x.Data).Split ();

            var finalResult = resultsGroup.Left.Count == 0
                ? Either<Unit,string>.Right (resultsGroup.Right.LastOrDefault ())
                : UpdateItems (resultsGroup.Left.SelectMany (x => x));

            if (LoadFinished != null) {
                LoadFinished (finalResult);
            }
        }

        private Either<Unit,string> UpdateItems (IEnumerable<StoreMsg<TimeEntry>> msgs)
        {
            if (resultsGroup.Left.Count == 0) {
                var error = resultsGroup.Right.LastOrDefault ();
                if (error != null && LoadFinished != null) {
                    LoadFinished (new ErrorEventArgs (error));
                }
            }
            else {
                UpdateItems (resultsGroup.Left.SelectMany (x => x));
            }
        }

        private Either<Unit,string> UpdateItems (IEnumerable<StoreMsg<TimeEntry>> msgs)
        {
            try {
                // 1. Get only TimeEntryHolders from current collection
                var timeHolders = Items.OfType<ITimeEntryHolder> ().ToList ();

                // 2. Remove, replace or add items from messages
                foreach (var msg in msgs) {
                    UpdateTimeHolders (timeHolders, msg.Data, msg.Action);
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
                return Either<Unit,string>.Left (new Unit ());
            }
            catch (Exception ex) {
                var log = ServiceContainer.Resolve<ILogger> ();
                log.Error (GetType ().Name, ex, "Failed to update collection");
                return Either<Unit,string>.Right (ex.Message);
            }
        }

        private void UpdateTimeHolders (IList<ITimeEntryHolder> timeHolders, TimeEntry entry, DataAction action)
        {
            if (action == DataAction.Put) {
                var foundIndex = timeHolders.IndexOf (x => x.IsAffectedByPut (entry));
                if (foundIndex > -1) {
                    timeHolders [foundIndex] = CreateTimeHolder (isGrouped, entry, timeHolders [foundIndex]); // Replace
                } else {
                    timeHolders.Add (CreateTimeHolder (isGrouped, entry)); // Insert
                }
            } else {
                var isAffectedByDelete = false;
                for (var i = 0; i < timeHolders.Count; i++) {
                    var updatedHolder = timeHolders [i].UpdateOrDelete (entry, out isAffectedByDelete);

                    if (isAffectedByDelete) {
                        if (updatedHolder == null) {
                            timeHolders.RemoveAt (i); // Remove
                        } else {
                            timeHolders [i] = updatedHolder; // Replace
                        }
                        break;
                    }
                }
            }
        }

        public static IList<IHolder> CreateItemCollection (IEnumerable<ITimeEntryHolder> timeHolders)
        {
            return timeHolders
                .OrderByDescending (x => x.GetStartTime ())
                .GroupBy (x => x.GetStartTime ().ToLocalTime().Date)
                .SelectMany (gr => gr.Cast<IHolder>().Prepend (new DateHolder (gr.Key, gr)))
                .ToList ();
        }

        public static ITimeEntryHolder CreateTimeHolder (bool isGrouped, TimeEntry entry, ITimeEntryHolder previous = null)
        {
            return isGrouped
                ? (ITimeEntryHolder)new TimeEntryGroup (entry, previous)
                    : new TimeEntryHolder (entry);
        }
    }
}
