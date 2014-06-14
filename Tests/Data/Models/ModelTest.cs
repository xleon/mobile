using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using Toggl.Phoebe.Data.NewModels;

namespace Toggl.Phoebe.Tests.Data.Models
{
    public abstract class ModelTest<T> : Test
        where T : IModel
    {
        protected static readonly string[] ExemptProperties = { "Data" };

        [Test]
        public void VerifyPropertyNames ()
        {
            const string fieldPrefix = "Property";
            var type = typeof(T);
            var fields = type.GetFields (BindingFlags.Static | BindingFlags.Public)
                .Where (t => t.Name.StartsWith (fieldPrefix, StringComparison.Ordinal))
                .ToList ();

            Assert.That (fields, Has.Count.AtLeast (1), String.Format ("{0} should define some property names.", type.Name));

            // Verify that defined property names have correct value and properties exist
            foreach (var field in fields) {
                var name = field.Name.Substring (fieldPrefix.Length);
                var value = field.GetValue (null);
                Assert.AreEqual (name, value, String.Format ("{0}.{1} property value is {2}.", type.Name, field.Name, value));

                var prop = type.GetProperty (name, BindingFlags.Public | BindingFlags.Instance);
                Assert.NotNull (prop, String.Format ("{0}.{1} property doesn't exist.", type.Name, name));
            }

            // Verify that all public properties have the property name defined
            var fieldNames = fields.Select (f => f.GetValue (null)).ToList ();
            var properties = type.GetProperties (BindingFlags.Instance | BindingFlags.Public);
            foreach (var prop in properties) {
                if (!ExemptProperties.Contains (prop.Name)) {
                    Assert.IsTrue (fieldNames.Contains (prop.Name), String.Format ("{0}.{1}{2} static field is not defined.", type.Name, fieldPrefix, prop.Name));
                }
            }
        }
    }
}
