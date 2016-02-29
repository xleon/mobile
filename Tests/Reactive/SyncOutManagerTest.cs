using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using Toggl.Phoebe.Tests;
using Toggl.Phoebe._Data;
using Toggl.Phoebe._Data.Json;
using Toggl.Phoebe._Data.Models;
using Toggl.Phoebe._Net;
using Toggl.Phoebe._Reactive;
using XPlatUtils;

namespace Toggl.Phoebe.Tests.Reactive
{
    [TestFixture]
    public class SyncOutManagerTest : Test
    {
        readonly ToggleClientMock togglClient = new ToggleClientMock ();

        public override void Init ()
        {
            base.Init ();

            var platformUtils = new PlatformUtils ();
            ServiceContainer.RegisterScoped<IPlatformUtils> (platformUtils);
            ServiceContainer.RegisterScoped<ITogglClient> (togglClient);

            RxChain.Init (Util.GetInitAppState ());
        }

        public override void Cleanup ()
        {
            base.Cleanup ();
            RxChain.Cleanup ();
        }

        [Test]
        public void TestSendMessageWithoutConnection ()
        {
            var tcs = Util.CreateTask<bool> ();
            var te = Util.CreateTimeEntryData (DateTime.Now);
            
            RunAsync (async () => {
                RxChain.Send (
                    new DataMsg.TimeEntryPut (te), new SyncTestOptions (false, (_, sent, queued) => {
                        try {
                            // As there's no connection, message should have been enqueued
                            Assert.True (queued.Any (x => x.LocalId == te.Id));
                            Assert.AreEqual (0, sent.Count);
                            tcs.SetResult (true);
                        }
                        catch (Exception ex) {
                            tcs.SetException (ex);
                        }                        
                    }));
                await tcs.Task;
            });
        }

        [Test]
        public void TestSendMessageWithConnection ()
        {
            var tcs = Util.CreateTask<bool> ();
            var te = Util.CreateTimeEntryData (DateTime.Now);

            RunAsync (async () => {
                RxChain.Send (
                    new DataMsg.TimeEntryPut (te), new SyncTestOptions (true, (_, sent, queued) => {
                        try {
                            // As there's connection, message should have been sent
                            Assert.False (queued.Any (x => x.LocalId == te.Id));
                            Assert.AreEqual (1, sent.Count);
                            tcs.SetResult (true);
                        }
                        catch (Exception ex) {
                            tcs.SetException (ex);
                        }                        
                    }));
                await tcs.Task;
            });
        }

        [Test]
        public void TestTrySendMessageAndReconnect ()
        {
            var tcs = Util.CreateTask<bool> ();
            var te = Util.CreateTimeEntryData (DateTime.Now);
            var te2 = Util.CreateTimeEntryData (DateTime.Now + TimeSpan.FromMinutes(5));

            RunAsync (async () => {
                RxChain.Send (
                    new DataMsg.TimeEntryPut (te), new SyncTestOptions (false, (_, sent, queued) => {
                        try {
                            // As there's no connection, message should have been enqueued
                            Assert.True (queued.Any (x => x.LocalId == te.Id));
                            Assert.AreEqual (0, sent.Count);
                        }
                        catch (Exception ex) {
                            tcs.SetException (ex);
                        }                        
                    }));

                RxChain.Send (
                    new DataMsg.TimeEntryPut (te2), new SyncTestOptions (true, (_, sent, queued) => {
                        try {
                            // As there's connection, messages should have been sent
                            Assert.False (queued.Any (x => x.LocalId == te.Id || x.LocalId == te2.Id));
                            Assert.True (sent.Count > 0);
                            tcs.SetResult (true);
                        }
                        catch (Exception ex) {
                            tcs.SetException (ex);
                        }                        
                    }));
                await tcs.Task;
            });
        }
    }
}

