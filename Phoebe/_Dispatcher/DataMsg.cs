using System;
using System.Threading.Tasks;
using Toggl.Phoebe.Helpers;
using Toggl.Phoebe.Logging;
using XPlatUtils;

namespace Toggl.Phoebe
{
    public static class DataMsg
    {
        public const string UNCAUGHT_ERROR = "UNCAUGHT_ERROR";
        public const string LOAD_MORE_ENTRIES = "LOAD_MORE_ENTRIES";
    }

    public class DataMsgUntyped
    {
        public string Tag { get; private set; }
        public Either<object, string> Data { get; private set; }

        DataMsgUntyped () { }

        public static DataMsgUntyped Success (string tag, object data = null)
        {
            return new DataMsgUntyped {
                Tag = tag,
                Data = Either<object, string>.Left (data)
            };
        }

        public static DataMsgUntyped Error (string tag, string error)
        {
            return new DataMsgUntyped {
                Tag = tag,
                Data = Either<object, string>.Right (error)
            };
        }
    }
}

