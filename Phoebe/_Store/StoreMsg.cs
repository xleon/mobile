using System;
using Toggl.Phoebe.Data;
using System.Collections.Generic;
using Toggl.Phoebe.Helpers;
using System.Linq;

namespace Toggl.Phoebe
{
    public struct StoreMsgUntyped {
        public readonly DataAction Action;
        public readonly object Data;

        public StoreMsgUntyped (DataAction tag, object data)
        {
            this.Action = tag;
            this.Data = data;
        }
    }

    public struct StoreMsg<T> {
        public readonly DataAction Action;
        public readonly T Data;

        public StoreMsg (StoreMsgUntyped msg)
        {
            this.Action = msg.Action;
            this.Data = (T)msg.Data;
        }
    }

    public class StoreResultUntyped
    {
        public string Tag { get; private set; }
        public Either<IEnumerable<StoreMsgUntyped>, string> Data { get; private set; }

        StoreResultUntyped() { }

        public static StoreResultUntyped Success (string tag, IEnumerable<StoreMsgUntyped> data = null)
        {
            return new StoreResultUntyped {
                Tag = tag,
                Data = Either<IEnumerable<StoreMsgUntyped>, string>.Left (data)
            };
        }

        public static StoreResultUntyped Error (string tag, string error)
        {
            return new StoreResultUntyped {
                Tag = tag,
                Data = Either<IEnumerable<StoreMsgUntyped>, string>.Right (error)
            };
        }
    }

    public class StoreResult<T>
    {
        public readonly Either<IEnumerable<StoreMsg<T>>, string> Data;

        public StoreResult (StoreResultUntyped res)
        {
            this.Data = res.Data.Select (x => x.Select (y => new StoreMsg<T> (y)), x => x);
        }

        public U Match<U> (Func<IEnumerable<StoreMsg<T>>,U> success, Func<string,U> error)
        {
            return this.Data.Match (success, error);
        }
    }
}

