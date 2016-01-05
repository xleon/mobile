using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Toggl.Phoebe.Data.DataObjects;
using Toggl.Phoebe.Data.Utils;
using Toggl.Phoebe.Logging;
using XPlatUtils;

namespace Toggl.Phoebe.Data.Utils
{
    public class TimeEntriesCollection : ObservableRangeCollection<IHolder>, IDisposable
    {
        private IDisposable disposable;
        private readonly bool isGrouped;
        private readonly bool loadTimeEntryInfo;

        public IEnumerable<IHolder> Data
        {
            get { return Items; }
        }

        public TimeEntriesCollection (IObservable<TimeEntryMessage> feed, bool isGrouped,
                                      int bufferMilliseconds = 500, bool useThreadPool = true, bool loadTimeEntryInfo = true)
        {
            this.isGrouped = isGrouped;
            this.loadTimeEntryInfo = loadTimeEntryInfo;

            disposable =
                feed.Synchronize (useThreadPool ? (IScheduler)Scheduler.Default : Scheduler.CurrentThread)
                .TimedBuffer (bufferMilliseconds)
                // SelectMany would process tasks in parallel, see https://goo.gl/eayv5N
                .Select (msgs => Observable.FromAsync (() => UpdateItemsAsync (msgs)))
                .Concat ()
            .Catch ((Exception ex) => {
                var log = ServiceContainer.Resolve<ILogger> ();
                log.Error (GetType ().Name, ex, "Failed to update collection");
                return Observable.Empty<Unit> ();
            })
            .Subscribe ();
        }

        public void Dispose ()
        {
            if (disposable != null) {
                disposable.Dispose ();
                disposable = null;
            }
        }

        private async Task UpdateItemsAsync (IEnumerable<TimeEntryMessage> msgs)
        {
            // 1. Get only TimeEntryHolders from current collection
            var timeHolders = Items.OfType<ITimeEntryHolder> ().ToList ();

            // 2. Remove, replace or add items from messages
            foreach (var msg in msgs) {
                UpdateTimeHoldersAsync (timeHolders, msg.Data, msg.Action);
            }

            // 3. Create the new item collection from holders (sort and add headers...)
            var newItemCollection = await CreateItemCollection (timeHolders, loadTimeEntryInfo);

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
        }

        private void UpdateTimeHoldersAsync (IList<ITimeEntryHolder> timeHolders, TimeEntryData entry, DataAction action)
        {
            if (action == DataAction.Put) {
                var foundIndex = timeHolders.IndexOf (x => x.IsAffectedByPut (entry));
                if (foundIndex > -1) {
                    timeHolders [foundIndex] = CreateTimeHolder (isGrouped, entry, timeHolders [foundIndex]); // Replace
                } else {
                    timeHolders.Add (CreateTimeHolder (isGrouped, entry)); // Insert
                }
            } else {
                bool isAffectedByDelete;
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

        public static async Task<IList<IHolder>> CreateItemCollection (IEnumerable<ITimeEntryHolder> timeHolders, bool loadTimeEntryInfo)
        {
            var timeHolders2 = timeHolders;
            if (loadTimeEntryInfo) {
                timeHolders2 = await Task.WhenAll (timeHolders.Select (async x => {
                    await x.LoadInfoAsync ();
                    return x;
                }));
            }

            return timeHolders2
                   .OrderByDescending (x => x.GetStartTime ())
                   .GroupBy (x => x.GetStartTime ().ToLocalTime().Date)
                   .SelectMany (gr => gr.Cast<IHolder>().Prepend (new DateHolder (gr.Key, gr)))
                   .ToList ();
        }

        public static ITimeEntryHolder CreateTimeHolder (bool isGrouped, TimeEntryData entry, ITimeEntryHolder previous = null)
        {
            return isGrouped
                   ? (ITimeEntryHolder)new TimeEntryGroup (entry, previous)
                   : new TimeEntryHolder (entry);
        }
    }
}
