using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Toggl.Phoebe.Data.Utils;
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
            disposable =
                AppStateManager
                    .Singleton
                    .Observe (app => app.TimerState)
                    // TODO: Recover buffer?
//			        .TimedBuffer (bufferMilliseconds)
                    .Subscribe (UpdateItems);
		}

		public void Dispose ()
		{
			if (disposable != null) {
				disposable.Dispose ();
				disposable = null;
			}
		}

		private void UpdateItems (ITimerState state)
		{
			try {
                var timeHolders = state.TimeEntries.Values.Select (x => new TimeEntryHolder (x.Data, x.Info));

				// Create the new item collection from holders (sort and add headers...)
				var newItemCollection = CreateItemCollection (timeHolders);

				// Check diffs, modify ItemCollection and notify changes
				var diffs = Diff.Calculate (Items, newItemCollection);

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
			}
			catch (Exception ex) {
				var log = ServiceContainer.Resolve<ILogger> ();
				log.Error (GetType ().Name, ex, "Failed to update collection");
			}
		}

		public IList<IHolder> CreateItemCollection (IEnumerable<TimeEntryHolder> timeHolders)
		{
			return grouper.Group (timeHolders)
				   .OrderByDescending (x => x.GetStartTime ())
				   .GroupBy (x => x.GetStartTime ().ToLocalTime ().Date)
				   .SelectMany (gr => gr.Cast<IHolder> ().Prepend (new DateHolder (gr.Key, gr)))
				   .ToList ();
		}

		public void RestoreTimeEntryFromUndo ()
		{
			var msg = new TimeEntryMsg (DataDir.None, DataAction.Put, lastRemovedItem.Data);
            RxChain.Send (this.GetType (), DataTag.TimeEntryRestoreFromUndo, msg);
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
				}
				else {
					entries = new[] { holder.Data };
				}

				var msg = new TimeEntryMsg (DataDir.Outcoming, entries.Select (
					x => Tuple.Create (DataAction.Delete, x)));
                
                RxChain.Send (this.GetType (), DataTag.TimeEntryRemove, msg);
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
				var msg = new TimeEntryMsg (DataDir.Outcoming, DataAction.Put, timeEntryHolder.Data);
                RxChain.Send (this.GetType (), DataTag.TimeEntryStop, msg);
			}
			lastRemovedItem = timeEntryHolder;

			// Remove item only from list
			var rmMsg = new TimeEntryMsg (DataDir.None, DataAction.Delete, timeEntryHolder.Data);
            RxChain.Send (this.GetType (), DataTag.TimeEntryRemoveWithUndo, rmMsg);

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
