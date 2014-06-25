using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Toggl.Phoebe.Data;
using XPlatUtils;

namespace Toggl.Phoebe.Tests
{
    public abstract class Test
    {
        private string databasePath;

        [TestFixtureSetUp]
        public virtual void Init ()
        {
        }

        [TestFixtureTearDown]
        public virtual void Cleanup ()
        {
        }

        [SetUp]
        public virtual void SetUp ()
        {
            SynchronizationContext.SetSynchronizationContext (new SynchronizationContext ());

            ServiceContainer.Register<MessageBus> ();
            ServiceContainer.Register<ITimeProvider> (() => new DefaultTimeProvider ());
            ServiceContainer.Register<IDataStore> (delegate {
                databasePath = Path.GetTempFileName ();
                return new SQLiteDataStore (databasePath);
            });
        }

        [TearDown]
        public virtual void TearDown ()
        {
            ServiceContainer.Clear ();

            if (databasePath != null) {
                File.Delete (databasePath);
                databasePath = null;
            }
        }

        protected void RunAsync (Func<Task> fn)
        {
            fn ().GetAwaiter ().GetResult ();
        }

        protected MessageBus MessageBus {
            get { return ServiceContainer.Resolve<MessageBus> (); }
        }

        protected IDataStore DataStore {
            get { return ServiceContainer.Resolve<IDataStore> (); }
        }
    }
}
