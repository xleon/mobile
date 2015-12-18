using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Toggl.Phoebe.Data.DataObjects;
using Toggl.Phoebe.Data.Utils;
using Toggl.Phoebe.Data.Views;
using Toggl.Phoebe.Logging;
using XPlatUtils;

namespace Toggl.Phoebe.Data.Utils
{
    public class TimeEntriesCollection : ObservableRangeCollection<IHolder>, ICollectionData<IHolder>
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

        /// <summary>Only for testing purposes</summary>
        public static async Task<TimeEntriesCollection> InitAdHoc (
            IObservable<TimeEntryMessage> feed, bool isGrouped, int bufferMilliseconds, params TimeEntryData[] timeEntries)
        {
            var v = new TimeEntriesCollection (feed, isGrouped, bufferMilliseconds, false, false);

            if (timeEntries.Length > 0) {
                var holders = new List<ITimeEntryHolder> ();
                foreach (var entry in timeEntries) {
                    // Create a new entry to protect the reference;
                    var protectedEntry = new TimeEntryData (entry);
                    holders.Add (await v.CreateTimeHolder (isGrouped, protectedEntry));
                }
                v.Reset (v.CreateItemCollection (holders));
            }

            return v;
        }

        private async Task UpdateItemsAsync (IEnumerable<TimeEntryMessage> msgs)
        {
            // 1. Get only TimeEntryHolders from current collection
            var timeHolders = Items.OfType<ITimeEntryHolder> ().ToList ();

            // 2. Remove, replace or add items from messages
            foreach (var msg in msgs) {
                await UpdateTimeHoldersAsync (timeHolders, msg.Data, msg.Action);
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
        }

        private async Task UpdateTimeHoldersAsync (IList<ITimeEntryHolder> timeHolders, TimeEntryData entry, DataAction action)
        {
            var foundIndex = -1;
            for (var j = 0; j < timeHolders.Count; j++) {
                if (timeHolders [j].Matches (entry)) {
                    foundIndex = j;
                    break;
                }
            }

            if (foundIndex > -1) {
                if (action == DataAction.Put) {
                    timeHolders [foundIndex] = await CreateTimeHolder (isGrouped, entry, timeHolders [foundIndex]); // Replace
                } else {
                    timeHolders.RemoveAt (foundIndex); // Remove
                }
            } else {
                if (action == DataAction.Put) {
                    timeHolders.Add (await CreateTimeHolder (isGrouped, entry)); // Insert
                }
            }
        }

        private IList<IHolder> CreateItemCollection (IEnumerable<ITimeEntryHolder> timeHolders)
        {
            return timeHolders
                   .OrderByDescending (x => x.GetStartTime ())
                   .GroupBy (x => x.GetStartTime ().ToLocalTime().Date)
                   .SelectMany (gr => gr.Cast<IHolder>().Prepend (new DateHolder (gr.Key, gr)))
                   .ToList ();
        }

        public async Task<ITimeEntryHolder> CreateTimeHolder (
            bool isGrouped, TimeEntryData entry, ITimeEntryHolder previous = null)
        {
            var holder = isGrouped
                         ? (ITimeEntryHolder)new TimeEntryGroup (entry, previous)
                         : new TimeEntryHolder (entry);

            if (loadTimeEntryInfo) {
                await holder.LoadInfoAsync ();
            }

            return holder;
        }
    }
}
