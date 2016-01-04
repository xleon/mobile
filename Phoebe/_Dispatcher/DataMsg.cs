using System;
using System.Threading.Tasks;
using Toggl.Phoebe.Helpers;
using Toggl.Phoebe.Logging;
using XPlatUtils;

namespace Toggl.Phoebe
{
    public enum DataTag {
        UncaughtError,
        LoadMoreTimeEntries,
        RunTimeEntriesUpdate,
        StopTimeEntry,
        RemoveTimeEntryPermanently,
        RemoveTimeEntryWithUndo,
        RestoreTimeEntryFromUndo,
    }

    public class DataMsgUntyped
    {
        public DataTag Tag { get; private set; }
        public Either<object, string> Data { get; private set; }

        DataMsgUntyped () { }

        public static DataMsgUntyped Success (DataTag tag, object data = null)
        {
            return new DataMsgUntyped {
                Tag = tag,
                Data = Either<object, string>.Left (data)
            };
        }

        public static DataMsgUntyped Error (DataTag tag, string error)
        {
            return new DataMsgUntyped {
                Tag = tag,
                Data = Either<object, string>.Right (error)
            };
        }
    }
}

