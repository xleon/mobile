using System;
using System.Linq;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;

namespace Toggl.Phoebe.Bugsnag.Json
{
    public static class JObjectExtensions
    {
        public static void Merge (this JObject dest, JObject source)
        {
            foreach (var kvp in source) {
                var prop = kvp.Key;
                var srcValue = kvp.Value;
                if (srcValue == null)
                    continue;

                var dstObject = dest [prop] as JObject;
                if (dstObject != null && srcValue is JObject) {
                    Merge (dstObject, (JObject)srcValue);
                } else {
                    dest [prop] = srcValue;
                }
            }
        }

        public static void Filter (this JObject obj, List<string> filters)
        {
            if (obj == null)
                return;

            foreach (var kvp in obj) {
                var prop = kvp.Key;
                var isFiltered = false;

                if (prop != null) {
                    isFiltered = filters.Any ((filter) => prop.Contains (filter));
                }

                if (isFiltered) {
                    obj [prop] = "[FILTERED]";
                } else {
                    var val = kvp.Value as JObject;
                    if (val != null) {
                        val.Filter (filters);
                    }
                }
            }
        }
    }
}
