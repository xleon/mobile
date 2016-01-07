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
    public class TimeEntriesCollection<T>
        : ObservableRangeCollection<IHolder>, ICollectionData<IHolder> where T : ITimeEntryHolder
    {
        private IDisposable disposable;
        private readonly bool loadTimeEntryInfo;
        private readonly IGrouper<TimeEntryHolder, T> grouper;

        public IEnumerable<IHolder> Data
        {
            get { return Items; }
        }

        public TimeEntriesCollection (IObservable<TimeEntryMessage> feed, int bufferMilliseconds = 500, bool useThreadPool = true, bool loadTimeEntryInfo = true)
        {
            this.grouper = CreateGrouper ();;
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
            var timeHolders = grouper.Ungroup (Items.OfType<T> ()).ToList ();

            // 2. Remove, replace or add items from messages
            foreach (var msg in msgs) {
                UpdateTimeHolders (timeHolders, msg.Data, msg.Action);
            }

            // 3. Create the new item collection from holders (sort and add headers...)
            var newItemCollection = await CreateItemCollectionAsync (timeHolders, loadTimeEntryInfo);

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

        private void UpdateTimeHolders (List<TimeEntryHolder> timeHolders, TimeEntryData entry, DataAction action)
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

        public async Task<IList<IHolder>> CreateItemCollectionAsync (IEnumerable<TimeEntryHolder> timeHolders, bool loadTimeEntryInfo)
        {
            IEnumerable<T> timeHolders2 = null;
            if (loadTimeEntryInfo) {
                timeHolders2 = await Task.WhenAll (grouper.Group (timeHolders).Select (async x => {
                    await x.LoadInfoAsync ();
                    return x;
                }));
            } else {
                timeHolders2 = grouper.Group (timeHolders);
            }

            return timeHolders2
                   .OrderByDescending (x => x.GetStartTime ())
                   .GroupBy (x => x.GetStartTime ().ToLocalTime().Date)
                   .SelectMany (gr => gr.Cast<IHolder>().Prepend (new DateHolder (gr.Key, gr.Cast<ITimeEntryHolder> ())))
                   .ToList ();
        }

        private IGrouper<TimeEntryHolder, T> CreateGrouper ()
        {
            if (typeof (T) == typeof (TimeEntryGroup)) {
                return (IGrouper<TimeEntryHolder, T>)new TimeEntryGroup.Grouper ();
            } else if (typeof (T) == typeof (TimeEntryHolder)) {
                return (IGrouper<TimeEntryHolder, T>)new TimeEntryHolder.Grouper ();
            } else {
                throw new NotSupportedException ();
            }
        }
    }
}
