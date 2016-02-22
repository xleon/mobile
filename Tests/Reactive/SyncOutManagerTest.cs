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
        public class ToggleClientMock : ITogglClient
        {
            public Random rnd = new Random ();
            public IList<CommonJson> ReceivedItems = new List<CommonJson> ();

            public Task<T> Create<T> (T jsonObject) where T : CommonJson
            {
                return Task.Run (() => {
                    ReceivedItems.Add (jsonObject);
                    jsonObject.RemoteId = rnd.Next (100);
                    return jsonObject;
                });
            }
            public Task<T> Get<T> (long id) where T : CommonJson
            {
                throw new NotImplementedException ();
            }
            public Task<List<T>> List<T> () where T : CommonJson
            {
                throw new NotImplementedException ();
            }
            public Task<T> Update<T> (T jsonObject) where T : CommonJson
            {
                return Task.Run (() => {
                    ReceivedItems.Add (jsonObject);
                    return jsonObject;
                });
            }
            public Task Delete<T> (T jsonObject) where T : CommonJson
            {
                return Task.Run (() => {
                    ReceivedItems.Add (jsonObject);
                });
            }
            public Task Delete<T> (IEnumerable<T> jsonObjects) where T : CommonJson
            {
                throw new NotImplementedException ();
            }
            public Task<UserJson> GetUser (string username, string password)
            {
                throw new NotImplementedException ();
            }
            public Task<UserJson> GetUser (string googleAccessToken)
            {
                throw new NotImplementedException ();
            }
            public Task<List<ClientJson>> ListWorkspaceClients (long workspaceId)
            {
                throw new NotImplementedException ();
            }
            public Task<List<ProjectJson>> ListWorkspaceProjects (long workspaceId)
            {
                throw new NotImplementedException ();
            }
            public Task<List<WorkspaceUserJson>> ListWorkspaceUsers (long workspaceId)
            {
                throw new NotImplementedException ();
            }
            public Task<List<TaskJson>> ListWorkspaceTasks (long workspaceId)
            {
                throw new NotImplementedException ();
            }
            public Task<List<TaskJson>> ListProjectTasks (long projectId)
            {
                throw new NotImplementedException ();
            }
            public Task<List<ProjectUserJson>> ListProjectUsers (long projectId)
            {
                throw new NotImplementedException ();
            }
            public Task<List<TimeEntryJson>> ListTimeEntries (DateTime start, DateTime end, System.Threading.CancellationToken cancellationToken)
            {
                throw new NotImplementedException ();
            }
            public Task<List<TimeEntryJson>> ListTimeEntries (DateTime start, DateTime end)
            {
                throw new NotImplementedException ();
            }
            public Task<List<TimeEntryJson>> ListTimeEntries (DateTime end, int days, System.Threading.CancellationToken cancellationToken)
            {
                throw new NotImplementedException ();
            }
            public Task<List<TimeEntryJson>> ListTimeEntries (DateTime end, int days)
            {
                throw new NotImplementedException ();
            }
            public Task<UserRelatedJson> GetChanges (DateTime? since)
            {
                throw new NotImplementedException ();
            }
            public Task CreateFeedback (FeedbackJson jsonObject)
            {
                throw new NotImplementedException ();
            }
            public Task CreateExperimentAction (ActionJson jsonObject)
            {
                throw new NotImplementedException ();
            }
        }

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
                    new DataMsg.TimeEntryPut (te), new SyncTestOptions (false, (sent, queued) => {
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
                    new DataMsg.TimeEntryPut (te), new SyncTestOptions (true, (sent, queued) => {
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
                    new DataMsg.TimeEntryPut (te), new SyncTestOptions (false, (sent, queued) => {
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
                    new DataMsg.TimeEntryPut (te2), new SyncTestOptions (true, (sent, queued) => {
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

