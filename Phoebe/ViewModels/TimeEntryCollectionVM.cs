using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using Toggl.Phoebe.Logging;
using Toggl.Phoebe.Data.Diff;
using Toggl.Phoebe.Helpers;
using Toggl.Phoebe.Reactive;
using Toggl.Phoebe.ViewModels.Timer;
using XPlatUtils;
using System.Threading;

namespace Toggl.Phoebe.ViewModels
{
    public class TimeEntryCollectionVM : ObservableRangeCollection<IHolder>, IDisposable
    {
        IDisposable disposable;
        readonly TimeEntryGrouper grouper;

        public TimeEntryCollectionVM (TimeEntryGroupMethod groupMethod, SynchronizationContext uiContext)
        {
            grouper = new TimeEntryGrouper (groupMethod);
            disposable = StoreManager
                         .Singleton
                         .Observe (x => x.State.TimeEntries)
                         .ObserveOn (uiContext)
                         .DistinctUntilChanged ()
                         .SelectMany (x => GetDiffsFromNewValues (this, x.Values))
                         .Subscribe (diffs => UpdateCollection (diffs));
        }

        public void Dispose ()
        {
            if (disposable != null) {
                disposable.Dispose ();
                disposable = null;
            }
        }

        private IObservable<IList<DiffSection<IHolder>>> GetDiffsFromNewValues (IList<IHolder> currentHolders, IEnumerable<RichTimeEntry> entries)
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

                return Observable.Return (diffs);
            } catch (Exception ex) {
                var log = ServiceContainer.Resolve<ILogger> ();
                log.Error (GetType ().Name, ex, "Failed to update collection");
                return Observable.Return (new List<DiffSection<IHolder>> ());
            }
        }

        private void UpdateCollection (IList<DiffSection<IHolder>> diffs)
        {
            Console.WriteLine ("updates: " + diffs.Count);

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
        }

        private List<IHolder> CreateItemCollection (IEnumerable<TimeEntryHolder> timeHolders)
        {
            return grouper.Group (timeHolders)
                   .OrderByDescending (x => x.GetStartTime ())
                   .GroupBy (x => x.GetStartTime ().ToLocalTime ().Date)
                   .SelectMany (gr => gr.Cast<IHolder>().Prepend (new DateHolder (gr.Key, gr.Cast<ITimeEntryHolder> ())))
                   .ToList ();
        }
    }
}
