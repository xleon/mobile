using System;
using NUnit.Framework;
using SQLite.Net.Interop;
using SQLite.Net.Platform.Generic;
using Toggl.Phoebe._Data.Json;
using Toggl.Phoebe._Data.Models;
using XPlatUtils;

namespace Toggl.Phoebe.Tests.Data.Mapper
{
    public class MapperJsonToDataTest : Test
    {
        JsonMapper mapper;

        [SetUp]
        public override void SetUp()
        {
            base.SetUp();
            ServiceContainer.Register<IPlatformUtils> (new PlatformUtils());
            mapper = new JsonMapper ();
        }

        [Test]
        public void TestCommonDataMap()
        {
            CommonJson commonJson = new ClientJson {
                RemoteId = 123,
                ModifiedAt = DateTime.Now,
                DeletedAt = null,
                Name = "Client",
                WorkspaceRemoteId = 321
            };

            var commonData = mapper.Map<CommonData> (commonJson);

            Assert.AreEqual (commonData.RemoteId, commonJson.RemoteId);
            Assert.AreEqual (commonData.ModifiedAt, commonJson.ModifiedAt.ToUtc());
            Assert.AreEqual (commonData.DeletedAt, null);
            Assert.AreEqual (commonData.IsDirty, false);
            Assert.AreEqual (commonData.RemoteRejected, false);
            Assert.AreEqual (commonData.Id, Guid.Empty);

            commonJson.DeletedAt = DateTime.Now;
            commonData = mapper.Map<CommonData> (commonJson);
            Assert.AreEqual (commonData.DeletedAt, commonJson.DeletedAt.ToUtc ());
        }

        [Test]
        public void TestMappingInheritance()
        {
            // Check that rules defined for CommonJson -> CommonData
            // works for a descendent object like ClientData.
            var clientJson = new ClientJson {
                RemoteId = 123,
                ModifiedAt = DateTime.Now,
                DeletedAt = null,
                Name = "Client",
                WorkspaceRemoteId = 321
            };

            var clientData = mapper.Map<ClientData> (clientJson);

            Assert.AreEqual (clientData.RemoteId, clientJson.RemoteId);
            Assert.AreEqual (clientData.ModifiedAt, clientJson.ModifiedAt.ToUtc());
            Assert.AreEqual (clientData.DeletedAt, null);
            Assert.AreEqual (clientData.IsDirty, false);
            Assert.AreEqual (clientData.RemoteRejected, false);
            Assert.AreEqual (clientData.Id, Guid.Empty);

            clientJson.DeletedAt = DateTime.Now;
            clientData = mapper.Map<ClientData> (clientJson);
            Assert.AreEqual (clientData.DeletedAt, clientJson.DeletedAt.ToUtc ());
        }

        [Test]
        public void TestClientDataMap()
        {
            var clientJson = new ClientJson {
                RemoteId = 123,
                ModifiedAt = DateTime.Now,
                DeletedAt = null,
                Name = "Client",
                WorkspaceRemoteId = 321
            };

            var clientData = mapper.Map<ClientData> (clientJson);

            Assert.AreEqual (clientData.Name, clientJson.Name);
            Assert.AreEqual (clientData.WorkspaceRemoteId, clientJson.WorkspaceRemoteId);
            Assert.AreEqual (clientData.WorkspaceId, Guid.Empty);
        }

        [Test]
        public void TestProjectDataMap()
        {
            var projectJson = new ProjectJson {
                RemoteId = 123,
                ModifiedAt = DateTime.Now,
                DeletedAt = DateTime.Now,
                Name = "Project",
                WorkspaceRemoteId = 321,
                ClientRemoteId = 333,
                IsActive = true,
                IsPrivate = true,
                Color = "1",
                IsBillable = true,
                IsTemplate = true,
                UseTasksEstimate = true
            };

            var projectData = mapper.Map<ProjectData> (projectJson);

            Assert.AreEqual (projectData.Name, projectJson.Name);
            Assert.AreEqual (projectData.IsActive, projectJson.IsActive);
            Assert.AreEqual (projectData.IsPrivate, projectJson.IsPrivate);
            Assert.AreEqual (projectData.IsBillable, projectJson.IsBillable);
            Assert.AreEqual (projectData.IsTemplate, projectJson.IsTemplate);
            Assert.AreEqual (projectData.UseTasksEstimate, projectJson.UseTasksEstimate);
            Assert.AreEqual (projectData.Color, int.Parse (projectJson.Color));
            Assert.AreEqual (projectData.ClientRemoteId, projectJson.ClientRemoteId);
            Assert.AreEqual (projectData.ClientId, null);
            Assert.AreEqual (projectData.WorkspaceRemoteId, projectJson.WorkspaceRemoteId);
            Assert.AreEqual (projectData.WorkspaceId, Guid.Empty);
        }

        [Test]
        public void TestTagDataMap()
        {
            var tagJson = new TagJson {
                RemoteId = 123,
                ModifiedAt = DateTime.Now,
                DeletedAt = DateTime.Now,
                Name = "Tag",
                WorkspaceRemoteId = 321,
            };

            var tagData = mapper.Map<TagData> (tagJson);

            Assert.AreEqual (tagData.Name, tagJson.Name);
            Assert.AreEqual (tagData.WorkspaceRemoteId, tagJson.WorkspaceRemoteId);
            Assert.AreEqual (tagData.WorkspaceId, Guid.Empty);

        }

        [Test]
        public void TestTaskDataMap()
        {
            var taskJson = new TaskJson {
                RemoteId = 123,
                ModifiedAt = DateTime.Now,
                DeletedAt = DateTime.Now,
                Name = "Task",
                WorkspaceRemoteId = 321,
                ProjectRemoteId = 111,
                Estimate = 12,
                IsActive = true
            };

            var taskData = mapper.Map<TaskData> (taskJson);

            Assert.AreEqual (taskData.Name, taskJson.Name);
            Assert.AreEqual (taskData.WorkspaceRemoteId, taskJson.WorkspaceRemoteId);
            Assert.AreEqual (taskData.WorkspaceId, Guid.Empty);
            Assert.AreEqual (taskData.ProjectId, Guid.Empty);
            Assert.AreEqual (taskData.ProjectRemoteId, taskJson.ProjectRemoteId);
            Assert.AreEqual (taskData.IsActive, taskJson.IsActive);
            Assert.AreEqual (taskData.Estimate, taskJson.Estimate);
        }

        [Test]
        public void TestWorkspaceUserDataMap()
        {
            var workspaceUserJson = new WorkspaceUserJson {
                RemoteId = 123,
                ModifiedAt = DateTime.Now,
                DeletedAt = DateTime.Now,
                Name = "WsU",  // Not used on WorkspaceUserData
                WorkspaceRemoteId = 321,
                IsActive = true,
                Email = "support@toggl.com",
                IsAdmin = true,
                UserRemoteId = 123
            };

            var workspaceUserData = mapper.Map<WorkspaceUserData> (workspaceUserJson);

            Assert.AreEqual (workspaceUserData.IsActive, workspaceUserJson.IsActive);
            Assert.AreEqual (workspaceUserData.IsAdmin, workspaceUserJson.IsAdmin);
            Assert.AreEqual (workspaceUserData.UserRemoteId, workspaceUserJson.UserRemoteId);
            Assert.AreEqual (workspaceUserData.WorkspaceRemoteId, workspaceUserJson.WorkspaceRemoteId);
            Assert.AreEqual (workspaceUserData.WorkspaceId, Guid.Empty);
            Assert.AreEqual (workspaceUserData.Email, workspaceUserJson.Email);
            Assert.AreEqual (workspaceUserData.UserRemoteId, workspaceUserJson.UserRemoteId);
            Assert.AreEqual (workspaceUserData.UserId, Guid.Empty);
        }

        [Test]
        public void TestWorkspaceDataMap()
        {
            var workspaceJson = new WorkspaceJson {
                RemoteId = 123,
                ModifiedAt = DateTime.Now,
                DeletedAt = DateTime.Now,
                Name = "WsU",
                IsAdmin = true,
                DefaultCurrency = "euro",
                DefaultRate = 1,
                IsPremium = true,
                LogoUrl = "http://logout.com",
                OnlyAdminsMayCreateProjects = true,
                OnlyAdminsSeeBillableRates = true,
                RoundingMode = RoundingMode.Up,
                RoundingPercision = 1
            };

            var wData = mapper.Map<WorkspaceData> (workspaceJson);

            Assert.AreEqual (wData.IsPremium, workspaceJson.IsPremium);
            Assert.AreEqual (wData.DefaultRate, workspaceJson.DefaultRate);
            Assert.AreEqual (wData.DefaultCurrency, workspaceJson.DefaultCurrency);
            Assert.AreEqual (wData.IsAdmin, workspaceJson.IsAdmin);
            Assert.AreEqual (wData.Name, workspaceJson.Name);
            Assert.AreEqual (wData.LogoUrl, workspaceJson.LogoUrl);
            Assert.AreEqual (wData.OnlyAdminsMayCreateProjects, workspaceJson.OnlyAdminsMayCreateProjects);
            Assert.AreEqual (wData.OnlyAdminsSeeBillableRates, workspaceJson.OnlyAdminsSeeBillableRates);
            Assert.AreEqual (wData.RoundingMode, workspaceJson.RoundingMode);
            Assert.AreEqual (wData.RoundingPercision, workspaceJson.RoundingPercision);
        }

        [Test]
        public void TestUserDataMap()
        {
            var obmJson = new OBMJson {
                Included = true,
                Number = 10
            };

            var userJson = new UserJson {
                RemoteId = 123,
                ModifiedAt = DateTime.Now,
                DeletedAt = DateTime.Now,
                Name = "User",
                ApiToken = "123",
                Email = "support@toggl.com",
                DateFormat = "MM-DD-YY",
                DurationFormat = _Data.DurationFormat.Classic,
                GoogleAccessToken = "GoogleToken",
                DefaultWorkspaceRemoteId = 111,
                ImageUrl = "image.jpg",
                Locale = "locale",
                OBM = obmJson,
                Password = "pass", // Not used on UserData
                SendProductEmails = true,
                SendTimerNotifications = true,
                SendWeeklyReport = true,
                StartOfWeek = DayOfWeek.Monday,
                StoreStartAndStopTime = true, // Not used on UserData
                TimeFormat = "MM:SS",
                Timezone = "TimeZone"
            };

            var userData = mapper.Map<UserData> (userJson);

            Assert.AreEqual (userData.ApiToken, userJson.ApiToken);
            Assert.AreEqual (userData.DateFormat, userJson.DateFormat);
            Assert.AreEqual (userData.DefaultWorkspaceId, Guid.Empty);
            Assert.AreEqual (userData.DefaultWorkspaceRemoteId, userJson.DefaultWorkspaceRemoteId);
            Assert.AreEqual (userData.DurationFormat, userJson.DurationFormat);
            Assert.AreEqual (userData.Email, userJson.Email);
            Assert.AreEqual (userData.ExperimentIncluded, userJson.OBM.Included);
            Assert.AreEqual (userData.ExperimentNumber, userJson.OBM.Number);
            Assert.AreEqual (userData.GoogleAccessToken, userJson.GoogleAccessToken);
            Assert.AreEqual (userData.ImageUrl, userJson.ImageUrl);
            Assert.AreEqual (userData.Locale, userJson.Locale);
            Assert.AreEqual (userData.Name, userJson.Name);
            Assert.AreEqual (userData.SendProductEmails, userJson.SendProductEmails);
            Assert.AreEqual (userData.SendTimerNotifications, userJson.SendTimerNotifications);
            Assert.AreEqual (userData.StartOfWeek, userJson.StartOfWeek);
            Assert.AreEqual (userData.SendWeeklyReport, userJson.SendWeeklyReport);
            Assert.AreEqual (userData.TimeFormat, userJson.TimeFormat);
            Assert.AreEqual (userData.Timezone, userJson.Timezone);
        }

        [Test]
        public void TestTimeEntryDataMap()
        {
            var tags = new System.Collections.Generic.List<string> {"tag1", "tag2", "tag3"};
            var duration = TimeSpan.FromMinutes (3);
            var startTime = new DateTime (DateTime.Now.Ticks);
            var stopTime = startTime + duration;
            var runningDuration = -Convert.ToInt64 ((startTime.ToUtc().Subtract (new DateTime (1970,1,1,0,0,0))).TotalSeconds);

            var teJson = new TimeEntryJson {
                RemoteId = 123,
                ModifiedAt = DateTime.Now,
                DeletedAt = DateTime.Now,
                Description = "description",
                Duration = Convert.ToInt64 (duration.TotalSeconds), // Duration comes in seconds!
                IsBillable = true,
                ProjectRemoteId = 123,
                DurationOnly = true,
                StartTime = startTime,
                StopTime = stopTime,
                Tags = tags,
                TaskRemoteId = null,
                UserRemoteId = 333,
                WorkspaceRemoteId = 222
            };

            var teData = mapper.Map<TimeEntryData> (teJson);

            Assert.AreEqual (teData.Description, teJson.Description);
            Assert.AreEqual (teData.DurationOnly, teJson.DurationOnly);
            Assert.AreEqual (teData.IsBillable, teJson.IsBillable);
            Assert.AreEqual (teData.ProjectId, null);
            Assert.AreEqual (teData.ProjectRemoteId, teJson.ProjectRemoteId);
            Assert.AreEqual (teData.StartTime, teJson.StartTime.ToUtc ());
            Assert.AreEqual (teData.StopTime, teJson.StopTime.ToUtc ());
            Assert.AreEqual (teData.State, TimeEntryState.Finished);
            Assert.AreEqual (teData.Tags, tags);
            Assert.AreEqual (teData.TaskId, null);
            Assert.AreEqual (teData.TaskRemoteId, teJson.TaskRemoteId);
            Assert.AreEqual (teData.UserId, Guid.Empty);
            Assert.AreEqual (teData.UserRemoteId, teJson.UserRemoteId);
            Assert.AreEqual (teData.WorkspaceId, Guid.Empty);
            Assert.AreEqual (teData.WorkspaceRemoteId, teJson.WorkspaceRemoteId);

            teJson.Duration = runningDuration;
            teData = mapper.Map<TimeEntryData> (teJson);

            //TODO: check startTime.
            Assert.AreEqual (teData.StopTime, null);
            Assert.AreEqual (teData.State, TimeEntryState.Running);
        }

        private class PlatformUtils : IPlatformUtils
        {
            public string AppIdentifier { get; set; }

            public string AppVersion { get; set; }

            public bool IsWidgetAvailable { get; set; }

            public ISQLitePlatform SQLiteInfo
            {
                get {
                    return new SQLitePlatformGeneric();
                }
            }

            public void DispatchOnUIThread (Action action)
            {
                action.Invoke();
            }
        }


    }
}
