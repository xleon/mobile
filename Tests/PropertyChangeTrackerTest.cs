using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using NUnit.Framework;
using Toggl.Phoebe.Data.Utils;

namespace Toggl.Phoebe.Tests
{
    public class PropertyChangeTrackerTest : Test
    {
        [Test]
        public void TestNotify()
        {
            TestTracker (null, true);
        }

        [Test]
        public void TestStale()
        {
            TestTracker (((PropertyChangeTracker tracker) => {
                tracker.MarkAllStale();
                tracker.ClearStale();
            }), false);
        }

        [Test]
        public void TestClear()
        {
            TestTracker (((PropertyChangeTracker tracker) => {
                tracker.ClearAll();
            }), false);
        }

        public void TestTracker (Action<PropertyChangeTracker> injection, bool shouldNotify)
        {
            var tracker = new PropertyChangeTracker ();
            var demoObject = DemoObject.Bake ();

            string detectedChangedTargetName = null;

            tracker.Add (demoObject, ((string obj) => {
                detectedChangedTargetName = obj;
            }));

            if (injection != null) {
                injection (tracker);
            }

            string changedTargetName = demoObject.Change ();

            if (shouldNotify) {
                Assert.That (changedTargetName, Is.EqualTo (detectedChangedTargetName));
            } else {
                Assert.That (changedTargetName, Is.Not.EqualTo (detectedChangedTargetName));
            }
        }

        public class DemoObject : INotifyPropertyChanged
        {
            private int obj = 0;

            public event PropertyChangedEventHandler PropertyChanged;

            private int GetRandomVal()
            {
                return new Random (Guid.NewGuid ().GetHashCode ()).Next (10);
            }

            public string Change()
            {
                Obj = GetRandomVal ();
                return "Obj";
            }

            private void Notify ([CallerMemberName] String propertyName = "")
            {
                if (PropertyChanged != null) {
                    PropertyChanged (this, new PropertyChangedEventArgs (propertyName));
                }
            }

            private DemoObject()
            {
                Change();
            }

            public static DemoObject Bake()
            {
                return new DemoObject ();
            }

            public int Obj
            {
                get { return this.obj; }
                set {
                    if (this.obj != value) {
                        this.obj = value;
                        Notify ();
                    }
                }
            }
        }
    }
}

