using System;
using Toggl.Phoebe.Data.DataObjects;

namespace Toggl.Phoebe.Data
{
    public static class DataExtensions
    {
        /// <summary>
        /// Checks if the two objects share the same type and primary key.
        /// </summary>
        /// <param name="data">Data object.</param>
        /// <param name="other">Other data object.</param>
        public static bool Matches (this CommonData data, object other)
        {
            if (data == other)
                return true;
            if (data == null || other == null)
                return false;
            if (data.GetType () != other.GetType ())
                return false;
            return data.Id == ((CommonData)data).Id;
        }
    }
}
