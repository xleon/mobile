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
        private MainThreadSynchronizationContext syncContext;

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
            syncContext = new MainThreadSynchronizationContext ();
            SynchronizationContext.SetSynchronizationContext (syncContext);

            // Create MessageBus egerly to avoid it being created in the background thread with invalid synchronization context.
            ServiceContainer.Register<MessageBus> (new MessageBus ());
            ServiceContainer.Register<ITimeProvider> (() => new DefaultTimeProvider ());
            ServiceContainer.Register<IDataStore> (delegate {
                databasePath = Path.GetTempFileName ();
                return new SqliteDataStore (databasePath);
            });
        }

        [TearDown]
        public virtual void TearDown ()
        {
            // Work through queued jobs before we start tearing everything down
            while (syncContext.Run ()) {
            }

            RunAsync (async delegate {
                // Use an empty transaction to ensure that the SQLiteDataStore has completed all scheduled jobs:
                await DataStore.ExecuteInTransactionAsync ((ctx) => {
                });

                ServiceContainer.Clear ();

                if (databasePath != null) {
                    File.Delete (databasePath);
                    databasePath = null;
                }
            });
        }

        protected void RunAsync (Func<Task> fn)
        {
            var awaiter = fn ().GetAwaiter ();

            // Process jobs and wait for the task to complete
            while (syncContext.Run () || !awaiter.IsCompleted) {
            }
        }

        protected MessageBus MessageBus {
            get { return ServiceContainer.Resolve<MessageBus> (); }
        }

        protected IDataStore DataStore {
            get { return ServiceContainer.Resolve<IDataStore> (); }
        }
    }
}
