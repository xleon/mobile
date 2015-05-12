using System;
using System.Collections.Generic;

namespace Toggl.Phoebe.Data.Extensions
{
    public static class ListExtensions
    {
        public static List<Guid> TransformToGuids (this IList<string> list)
        {
            var ids = new List<Guid> ();
            if (list != null && list.Count > 0) {
                foreach (var guidEntity in list) {
                    var guidParsing = Guid.Empty;
                    Guid.TryParse (guidEntity, out guidParsing);
                    if (guidParsing != Guid.Empty) {
                        ids.Add (guidParsing);
                    }
                }
            }
            return ids;
        }
    }
}

