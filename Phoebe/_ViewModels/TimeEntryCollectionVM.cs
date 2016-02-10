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
                StoreManager
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

		private void UpdateItems (TimerState state)
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
            // Use DataDir.Incoming to prevent SyncOut from sending the message
            var msg = new TimeEntryMsg (DataDir.Incoming, lastRemovedItem.DataCollection);
            RxChain.Send (this.GetType (), DataTag.TimeEntryUpdateOnlyAppState, msg);
		}

		public void RemoveTimeEntryWithUndo (ITimeEntryHolder timeEntryHolder)
		{
			if (timeEntryHolder == null) {
				return;
			}

			Action<ITimeEntryHolder> removeTimeEntryPermanently = holder => {
                var msg = new TimeEntryMsg (DataDir.Outcoming, holder.DataCollection);
                RxChain.Send (this.GetType (), DataTag.TimeEntryUpdate, msg);
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
                TimeEntryMsg.StopAndSend (timeEntryHolder.Data);
			}
			lastRemovedItem = timeEntryHolder;

            // Use DataDir.Incoming to prevent SyncOut from sending the message
			var rmMsg = new TimeEntryMsg (DataDir.Incoming, timeEntryHolder.DataCollection);
            RxChain.Send (this.GetType (), DataTag.TimeEntryUpdateOnlyAppState, rmMsg);

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
