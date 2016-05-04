using System;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using Toggl.Phoebe.Tests;
using Toggl.Phoebe.Data;
using Toggl.Phoebe.Data.Json;
using Toggl.Phoebe.Data.Models;
using Toggl.Phoebe.Net;
using Toggl.Phoebe.Reactive;
using Toggl.Phoebe.Tests.Reactive;
using XPlatUtils;
using System.IO;
using SQLite.Net.Platform.Generic;
using Toggl.Phoebe.Logging;

namespace Toggl.Phoebe.Tests.Staging
{
    [TestFixture]
    public class SyncOutManagerTest2
    {
        protected string databasePath;
        protected readonly string databaseDir = Path.GetDirectoryName(Path.GetTempFileName());

        IUserData userData;
        TogglRestClient togglClient;
        NetworkSwitcher networkSwitcher;

        [OneTimeSetUp]
        public async Task Init()
        {
            databasePath = DatabaseHelper.GetDatabasePath(databaseDir, SyncSqliteDataStore.DB_VERSION);
            ServiceContainer.Register<ISyncDataStore>(
                new SyncSqliteDataStore(databasePath, new SQLitePlatformGeneric()));

            ServiceContainer.Register<MessageBus>(new MessageBus());
            ServiceContainer.Register<ITimeProvider>(new DefaultTimeProvider());
            ServiceContainer.Register<TimeCorrectionManager>(new TimeCorrectionManager());
            ServiceContainer.Register<LogStore>((LogStore)null);
            ServiceContainer.Register<ILoggerClient>((ILoggerClient)null);
            ServiceContainer.Register<ILogger>(new VoidLogger());
            ServiceContainer.Register<INetworkPresence>(new Reactive.NetWorkPresenceMock());


            var platformUtils = new PlatformUtils()
            {
                AppIdentifier = "TogglPhoebe",
                AppVersion = "0.1"
            };
            ServiceContainer.RegisterScoped<IPlatformUtils> (platformUtils);

            this.networkSwitcher = new NetworkSwitcher();
            ServiceContainer.RegisterScoped<INetworkPresence>(networkSwitcher);

            this.togglClient = new TogglRestClient(Build.ApiUrl);
            ServiceContainer.RegisterScoped<ITogglClient> (togglClient);
            RxChain.Init(Util.GetInitAppState());

            IDisposable subscription = null;
            var tsc = new TaskCompletionSource<bool>();
            subscription =
                StoreManager
                .Singleton
                .Observe()
                .Subscribe(x =>
            {
                if (x.State.User.ApiToken != null)
                {
                    this.userData = x.State.User;
                    subscription?.Dispose();
                    tsc.SetResult(true);
                }
            });

            var email = string.Format("mobile.{0}@toggl.com", Util.UserId);
            var password = "123456";
            RxChain.Send(ServerRequest.Authenticate.Signup(email, password));
            await tsc.Task;
        }

        public async Task Cleanup()
        {
            if (this.userData != null)
            {
                try
                {
                    await togglClient.DeleteUser(this.userData.ApiToken);
                }
                catch (Exception ex)
                {
                    throw ex;
                }
            }
            RxChain.Cleanup();

            ServiceContainer.Clear();
            if (databasePath != null)
            {
                File.Delete(databasePath);
                databasePath = null;
            }
        }

        [Test]
        public async Task TestSendMessageWithoutConnection()
        {
            var tcs = Util.CreateTask<bool> ();
            var te = Util.CreateTimeEntryData(DateTime.Now, userData.RemoteId.Value, userData.DefaultWorkspaceRemoteId);

            networkSwitcher.SetNetworkConnection(false);
            RxChain.Send(new DataMsg.TimeEntryPut(te), new RxChain.Continuation((_, sent, queued) =>
            {
                try
                {
                    // As there's no connection, message should have been enqueued
                    Assert.That(queued.Any(x => x.Data.Id == te.Id), Is.True);
                    Assert.That(0, Is.EqualTo(sent.Count()));
                    tcs.SetResult(true);
                }
                catch (Exception ex)
                {
                    tcs.SetException(ex);
                }
            }));
            await tcs.Task;
        }

        [Test]
        public async Task TestSendMessageWithConnection()
        {
            var tcs = Util.CreateTask<bool> ();
            var te = Util.CreateTimeEntryData(DateTime.Now, userData.RemoteId.Value, userData.DefaultWorkspaceRemoteId);

            networkSwitcher.SetNetworkConnection(true);
            RxChain.Send(new DataMsg.TimeEntryPut(te), new RxChain.Continuation((_, sent, queued) =>
            {
                try
                {
                    // As there's connection, message should have been sent
                    Assert.False(queued.Any(x => x.Data.Id == te.Id));
                    Assert.AreEqual(1, sent.Count());
                    tcs.SetResult(true);
                }
                catch (Exception ex)
                {
                    tcs.SetException(ex);
                }
            }));
            await tcs.Task;
        }

        [Test]
        public async Task TestTrySendMessageAndReconnect()
        {
            var tcs = Util.CreateTask<bool> ();
            var te = Util.CreateTimeEntryData(DateTime.Now, userData.RemoteId.Value, userData.DefaultWorkspaceRemoteId);
            var te2 = Util.CreateTimeEntryData(DateTime.Now + TimeSpan.FromMinutes(5), userData.RemoteId.Value, userData.DefaultWorkspaceRemoteId);

            networkSwitcher.SetNetworkConnection(false);
            RxChain.Send(new DataMsg.TimeEntryPut(te), new RxChain.Continuation((_, sent, queued) =>
            {
                try
                {
                    // As there's no connection, message should have been enqueued
                    Assert.True(queued.Any(x => x.Data.Id == te.Id));
                    Assert.AreEqual(0, sent.Count());
                }
                catch (Exception ex)
                {
                    tcs.SetException(ex);
                }
            }));

            networkSwitcher.SetNetworkConnection(true);
            RxChain.Send(new DataMsg.TimeEntryPut(te2), new RxChain.Continuation((_, sent, queued) =>
            {
                try
                {
                    // As there's connection, messages should have been sent
                    Assert.False(queued.Any(x => x.Data.Id == te.Id || x.Data.Id == te2.Id));
                    Assert.True(sent.Count() > 0);
                    tcs.SetResult(true);
                }
                catch (Exception ex)
                {
                    tcs.SetException(ex);
                }
            }));
            await tcs.Task;
        }

        [Test]
        public async Task TestSendEntriesOlderThanTwoMonths()
        {
            var tcs = Util.CreateTask<bool> ();
            var te = Util.CreateTimeEntryData(DateTime.Today.AddDays(-90), userData.RemoteId.Value, userData.DefaultWorkspaceRemoteId);

            networkSwitcher.SetNetworkConnection(true);
            RxChain.Send(new DataMsg.TimeEntryPut(te), new RxChain.Continuation((_, sent, queued) =>
            {
                try
                {
                    // As there's connection, message should have been sent
                    Assert.False(queued.Any(x => x.Data.Id == te.Id));
                    Assert.AreEqual(1, sent.Count());
                    tcs.SetResult(true);
                }
                catch (Exception ex)
                {
                    tcs.SetException(ex);
                }
            }));
            await tcs.Task;
        }
    }
}
