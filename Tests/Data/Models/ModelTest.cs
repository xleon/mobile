using System;
using System.Collections.Generic;
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

        [Test]
        public void VerifyConstructors ()
        {
            var type = typeof(T);

            Assert.IsNotNull (type.GetConstructor (new Type[] { }), "Default constructor not found.");
            Assert.IsNotNull (type.GetConstructor (new[] { typeof(Guid) }), "Lazy load constructor not found.");

            var dataProp = type.GetProperty ("Data");
            Assert.IsNotNull (type.GetConstructor (new[] { dataProp.PropertyType }), "Wrapping constructor not found.");
        }

        [Test]
        public void PropertyChangeEvents ()
        {
            var type = typeof(T);

            var properties = type.GetProperties (BindingFlags.Instance | BindingFlags.Public)
                .Where (prop => prop.CanWrite)
                .Where (prop => !ExemptProperties.Contains (prop.Name));

            foreach (var prop in properties) {
                var changed = new List<string> ();
                var inst = Activator.CreateInstance<T> ();
                inst.PropertyChanged += (s, e) => changed.Add (e.PropertyName);

                // Simulate property change
                var val = prop.GetValue (inst);

                if (prop.PropertyType == typeof(string)) {
                    val = ((string)val ?? String.Empty) + "Test";
                } else if (prop.PropertyType == typeof(int)) {
                    val = (int)val + 1;
                } else if (prop.PropertyType == typeof(int?)) {
                    val = ((int?)val ?? 0) + 1;
                } else if (prop.PropertyType == typeof(long)) {
                    val = (long)val + 1;
                } else if (prop.PropertyType == typeof(long?)) {
                    val = ((long?)val ?? 0) + 1;
                } else if (prop.PropertyType == typeof(decimal)) {
                    val = (decimal)val + 1;
                } else if (prop.PropertyType == typeof(decimal?)) {
                    val = ((decimal?)val ?? 0) + 1;
                } else if (prop.PropertyType == typeof(bool)) {
                    val = !(bool)val;
                } else if (prop.PropertyType == typeof(DateTime)) {
                    if ((DateTime)val == DateTime.MinValue) {
                        val = Time.UtcNow;
                    } else {
                        val = (DateTime)val + TimeSpan.FromMinutes (1);
                    }
                } else if (prop.PropertyType == typeof(DateTime?)) {
                    if (val == null || (DateTime?)val == DateTime.MinValue) {
                        val = (DateTime?)Time.UtcNow;
                    } else {
                        val = ((DateTime?)val).Value + TimeSpan.FromMinutes (1);
                    }
                } else if (prop.PropertyType.IsEnum) {
                    val = (int)val + 1;
                } else if (prop.PropertyType.GetInterfaces ().Contains (typeof(IModel))) {
                    val = Activator.CreateInstance (prop.PropertyType, Guid.NewGuid ());
                } else {
                    throw new InvalidOperationException (String.Format ("Don't know how to handle testing of {0} type.", prop.PropertyType));
                }

                prop.SetValue (inst, val);

                // Verify that the property changed event was fired:
                Assert.That (changed, Has.Some.Matches<string> (name => name == prop.Name),
                    String.Format ("{0} did not raise a PropertyChanged event.", prop.Name));
            }
        }
    }
}
