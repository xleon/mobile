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

namespace Toggl.Phoebe.Tests.Staging
{
    [TestFixture]
    public class SyncOutManagerTest2 : Test
    {
        UserJson userJson;
        TogglRestClient togglClient;

        public override void Init()
        {
            base.Init();

            var platformUtils = new PlatformUtils()
            {
                AppIdentifier = "TogglPhoebe",
                AppVersion = "0.1"
            };
            ServiceContainer.RegisterScoped<IPlatformUtils> (platformUtils);

            togglClient = new TogglRestClient(Build.ApiUrl);
            ServiceContainer.RegisterScoped<ITogglClient> (togglClient);
            RxChain.Init(Util.GetInitAppState());

            var tmpUser = new UserJson()
            {
                Email = string.Format("mobile.{0}@toggl.com", Util.UserId),
                Password = "123456",
                Timezone = Time.TimeZoneId,
            };
            userJson = await togglClient.Create(tmpUser);
            togglClient.Authenticate(userJson.ApiToken);
        }

        public async override void Cleanup()
        {
            if (userJson != null)
            {
                try
                {
                    await togglClient.Delete(userJson);
                }
                catch (Exception ex)
                {
                    throw ex;
                }
            }
            RxChain.Cleanup();
            base.Cleanup();
        }

        [Test]
        public async Task TestSendMessageWithoutConnection()
        {
            var tcs = Util.CreateTask<bool> ();
            var te = Util.CreateTimeEntryData(DateTime.Now, userJson.RemoteId.Value, userJson.DefaultWorkspaceRemoteId);

            RxChain.Send(
                new DataMsg.TimeEntryPut(te), new SyncTestOptions(false, (_, sent, queued) =>
            {
                try
                {
                    // As there's no connection, message should have been enqueued
                    Assert.That(queued.Any(x => x.LocalId == te.Id), Is.True);
                    Assert.That(0, Is.EqualTo(sent.Count));
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
            var te = Util.CreateTimeEntryData(DateTime.Now, userJson.RemoteId.Value, userJson.DefaultWorkspaceRemoteId);

            RxChain.Send(
                new DataMsg.TimeEntryPut(te), new SyncTestOptions(true, (_, sent, queued) =>
            {
                try
                {
                    // As there's connection, message should have been sent
                    Assert.False(queued.Any(x => x.LocalId == te.Id));
                    Assert.AreEqual(1, sent.Count);
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
            var te = Util.CreateTimeEntryData(DateTime.Now, userJson.RemoteId.Value, userJson.DefaultWorkspaceRemoteId);
            var te2 = Util.CreateTimeEntryData(DateTime.Now + TimeSpan.FromMinutes(5), userJson.RemoteId.Value, userJson.DefaultWorkspaceRemoteId);

            RxChain.Send(
                new DataMsg.TimeEntryPut(te), new SyncTestOptions(false, (_, sent, queued) =>
            {
                try
                {
                    // As there's no connection, message should have been enqueued
                    Assert.True(queued.Any(x => x.LocalId == te.Id));
                    Assert.AreEqual(0, sent.Count);
                }
                catch (Exception ex)
                {
                    tcs.SetException(ex);
                }
            }));

            RxChain.Send(
                new DataMsg.TimeEntryPut(te2), new SyncTestOptions(true, (_, sent, queued) =>
            {
                try
                {
                    // As there's connection, messages should have been sent
                    Assert.False(queued.Any(x => x.LocalId == te.Id || x.LocalId == te2.Id));
                    Assert.True(sent.Count > 0);
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
