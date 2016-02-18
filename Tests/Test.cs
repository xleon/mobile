using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using NUnit.Framework;
using SQLite.Net.Platform.Generic;
using Toggl.Phoebe.Data;
using Toggl.Phoebe.Logging;
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
            syncContext = new MainThreadSynchronizationContext ();
            SynchronizationContext.SetSynchronizationContext (syncContext);

            databasePath = Path.GetTempFileName ();
            ServiceContainer.RegisterScoped<MessageBus> (new MessageBus ());
            ServiceContainer.RegisterScoped<ITimeProvider> (new DefaultTimeProvider ());
            ServiceContainer.Register<TimeCorrectionManager> (new TimeCorrectionManager ());
            ServiceContainer.RegisterScoped<Toggl.Phoebe._Data.ISyncDataStore> (
                new Toggl.Phoebe._Data.SyncSqliteDataStore (databasePath, new SQLitePlatformGeneric ()));
            ServiceContainer.RegisterScoped<LogStore> ((LogStore)null);
            ServiceContainer.RegisterScoped<ILoggerClient> ((ILoggerClient)null);
            ServiceContainer.RegisterScoped<ILogger> (new VoidLogger());
            ServiceContainer.RegisterScoped<INetworkPresence> (new Reactive.NetWorkPresenceMock ());
        }

        [TestFixtureTearDown]
        public virtual void Cleanup ()
        {
            ServiceContainer.Clear ();
            if (databasePath != null) {
                File.Delete (databasePath);
                databasePath = null;
            }
        }

        [SetUp]
        public virtual void SetUp ()
        {
            syncContext = new MainThreadSynchronizationContext ();
            SynchronizationContext.SetSynchronizationContext (syncContext);
        }

        [TearDown]
        public virtual void TearDown ()
        {
//            RunAsync (async delegate {
//                // Use an empty transaction to ensure that the SQLiteDataStore has completed all scheduled jobs:
//                await DataStore.ExecuteInTransactionAsync ((ctx) => {
//                });
//            });

            // Make sure all of the scheduled actions have been completed
            while (syncContext.Run ()) {
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

//        protected IDataStore DataStore
//        {
//            get { return ServiceContainer.Resolve<IDataStore> (); }
//        }
    }
}
