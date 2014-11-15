using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using NUnit.Framework;
using Bugsnag;
using Toggl.Phoebe.Data;
using Toggl.Phoebe.Net;
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
            ServiceContainer.Register<TimeCorrectionManager> ();
            ServiceContainer.Register<IDataStore> (delegate {
                databasePath = Path.GetTempFileName ();
                return new SqliteDataStore (databasePath);
            });
            ServiceContainer.Register<LogStore> ((LogStore)null);
            ServiceContainer.Register<IBugsnagClient> ((IBugsnagClient)null);
            ServiceContainer.Register<Logger> ();
        }

        [TearDown]
        public virtual void TearDown ()
        {
            RunAsync (async delegate {
                // Use an empty transaction to ensure that the SQLiteDataStore has completed all scheduled jobs:
                await DataStore.ExecuteInTransactionAsync ((ctx) => {
                });
            });

            // Make sure all of the scheduled actions have been completed
            while (syncContext.Run ()) {
            }

            ServiceContainer.Clear ();

            if (databasePath != null) {
                File.Delete (databasePath);
                databasePath = null;
            }
        }

        protected async Task SetUpFakeUser (Guid userId)
        {
            ServiceContainer.Register<ISettingsStore> (Mock.Of<ISettingsStore> (
                        (store) => store.ApiToken == "test" &&
                        store.UserId == userId));
            var authManager = new AuthManager ();
            ServiceContainer.Register<AuthManager> (authManager);

            // Wait for the auth manager to load user data:
            var tcs = new TaskCompletionSource<object> ();
            Action checkUser = delegate {
                if (authManager.User != null && authManager.User.DefaultWorkspaceId != Guid.Empty) {
                    tcs.TrySetResult (null);
                }
            };
            authManager.PropertyChanged += (sender, e) => {
                if (e.PropertyName == AuthManager.PropertyUser) {
                    checkUser ();
                }
            };

            checkUser ();
            await tcs.Task;

            MessageBus.Send (new AuthChangedMessage (authManager, AuthChangeReason.Login));
        }

        protected void RunAsync (Func<Task> fn)
        {
            var awaiter = fn ().GetAwaiter ();

            // Process jobs and wait for the task to complete
            while (syncContext.Run () || !awaiter.IsCompleted) {
            }

            // Propagate exceptions
            awaiter.GetResult ();
        }

        protected MessageBus MessageBus
        {
            get { return ServiceContainer.Resolve<MessageBus> (); }
        }

        protected IDataStore DataStore
        {
            get { return ServiceContainer.Resolve<IDataStore> (); }
        }
    }
}
