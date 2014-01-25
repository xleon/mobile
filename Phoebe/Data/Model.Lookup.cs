using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using XPlatUtils;

namespace Toggl.Phoebe.Data
{
    public partial class Model
    {
        public static ModelManager Manager {
            get { return ServiceContainer.Resolve<ModelManager> (); }
        }

        public static T Update<T> (T model)
            where T : Model
        {
            return Manager.Update<T> (model);
        }

        public static T ById<T> (Guid id)
            where T : Model
        {
            return Manager.Get<T> (id);
        }

        public static T ByRemoteId<T> (long remoteId)
            where T : Model
        {
            return Manager.GetByRemoteId<T> (remoteId);
        }

        public static IModelQuery<T> Query<T> (Expression<Func<T, bool>> predicate = null)
            where T : Model, new()
        {
            return Manager.Query (predicate);
        }
    }
}