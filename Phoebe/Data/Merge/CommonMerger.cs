using System;
using System.Collections.Generic;
using Toggl.Phoebe.Data.DataObjects;

namespace Toggl.Phoebe.Data.Merge
{
    public abstract class CommonMerger<T>
        where T : CommonData
    {
        private readonly T baseData;
        private readonly List<T> changes = new List<T> ();
        private T resultData;

        protected CommonMerger (T baseData)
        {
            this.baseData = baseData;
            baseData.ModifiedAt = baseData.ModifiedAt.ToUtc ();
        }

        protected virtual T Merge ()
        {
            var data = (T)Activator.CreateInstance (typeof (T), baseData);
            var latestData = changes [0];

            // Merge common data
            data.RemoteId = GetValue (d => d.RemoteId);
            data.ModifiedAt = latestData.ModifiedAt;
            data.DeletedAt = GetValue (d => d.DeletedAt);
            data.IsDirty = latestData.IsDirty;
            data.RemoteRejected = false;

            return data;
        }

        protected T GetData<U> (Func<T, U> fieldSelector)
        {
            var baseField = fieldSelector (baseData);
            foreach (var change in changes) {
                if (!EqualityComparer<U>.Default.Equals (baseField, fieldSelector (change))) {
                    return change;
                }
            }

            return baseData;
        }

        protected U GetValue<U> (Func<T, U> fieldSelector)
        {
            return fieldSelector (GetData (fieldSelector));
        }

        public void Add (T change)
        {
            change.ModifiedAt = change.ModifiedAt.ToUtc ();
            changes.Add (change);
            resultData = null;
        }

        protected T Base
        {
            get { return baseData; }
        }

        public T Result
        {
            get {
                if (resultData == null) {
                    if (changes.Count == 0) {
                        resultData = baseData;
                    } else if (changes.Count == 1) {
                        resultData = changes [0];
                    } else {
                        changes.Sort ((a, b) => b.ModifiedAt.CompareTo (a.ModifiedAt));
                        resultData = Merge ();
                    }
                }

                return resultData;
            }
        }
    }
}
