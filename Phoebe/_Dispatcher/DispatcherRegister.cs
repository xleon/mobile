using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using Toggl.Phoebe.Logging;
using Toggl.Phoebe.Net;
using XPlatUtils;
using Toggl.Phoebe.Data.Json;
using Toggl.Phoebe.Data.Json.Converters;
using Toggl.Phoebe.Helpers;
using Toggl.Phoebe.Data.DataObjects;

namespace Toggl.Phoebe
{
    public static class DispatcherRegister
    {
        public static Func<IDataMsg, Task<IDataMsg>> GetAction (DataTag tag)
        {
            switch (tag) {
            case DataTag.TimeEntryLoadFromServer:
                return TimeEntryLoadFromServer;

            case DataTag.TimeEntryLoad:
            case DataTag.TimeEntryStop:
            case DataTag.TimeEntryRemoveWithUndo:
            case DataTag.TimeEntryRestoreFromUndo:
            case DataTag.TimeEntryRemove:
                return LetGoThrough;

            default:
                throw new ActionNotFoundException (tag, typeof (DispatcherRegister));
            }
        }

        static async Task<IDataMsg> TimeEntryLoadFromServer (IDataMsg msg)
        {
            const int daysLoad = Literals.TimeEntryLoadDays;
            var startFrom = msg.ForceGetWrappedData<DateTime> ();

            try {
                // Download new Entries
                var client = ServiceContainer.Resolve<ITogglClient> ();
                var jsonEntries = await client.ListTimeEntries (startFrom, daysLoad);

                // TODO: Temporary, every database writing should be done in the Store component
                // Move it once, the JSON and DB serialization have been separated
                var msgs = await ServiceContainer.Resolve<Toggl.Phoebe.Data.IDataStore> ()
                    .ExecuteInTransactionWithMessagesAsync (ctx =>
                        jsonEntries.ForEach (json => json.Import (ctx)));

                var dataMsgs = msgs.Select (x => new DataActionMsg<TimeEntryData> (x)).ToList ();

                return DataMsg.Success (dataMsgs, msg.Tag, DataDir.Incoming);

            } catch (Exception exc) {
                var tag = typeof (DispatcherRegister).Name;
                var log = ServiceContainer.Resolve<ILogger> ();
                const string errorMsg = "Failed to fetch time entries {1} days up to {0}";

                if (exc.IsNetworkFailure () || exc is TaskCanceledException) {
                    log.Info (tag, exc, errorMsg, startFrom, daysLoad);
                } else {
                    log.Warning (tag, exc, errorMsg, startFrom, daysLoad);
                }

                return DataMsg.Error<TimeEntryData> (exc, msg.Tag, DataDir.Incoming);
            }
        }

        public static Task<IDataMsg> LetGoThrough (IDataMsg msg)
        {
            return Task.Run (() => msg);
        }
    }
}
