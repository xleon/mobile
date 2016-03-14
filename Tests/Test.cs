using System.IO;
using System.Threading;
using NUnit.Framework;
using SQLite.Net.Platform.Generic;
using Toggl.Phoebe.Logging;
using Toggl.Phoebe.Net;
using XPlatUtils;

namespace Toggl.Phoebe.Tests
{
    public abstract class Test
    {
        protected string databasePath;
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
            ServiceContainer.RegisterScoped<_Data.ISyncDataStore> (
                new _Data.SyncSqliteDataStore (databasePath, new SQLitePlatformGeneric ()));
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
        }

        [TearDown]
        public virtual void TearDown ()
        {
        }

        protected MessageBus MessageBus
        {
            get { return ServiceContainer.Resolve<MessageBus> (); }
        }
    }
}
