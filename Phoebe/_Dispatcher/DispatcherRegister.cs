using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Toggl.Phoebe.Logging;
using Toggl.Phoebe.Net;
using XPlatUtils;
using Toggl.Phoebe.Data.Json;

namespace Toggl.Phoebe
{
    public static class DispatcherRegister
    {
        public static Func<IDataMsg, Task<IDataMsg>> GetAction (DataTag tag)
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
                throw new ActionNotFoundException (tag, typeof (DispatcherRegister));
            }
        }

        static async Task<IDataMsg> RunTimeEntriesUpdate (IDataMsg msg)
        {
            throw new NotImplementedException ();
//            var msgData = msg.Data.Match (x => x as Tuple<DateTime, int>, e => { throw new Exception (e); });
//
//            bool hadErrors = false;
//            bool hasMore = true;
//            var endDate = DateTime.MinValue;
//            var startFrom = msgData.Item1;
//            var daysLoad = msgData.Item2;
//
//            // Try to update with latest data from server
//            try {
////                bus.Send (new UpdateStartedMessage (this, startFrom));
//
//                // Download new Entries
//                var client = ServiceContainer.Resolve<ITogglClient> ();
//                var jsonEntries = await client.ListTimeEntries (startFrom, daysLoad);
//
//                // TODO: Send this info to the view
////                endDate = entries.Min (p => p.StartTime);
////                hasMore = entries.Any ();
//
//                // Store them in the local data store
//                return DataMsg<List<TimeEntryJson>>.Success (msg.Tag, jsonEntries);
//
//            } catch (Exception exc) {
//                hadErrors = true;
//                var tag = "ActionRegister";
//                var log = ServiceContainer.Resolve<ILogger> ();
//                const string errorMsg = "Failed to fetch time entries {1} days up to {0}";
//                if (exc.IsNetworkFailure () || exc is TaskCanceledException) {
//                    log.Info (tag, exc, errorMsg, startFrom, daysLoad);
//                } else {
//                    log.Warning (tag, exc, errorMsg, startFrom, daysLoad);
//                }
//
//                return DataMsg.Error (msg.Tag, exc);
//            }
//            } finally {
//                bus.Send (new UpdateFinishedMessage (this, startFrom, endDate, hasMore, hadErrors));
//            }
        }

        static Task<IDataMsg> LetGoThrough (IDataMsg msg)
        {
            return Task.Run (() => msg);
        }
    }
}
