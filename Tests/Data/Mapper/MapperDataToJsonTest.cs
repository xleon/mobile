using System;
using NUnit.Framework;
using SQLite.Net.Interop;
using SQLite.Net.Platform.Generic;
using Toggl.Phoebe._Data.Json;
using Toggl.Phoebe._Data.Models;
using XPlatUtils;

namespace Toggl.Phoebe.Tests.Data.Mapper
{
    public class MapperDataToJsonTest : Test
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
        public void TestCommonJsonMap()
        {
            var commonData = new ClientData {
                Id = Guid.NewGuid(),
                ModifiedAt = DateTime.Now,
                DeletedAt = DateTime.Now,
                Name = "Client",
                WorkspaceId = Guid.NewGuid(),
                WorkspaceRemoteId = 321,
                RemoteId = 123
            };

            var commonJson = mapper.Map<CommonJson> (commonData);

            Assert.AreEqual (commonJson.RemoteId, commonData.RemoteId);
            Assert.AreEqual (commonJson.ModifiedAt, commonData.ModifiedAt.ToUtc ());
        }

        [Test]
        public void TestMappingInheritance()
        {
            // Check that rules defined for CommonData -> CommonJson
            // works for a descendent object like ClientData.
            var clientData = new ClientData {
                Id = Guid.NewGuid(),
                ModifiedAt = DateTime.Now,
                DeletedAt = null,
                Name = "Client",
                WorkspaceId = Guid.NewGuid(),
                WorkspaceRemoteId = 321,
                RemoteId = 123
            };

            var clientJson = mapper.Map<ClientJson> (clientData);

            Assert.AreEqual (clientJson.RemoteId, clientJson.RemoteId);
            Assert.AreEqual (clientJson.ModifiedAt, clientJson.ModifiedAt.ToUtc());
            Assert.AreEqual (clientJson.DeletedAt, clientJson.DeletedAt.ToUtc ());

            clientData.DeletedAt = DateTime.Now;
            clientJson = mapper.Map<ClientJson> (clientData);
            Assert.AreEqual (clientJson.DeletedAt, clientJson.DeletedAt.ToUtc ());
        }

        [Test]
        public void TestClientJsonMap()
        {
            var clientData = new ClientData {
                Name = "Client",
                WorkspaceId = Guid.NewGuid(),
                WorkspaceRemoteId = 321,
                RemoteId = 123
            };

            var clientJson = mapper.Map<ClientJson> (clientData);

            Assert.AreEqual (clientJson.WorkspaceRemoteId, clientData.WorkspaceRemoteId);
            Assert.AreEqual (clientJson.Name, clientData.Name);
        }

        [Test]
        public void TestProjectJsonMap()
        {
            var projectData = new ProjectData {
                Name = "Project",
                ClientId = Guid.NewGuid (),
                ClientRemoteId = 333,
                WorkspaceId = Guid.NewGuid(),
                WorkspaceRemoteId = 321,
                RemoteId = 123,
                Color = 3,
                IsActive = true,
                IsBillable = true,
                IsDirty = true,
                IsPrivate = true,
                IsTemplate = true,
                UseTasksEstimate = true,
                RemoteRejected = false
            };

            var projectJson = mapper.Map<ProjectJson> (projectData);

            Assert.AreEqual (projectJson.Name, projectData.Name);
            Assert.AreEqual (projectJson.IsActive, projectData.IsActive);
            Assert.AreEqual (projectJson.IsPrivate, projectData.IsPrivate);
            Assert.AreEqual (projectJson.IsBillable, projectData.IsBillable);
            Assert.AreEqual (projectJson.IsTemplate, projectData.IsTemplate);
            Assert.AreEqual (projectJson.UseTasksEstimate, projectData.UseTasksEstimate);
            Assert.AreEqual (projectJson.Color, projectData.Color.ToString ());
            Assert.AreEqual (projectJson.ClientRemoteId, projectData.ClientRemoteId);
            Assert.AreEqual (projectJson.WorkspaceRemoteId, projectData.WorkspaceRemoteId);
        }

        [Test]
        public void TestTagJsonMap()
        {
            var tagData = new TagData {
                RemoteId = 123,
                Name = "Tag",
                WorkspaceId = Guid.Empty,
                WorkspaceRemoteId = 321,
            };

            var tagJson = mapper.Map<TagJson> (tagData);

            Assert.AreEqual (tagJson.Name, tagData.Name);
            Assert.AreEqual (tagJson.WorkspaceRemoteId, tagData.WorkspaceRemoteId);
        }

        [Test]
        public void TestTaskJsonMap()
        {
            var taskData = new TaskData {
                RemoteId = 123,
                Name = "Task",
                WorkspaceRemoteId = 321,
                ProjectRemoteId = 111,
                Estimate = 12,
                IsActive = true
            };

            var taskJson = mapper.Map<TaskJson> (taskData);

            Assert.AreEqual (taskJson.Name, taskData.Name);
            Assert.AreEqual (taskJson.WorkspaceRemoteId, taskData.WorkspaceRemoteId);
            Assert.AreEqual (taskJson.ProjectRemoteId, taskData.ProjectRemoteId);
            Assert.AreEqual (taskJson.IsActive, taskData.IsActive);
            Assert.AreEqual (taskJson.Estimate, taskData.Estimate);
        }

        [Test]
        public void TestWorkspaceUserJsonMap()
        {
            var workspaceUserData = new WorkspaceUserData {
                RemoteRejected = true,
                WorkspaceId = Guid.Empty,
                UserId = Guid.Empty,
                RemoteId = 123,
                WorkspaceRemoteId = 321,
                IsActive = true,
                Email = "support@toggl.com",
                IsAdmin = true,
                UserRemoteId = 123
            };

            var workspaceUserJson = mapper.Map<WorkspaceUserJson> (workspaceUserData);

            Assert.AreEqual (workspaceUserJson.IsActive, workspaceUserData.IsActive);
            Assert.AreEqual (workspaceUserJson.IsAdmin, workspaceUserData.IsAdmin);
            Assert.AreEqual (workspaceUserJson.UserRemoteId, workspaceUserData.UserRemoteId);
            Assert.AreEqual (workspaceUserJson.WorkspaceRemoteId, workspaceUserData.WorkspaceRemoteId);
            Assert.AreEqual (workspaceUserJson.Email, workspaceUserData.Email);
            Assert.AreEqual (workspaceUserJson.UserRemoteId, workspaceUserData.UserRemoteId);
        }

        [Test]
        public void TestWorkspaceJsonMap()
        {
            var workspaceData = new WorkspaceData {
                RemoteId = 123,
                Name = "WsU",
                IsAdmin = true,
                DefaultCurrency = "euro",
                DefaultRate = 1,
                IsPremium = true,
                LogoUrl = "http://logout.com",
                OnlyAdminsMayCreateProjects = true,
                OnlyAdminsSeeBillableRates = true,
                RoundingMode = RoundingMode.Up,
                RoundingPercision = 1,
                BillableRatesVisibility = AccessLevel.Admin,
                ProjectCreationPrivileges = AccessLevel.Regular
            };

            var wData = mapper.Map<WorkspaceData> (workspaceData);

            Assert.AreEqual (wData.IsPremium, workspaceData.IsPremium);
            Assert.AreEqual (wData.DefaultRate, workspaceData.DefaultRate);
            Assert.AreEqual (wData.DefaultCurrency, workspaceData.DefaultCurrency);
            Assert.AreEqual (wData.IsAdmin, workspaceData.IsAdmin);
            Assert.AreEqual (wData.Name, workspaceData.Name);
            Assert.AreEqual (wData.LogoUrl, workspaceData.LogoUrl);
            Assert.AreEqual (wData.OnlyAdminsMayCreateProjects, workspaceData.OnlyAdminsMayCreateProjects);
            Assert.AreEqual (wData.OnlyAdminsSeeBillableRates, workspaceData.OnlyAdminsSeeBillableRates);
            Assert.AreEqual (wData.RoundingMode, workspaceData.RoundingMode);
            Assert.AreEqual (wData.RoundingPercision, workspaceData.RoundingPercision);
        }

        [Test]
        public void TestUserJsonMap()
        {
            var userData = new UserData {
                RemoteId = 123,
                Name = "User",
                ApiToken = "123",
                Email = "support@toggl.com",
                DateFormat = "MM-DD-YY",
                DurationFormat = _Data.DurationFormat.Classic,
                GoogleAccessToken = "GoogleToken",
                DefaultWorkspaceRemoteId = 111,
                ImageUrl = "image.jpg",
                Locale = "locale",
                ExperimentIncluded = true,
                ExperimentNumber = 1,
                SendProductEmails = true,
                SendTimerNotifications = true,
                SendWeeklyReport = true,
                StartOfWeek = DayOfWeek.Monday,
                TimeFormat = "MM:SS",
                Timezone = "TimeZone"
            };

            var userJson = mapper.Map<UserJson> (userData);

            Assert.AreEqual (userJson.ApiToken, userData.ApiToken);
            Assert.AreEqual (userJson.DateFormat, userData.DateFormat);
            Assert.AreEqual (userJson.DefaultWorkspaceRemoteId, userData.DefaultWorkspaceRemoteId);
            Assert.AreEqual (userJson.DurationFormat, userData.DurationFormat);
            Assert.AreEqual (userJson.Email, userData.Email);
            Assert.AreEqual (userJson.OBM.Included, userData.ExperimentIncluded);
            Assert.AreEqual (userJson.OBM.Number, userData.ExperimentNumber);
            Assert.AreEqual (userJson.GoogleAccessToken, userData.GoogleAccessToken);
            Assert.AreEqual (userJson.ImageUrl, userData.ImageUrl);
            Assert.AreEqual (userJson.Locale, userData.Locale);
            Assert.AreEqual (userJson.Name, userData.Name);
            Assert.AreEqual (userJson.SendProductEmails, userData.SendProductEmails);
            Assert.AreEqual (userJson.SendTimerNotifications, userData.SendTimerNotifications);
            Assert.AreEqual (userJson.StartOfWeek, userData.StartOfWeek);
            Assert.AreEqual (userJson.SendWeeklyReport, userData.SendWeeklyReport);
            Assert.AreEqual (userJson.TimeFormat, userData.TimeFormat);
            Assert.AreEqual (userJson.Timezone, userData.Timezone);
            Assert.AreEqual (userJson.CreatedWith, Platform.DefaultCreatedWith);
        }

        [Test]
        public void TestTimeEntryJsonMap()
        {
            var tags = new System.Collections.Generic.List<string> {"tag1", "tag2", "tag3"};
            var duration = TimeSpan.FromMinutes (3);
            var startTime = new DateTime (DateTime.Now.Ticks);
            var stopTime = startTime + duration;

            var teData = new TimeEntryData {
                RemoteId = 123,
                Description = "description",
                IsBillable = true,
                ProjectRemoteId = 123,
                DurationOnly = true,
                StartTime = startTime,
                StopTime = stopTime,
                Tags = tags,
                TaskRemoteId = null,
                UserRemoteId = 333,
                WorkspaceRemoteId = 222,
                State = TimeEntryState.Finished
            };

            var teJson = mapper.Map<TimeEntryJson> (teData);

            Assert.AreEqual (teJson.Description, teData.Description);
            Assert.AreEqual (teJson.DurationOnly, teData.DurationOnly);
            Assert.AreEqual (teJson.IsBillable, teData.IsBillable);
            Assert.AreEqual (teJson.ProjectRemoteId, teData.ProjectRemoteId);
            Assert.AreEqual (teJson.StartTime, teData.StartTime.ToUtc ());
            Assert.AreEqual (teJson.StopTime, teData.StopTime.ToUtc ());
            Assert.AreEqual (teJson.Duration, Convert.ToInt64 (duration.TotalSeconds));
            Assert.IsTrue (teJson.Duration > 0);
            Assert.AreEqual (teJson.Tags, tags);
            Assert.AreEqual (teJson.TaskRemoteId, teData.TaskRemoteId);
            Assert.AreEqual (teJson.UserRemoteId, teData.UserRemoteId);
            Assert.AreEqual (teJson.WorkspaceRemoteId, teData.WorkspaceRemoteId);
            Assert.AreEqual (teJson.CreatedWith, Platform.DefaultCreatedWith);

            teData.State = TimeEntryState.Running;
            teJson = mapper.Map<TimeEntryJson> (teData);

            //TODO: check startTime.
            Assert.AreEqual (teJson.StopTime, stopTime.ToUtc ());
            Assert.IsTrue (teJson.Duration < 0);
            Assert.AreEqual (teJson.StartTime, startTime.ToUtc ());
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
