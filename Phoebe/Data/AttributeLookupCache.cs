using System;
using System.Collections.Generic;

namespace Toggl.Phoebe.Data
{
    public class AttributeLookupCache<T>
        where T : Attribute
    {
        private readonly Dictionary<Type, Dictionary<string, bool>> registry =
            new Dictionary<Type, Dictionary<string, bool>> ();

        public bool HasAttribute (object obj, string property)
        {
            var type = obj.GetType ();
            return HasAttribute (type, property);
        }

        public bool HasAttribute (Type type, string property)
        {
            lock (registry) {
                Dictionary<string, bool> typeRegistry;
                if (!registry.TryGetValue (type, out typeRegistry)) {
                    registry [type] = typeRegistry = new Dictionary<string, bool> ();
                }

                bool hasAttr;
                if (!typeRegistry.TryGetValue (property, out hasAttr)) {
                    var propInfo = type.GetProperty (property);
                    hasAttr = propInfo.GetCustomAttributes (typeof(T), true).Length > 0;
                }
                return hasAttr;
            }
        }
    }
}
