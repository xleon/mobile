using System;
using System.Linq;
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
            case DataTag.LoadMoreTimeEntries:
                return LoadMoreTimeEntries;

            case DataTag.RunTimeEntriesUpdate:
                return RunTimeEntriesUpdate;

            case DataTag.StopTimeEntry:
            case DataTag.RemoveTimeEntryWithUndo:
            case DataTag.RestoreTimeEntryFromUndo:
            case DataTag.RemoveTimeEntryPermanently:
                return LetGoThrough;

            default:
                throw new ActionNotFoundException (tag, typeof (DispatcherRegister));
            }
        }

        static Task<IDataMsg> LoadMoreTimeEntries (IDataMsg msg)
        {
            // Try to update with latest data from server
            Dispatcher.Send (DataTag.RunTimeEntriesUpdate, msg.ForceGetData<UpdateStartedMessage> ());

            // And then pass the message to the store to load first data locally
            return LetGoThrough (msg);
        }

        static async Task<IDataMsg> RunTimeEntriesUpdate (IDataMsg msg)
        {
            var msgData = msg.ForceGetData<UpdateStartedMessage> ();
            var startFrom = msgData.StartDate;
            var daysLoad = msgData.DaysLoad;

            bool hadErrors = false;
            bool hasMore = true;
            var endDate = DateTime.MinValue;
            var jsonEntries = new List<TimeEntryJson> ();

            try {
                // Download new Entries
                var client = ServiceContainer.Resolve<ITogglClient> ();
                jsonEntries = await client.ListTimeEntries (startFrom, daysLoad);

                endDate = jsonEntries.Min (p => p.StartTime);
                hasMore = jsonEntries.Any ();
            } catch (Exception exc) {
                hadErrors = true;
                var tag = typeof (DispatcherRegister).Name;
                var log = ServiceContainer.Resolve<ILogger> ();
                const string errorMsg = "Failed to fetch time entries {1} days up to {0}";
                if (exc.IsNetworkFailure () || exc is TaskCanceledException) {
                    log.Info (tag, exc, errorMsg, startFrom, daysLoad);
                } else {
                    log.Warning (tag, exc, errorMsg, startFrom, daysLoad);
                }
            }

            // TODO: Use an Either.Right to pass the error instead
            return DataMsg.Success (
               msg.Tag, new UpdateFinishedMessage (
                   jsonEntries, startFrom, endDate, hasMore, hadErrors));
        }

        static Task<IDataMsg> LetGoThrough (IDataMsg msg)
        {
            return Task.Run (() => msg);
        }
    }
}
