using System;
using System.Linq.Expressions;
using Newtonsoft.Json;
using XPlatUtils;

namespace Toggl.Phoebe.Data
{
    public abstract class IntermediateModel<TFrom, TTo> : Model
        where TFrom : Model
        where TTo : Model
    {
        private static string GetPropertyName<T> (Expression<Func<IntermediateModel<TFrom, TTo>, T>> expr)
        {
            return expr.ToPropertyName ();
        }

        private readonly int fromRelationId;
        private readonly int toRelationId;

        public IntermediateModel ()
        {
            fromRelationId = ForeignRelation<TFrom> (PropertyFromId, PropertyFrom);
            toRelationId = ForeignRelation<TTo> (PropertyToId, PropertyTo);
        }

        #region Relations

        public static readonly string PropertyFromId = GetPropertyName ((m) => m.FromId);

        public Guid? FromId {
            get { return GetForeignId (fromRelationId); }
            set { SetForeignId (fromRelationId, value); }
        }

        public static readonly string PropertyFrom = GetPropertyName ((m) => m.From);

        [SQLite.Ignore]
        [JsonConverter (typeof(ForeignKeyJsonConverter))]
        public virtual TFrom From {
            get { return GetForeignModel<TFrom> (fromRelationId); }
            set { SetForeignModel (fromRelationId, value); }
        }

        public static readonly string PropertyToId = GetPropertyName ((m) => m.ToId);

        public Guid? ToId {
            get { return GetForeignId (toRelationId); }
            set { SetForeignId (toRelationId, value); }
        }

        public static readonly string PropertyTo = GetPropertyName ((m) => m.To);

        [SQLite.Ignore]
        [JsonConverter (typeof(ForeignKeyJsonConverter))]
        public virtual TTo To {
            get { return GetForeignModel<TTo> (toRelationId); }
            set { SetForeignModel (toRelationId, value); }
        }

        #endregion

    }
}
