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

                case DataTag.TimeEntryUpdate:
                    return TimeEntryUpdate (msg, dataStore);

                // Don't touch the database
                case DataTag.TimeEntryUpdateOnlyAppState:
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
            var userId = ServiceContainer
                .Resolve<Toggl.Phoebe.Net.AuthManager> ()
                .GetUserId ();

            var dbEntries = dataStore
                .Table<TimeEntryData> ()
                .Where (r =>
                        r.State != TimeEntryState.New &&
                        r.StartTime >= startDate && r.StartTime < paginationDate &&
                        r.DeletedAt == null &&
                        r.UserId == userId)
                .Take (Literals.TimeEntryLoadMaxInit)
                .OrderByDescending (r => r.StartTime)
                .ToList ();

            // Try to update with latest data from server with old paginationDate to get the same data
            RxChain.Send (typeof(StoreRegister), DataTag.EmptyQueueAndSync, paginationDate);
            paginationDate = dbEntries.Count > 0 ? startDate : paginationDate;

            // TODO: Check if there're entries in the db that haven't been synced
            return DataMsg.Success (msg.Tag, new TimeEntryMsg (DataDir.Incoming, dbEntries));
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

            var msgs = dataStore.Update (ctx => {
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

            var entryMsg = new TimeEntryMsg (dir, msgs.Cast<TimeEntryData> ());
            return DataMsg.Success<TimeEntryMsg> (msg.Tag, entryMsg);
        }

        static IDataMsg TimeEntryUpdate (IDataMsg msg, ISyncDataStore dataStore)
        {
            try {
                var entryMsg = msg.ForceGetData<TimeEntryMsg> ();
                // TODO: Sanity check: DataDir.Outcoming, etc...

                var dbMsgs = dataStore.Update (ctx => {
                    foreach (var entry in entryMsg.Data) {
                        if (entry.DeletedAt == null) {
                            ctx.Put (entry);
                        }
                        else {
                            ctx.Delete (entry);
                        }
                    }

                    if (ctx.UpdatedItems.Count != entryMsg.Data.Count) {
                        var missing = entryMsg.Data
                            .Where(x => ctx.UpdatedItems.All (y => y.Id != x.Id))
                            .Select(x => x.Id);

                        // Throw an exception to roll back any change
                        throw new Exception(string.Format("Couldn't stop time entries: {0}",
                            string.Join (", ", missing))); 
                    }
                });

                return DataMsg.Success (msg.Tag, entryMsg);
            }
            catch (Exception ex) {
                return DataMsg.Error<TimeEntryMsg> (msg.Tag, ex);
            }
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

