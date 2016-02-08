using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Toggl.Phoebe.Logging;
using Toggl.Phoebe._Data;
using Toggl.Phoebe._Data.Json;
using Toggl.Phoebe._Data.Models;
using Toggl.Phoebe._Helpers;
using Toggl.Phoebe._Net;
using Toggl.Phoebe._ViewModels.Timer;
using XPlatUtils;

namespace Toggl.Phoebe._Reactive
{
    public static class StoreRegister
    {
        public static IDataMsg ResolveAction (IDataMsg msg, ISyncDataStore dataStore)
        {
            switch (msg.Tag) {
                case DataTag.TimeEntryLoad:
                    return TimeEntryLoad (msg, dataStore);

                case DataTag.TimeEntryReceivedFromServer:
                    return TimeEntryReceivedFromServer (msg, dataStore);

                case DataTag.TimeEntryStop:
                    return TimeEntryStop (msg, dataStore);

                case DataTag.TimeEntryRemove:
                    return TimeEntryRemove (msg, dataStore);

                // These operations don't really do anything with the database
                case DataTag.TimeEntryRemoveWithUndo:
                case DataTag.TimeEntryRestoreFromUndo:
                case DataTag.TestSyncOutManager:
                    return msg;
                    
                default:
                    throw new ActionNotFoundException (msg.Tag, typeof (StoreRegister));
            }
        }
        // Set initial pagination Date to the beginning of the next day.
        // So, we can include all entries created -Today-.
        static DateTime paginationDate = Time.UtcNow.Date.AddDays (1);
        static IDataMsg TimeEntryLoad (IDataMsg msg, ISyncDataStore dataStore)
        {
            var startDate = GetDatesByDays (dataStore, paginationDate, Literals.TimeEntryLoadDays);

            // Always fall back to local data:
            var userId = ServiceContainer.Resolve<Toggl.Phoebe.Net.AuthManager> ().GetUserId ();
            var baseQuery =
                dataStore.Table<TimeEntryData> ()
                .Where (r =>
                        r.State != TimeEntryState.New &&
                        r.StartTime >= startDate && r.StartTime < paginationDate &&
                        r.DeletedAt == null &&
                        r.UserId == userId)
                .Take (Literals.TimeEntryLoadMaxInit);

            var dbMsgs = baseQuery
                .OrderByDescending (r => r.StartTime)
                .ToList ()  // Force SQL execution
                .Select (x => Tuple.Create (DataAction.Put, x))
                .ToList ();

            // Try to update with latest data from server with old paginationDate to get the same data
            RxChain.Send (typeof(StoreRegister), DataTag.EmptyQueueAndSync, paginationDate);
            paginationDate = dbMsgs.Count > 0 ? startDate : paginationDate;

            // TODO: Check if there're entries in the db that haven't been synced
            return DataMsg.Success (msg.Tag, new TimeEntryMsg (DataDir.Incoming, dbMsgs));
        }

        static IDataMsg TimeEntryReceivedFromServer (IDataMsg msg, ISyncDataStore dataStore)
        {
            var jsonEntries = msg.GetDataOrDefault<List<TimeEntryJson>> ();
            if (jsonEntries == null) {
                // TODO: Error management
            }

            // TODO: Error management when mapping JSON or storing in db
            var mapper = new JsonMapper ();
            var dir = DataDir.Incoming;

            var msgs = dataStore.Update (dir, ctx => {
                foreach (var jsonEntry in jsonEntries) {
                    var dataEntry = mapper.Map<TimeEntryData> (jsonEntry);

                    if (dataEntry.DeletedAt != null) {
                        ctx.Delete (dataEntry);
                    }
                    else {
                        ctx.Put (dataEntry);
                    }
                }
            });

            var entryMsg = new TimeEntryMsg (dir,
                msgs.Select (x => Tuple.Create (x.Action, (TimeEntryData)x.Data)));
            return DataMsg.Success<TimeEntryMsg> (msg.Tag, entryMsg);
        }

        static IDataMsg TimeEntryStop (IDataMsg msg, ISyncDataStore dataStore)
        {
            try {
                var dir = DataDir.Outcoming;
                var entryMsg = msg.ForceGetData<TimeEntryMsg> ();

                var dbMsgs = dataStore.Update (dir, ctx => {
                    foreach (var tuple in entryMsg) {
                        var entryData = tuple.Item2;

                        // Code from TimeEntryModel.StopAsync
                        if (entryData.State != TimeEntryState.Running) {
                            throw new InvalidOperationException (
                                String.Format ("Cannot stop a time entry ({0}) in {1} state.",
                                    entryData.Id, entryData.State));
                        }

                        entryData.State = TimeEntryState.Finished;
                        entryData.StopTime = Time.UtcNow;
                        ctx.Put (entryData);
                    }

                    if (ctx.Messages.Count != entryMsg.Count) {
                        var missing = entryMsg
                            .Where(x => ctx.Messages.All (y => y.Data.Id != x.Item2.Id))
                            .Select(x => x.Item2.Id);

                        // Throw an exception to roll back any change
                        throw new Exception(string.Format("Couldn't stop time entries: {0}",
                            string.Join (", ", missing))); 
                    }
                });

                return DataMsg.Success (msg.Tag, new TimeEntryMsg (DataDir.Outcoming, entryMsg));
            }
            catch (Exception ex) {
                return DataMsg.Error<TimeEntryMsg> (msg.Tag, ex);
            }
        }

        static IDataMsg TimeEntryRemove (IDataMsg msg, ISyncDataStore dataStore)
        {
            var dir = DataDir.Outcoming;
            var entryMsg = msg.ForceGetData<TimeEntryMsg> ();

            var msgs = dataStore.Update (dir, ctx => {
                foreach (var tuple in entryMsg) {
                    ctx.Delete (tuple.Item2);
                }
            });

            var removed = msgs
                .Select (x => x.Data as TimeEntryData)
                // If the entry wasn't synced with the server we don't need to notify
                // (the entry was already removed in the view at the start of the undo timeout)
                .Where (x => x.RemoteId != null)
                .Select (x => Tuple.Create (DataAction.Delete, x));

            return DataMsg.Success (msg.Tag, new TimeEntryMsg (dir, removed));
        }

        #region Util
        // TODO: replace this method with the SQLite equivalent.
        static DateTime GetDatesByDays (ISyncDataStore dataStore, DateTime startDate, int numDays)
        {
            var baseQuery = dataStore.Table<TimeEntryData> ().Where (
                r => r.State != TimeEntryState.New &&
                r.StartTime < startDate &&
                r.DeletedAt == null);

            var entries = baseQuery.ToList ();
            if (entries.Count > 0) {
                var group = entries
                    .OrderByDescending (r => r.StartTime)
                    .GroupBy (t => t.StartTime.Date)
                    .Take (numDays)
                    .LastOrDefault ();
                return group.Key;
            }
            return DateTime.MinValue;
        }
        #endregion
    }
}

