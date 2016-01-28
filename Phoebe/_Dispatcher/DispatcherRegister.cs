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
        public static Task<IDataMsg> ResolveAction (IDataMsg msg)
        {
            switch (msg.Tag) {
            case DataTag.TimeEntryLoadFromServer:
                return TimeEntryLoadFromServer (msg);

            case DataTag.TimeEntryLoad:
            case DataTag.TimeEntryStop:
            case DataTag.TimeEntryRemoveWithUndo:
            case DataTag.TimeEntryRestoreFromUndo:
            case DataTag.TimeEntryRemove:
                return LetGoThrough (msg);

            default:
                throw new ActionNotFoundException (msg.Tag, typeof (DispatcherRegister));
            }
        }

        static async Task<IDataMsg> TimeEntryLoadFromServer (IDataMsg msg)
        {
            const int daysLoad = Literals.TimeEntryLoadDays;
            var startFrom = msg.ForceGetData<DateTime> ();

            try {
                // Download new Entries
                var client = ServiceContainer.Resolve<ITogglClient> ();
                var jsonEntries = await client.ListTimeEntries (startFrom, daysLoad);

                return DataMsg.Success (msg.Tag, jsonEntries);

            } catch (Exception exc) {
                var tag = typeof (DispatcherRegister).Name;
                var log = ServiceContainer.Resolve<ILogger> ();
                const string errorMsg = "Failed to fetch time entries {1} days up to {0}";

                if (exc.IsNetworkFailure () || exc is TaskCanceledException) {
                    log.Info (tag, exc, errorMsg, startFrom, daysLoad);
                } else {
                    log.Warning (tag, exc, errorMsg, startFrom, daysLoad);
                }

                return DataMsg.Error<TimeEntryData> (msg.Tag, exc);
            }
        }

        public static Task<IDataMsg> LetGoThrough (IDataMsg msg)
        {
            return Task.Run (() => msg);
        }
    }
}
