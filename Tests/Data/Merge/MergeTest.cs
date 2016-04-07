using System.Collections;
using System.Reflection;
using NUnit.Framework;

namespace Toggl.Phoebe.Tests.Data.Merge
{
    public abstract class MergeTest : Test
    {
        protected static void AssertPropertiesEqual<T> (T expected, T actual)
        {
            var properties = expected.GetType().GetProperties();
            foreach (var prop in properties)
            {
                object expectedValue = prop.GetValue(expected, null);
                object actualValue = prop.GetValue(actual, null);

                if (actualValue is IList)
                {
                    AssertListsEquals(prop, (IList)actualValue, (IList)expectedValue);
                }
                else if (!Equals(expectedValue, actualValue))
                {
                    Assert.Fail("Property {0}.{1} does not match. Expected: {2} but was: {3}", prop.DeclaringType.Name, prop.Name, expectedValue, actualValue);
                }
            }
        }

        private static void AssertListsEquals(PropertyInfo property, IList actualList, IList expectedList)
        {
            if (actualList.Count != expectedList.Count)
            {
                Assert.Fail("Property {0}.{1} does not match. Expected IList containing {2} elements but was IList containing {3} elements", property.PropertyType.Name, property.Name, expectedList.Count, actualList.Count);
            }

            for (int i = 0; i < actualList.Count; i++)
            {
                if (!Equals(actualList [i], expectedList [i]))
                {
                    Assert.Fail("Property {0}.{1} does not match. Expected IList with element {1} equals to {2} but was IList with element {1} equals to {3}", property.PropertyType.Name, property.Name, expectedList [i], actualList [i]);
                }
            }
        }
    }
}
