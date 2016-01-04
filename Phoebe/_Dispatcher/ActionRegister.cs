using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Toggl.Phoebe.Logging;
using Toggl.Phoebe.Net;
using XPlatUtils;

namespace Toggl.Phoebe
{
    public static class ActionRegister
    {
        public static Func<DataMsgUntyped, Task<DataMsgUntyped>> GetCallback (DataTag tag)
        {
            switch (tag) {
            case DataTag.RunTimeEntriesUpdate:
                return RunTimeEntriesUpdate;

            case DataTag.LoadMoreTimeEntries:
            case DataTag.StopTimeEntry:
            case DataTag.RemoveTimeEntryWithUndo:
            case DataTag.RestoreTimeEntryFromUndo:
            case DataTag.RemoveTimeEntryPermanently:
                return LetGoThrough;

            default:
                return null;
            }
        }

        static async Task<DataMsgUntyped> RunTimeEntriesUpdate (DataMsgUntyped msg)
        {
            var msgData = msg.Data.Match (x => x as Tuple<DateTime, int>, e => { throw new Exception (e); });

            bool hadErrors = false;
            bool hasMore = true;
            var endDate = DateTime.MinValue;
            var startFrom = msgData.Item1;
            var daysLoad = msgData.Item2;

            // Try to update with latest data from server
            try {
//                bus.Send (new UpdateStartedMessage (this, startFrom));

                // Download new Entries
                var client = ServiceContainer.Resolve<ITogglClient> ();
                var jsonEntries = await client.ListTimeEntries (startFrom, daysLoad);

                // TODO: Send this info to the view
//                endDate = entries.Min (p => p.StartTime);
//                hasMore = entries.Any ();

                // Store them in the local data store
                return DataMsgUntyped.Success (msg.Tag, jsonEntries);

            } catch (Exception exc) {
                hadErrors = true;
                var tag = "ActionRegister";
                var log = ServiceContainer.Resolve<ILogger> ();
                const string errorMsg = "Failed to fetch time entries {1} days up to {0}";
                if (exc.IsNetworkFailure () || exc is TaskCanceledException) {
                    log.Info (tag, exc, errorMsg, startFrom, daysLoad);
                } else {
                    log.Warning (tag, exc, errorMsg, startFrom, daysLoad);
                }

                return DataMsgUntyped.Error (msg.Tag, errorMsg + ": " + exc.Message);
            }
//            } finally {
//                bus.Send (new UpdateFinishedMessage (this, startFrom, endDate, hasMore, hadErrors));
//            }
        }

        static Task<DataMsgUntyped> LetGoThrough (DataMsgUntyped msg)
        {
            return Task.Run (() => msg);
        }
    }
}
