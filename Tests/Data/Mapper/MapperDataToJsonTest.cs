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

            Assert.That (commonJson.RemoteId, Is.EqualTo (commonData.RemoteId));
            Assert.That (commonJson.ModifiedAt, Is.EqualTo (commonData.ModifiedAt.ToUtc ()));
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

            Assert.That (clientJson.RemoteId, Is.EqualTo (clientJson.RemoteId));
            Assert.That (clientJson.ModifiedAt, Is.EqualTo (clientJson.ModifiedAt.ToUtc()));
            Assert.That (clientJson.DeletedAt, Is.EqualTo (clientJson.DeletedAt.ToUtc ()));

            clientData.DeletedAt = DateTime.Now;
            clientJson = mapper.Map<ClientJson> (clientData);
            Assert.That (clientJson.DeletedAt, Is.EqualTo (clientJson.DeletedAt.ToUtc ()));
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

            Assert.That (clientJson.WorkspaceRemoteId, Is.EqualTo (clientData.WorkspaceRemoteId));
            Assert.That (clientJson.Name, Is.EqualTo (clientData.Name));
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
                SyncPending = true,
                IsPrivate = true,
                IsTemplate = true,
                UseTasksEstimate = true
            };

            var projectJson = mapper.Map<ProjectJson> (projectData);

            Assert.That (projectJson.Name, Is.EqualTo (projectData.Name));
            Assert.That (projectJson.IsActive, Is.EqualTo (projectData.IsActive));
            Assert.That (projectJson.IsPrivate, Is.EqualTo (projectData.IsPrivate));
            Assert.That (projectJson.IsBillable, Is.EqualTo (projectData.IsBillable));
            Assert.That (projectJson.IsTemplate, Is.EqualTo (projectData.IsTemplate));
            Assert.That (projectJson.UseTasksEstimate, Is.EqualTo (projectData.UseTasksEstimate));
            Assert.That (projectJson.Color, Is.EqualTo (projectData.Color.ToString ()));
            Assert.That (projectJson.ClientRemoteId, Is.EqualTo (projectData.ClientRemoteId));
            Assert.That (projectJson.WorkspaceRemoteId, Is.EqualTo (projectData.WorkspaceRemoteId));
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

            Assert.That (tagJson.Name, Is.EqualTo (tagData.Name));
            Assert.That (tagJson.WorkspaceRemoteId, Is.EqualTo (tagData.WorkspaceRemoteId));
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

            Assert.That (taskJson.Name, Is.EqualTo (taskData.Name));
            Assert.That (taskJson.WorkspaceRemoteId, Is.EqualTo (taskData.WorkspaceRemoteId));
            Assert.That (taskJson.ProjectRemoteId, Is.EqualTo (taskData.ProjectRemoteId));
            Assert.That (taskJson.IsActive, Is.EqualTo (taskData.IsActive));
            Assert.That (taskJson.Estimate, Is.EqualTo (taskData.Estimate));
        }

        [Test]
        public void TestWorkspaceUserJsonMap()
        {
            var workspaceUserData = new WorkspaceUserData {
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

            Assert.That (workspaceUserJson.IsActive, Is.EqualTo (workspaceUserData.IsActive));
            Assert.That (workspaceUserJson.IsAdmin, Is.EqualTo (workspaceUserData.IsAdmin));
            Assert.That (workspaceUserJson.UserRemoteId, Is.EqualTo (workspaceUserData.UserRemoteId));
            Assert.That (workspaceUserJson.WorkspaceRemoteId, Is.EqualTo (workspaceUserData.WorkspaceRemoteId));
            Assert.That (workspaceUserJson.Email, Is.EqualTo (workspaceUserData.Email));
            Assert.That (workspaceUserJson.UserRemoteId, Is.EqualTo (workspaceUserData.UserRemoteId));
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

            Assert.That (wData.IsPremium, Is.EqualTo (workspaceData.IsPremium));
            Assert.That (wData.DefaultRate, Is.EqualTo (workspaceData.DefaultRate));
            Assert.That (wData.DefaultCurrency, Is.EqualTo (workspaceData.DefaultCurrency));
            Assert.That (wData.IsAdmin, Is.EqualTo (workspaceData.IsAdmin));
            Assert.That (wData.Name, Is.EqualTo (workspaceData.Name));
            Assert.That (wData.LogoUrl, Is.EqualTo (workspaceData.LogoUrl));
            Assert.That (wData.OnlyAdminsMayCreateProjects, Is.EqualTo (workspaceData.OnlyAdminsMayCreateProjects));
            Assert.That (wData.OnlyAdminsSeeBillableRates, Is.EqualTo (workspaceData.OnlyAdminsSeeBillableRates));
            Assert.That (wData.RoundingMode, Is.EqualTo (workspaceData.RoundingMode));
            Assert.That (wData.RoundingPercision, Is.EqualTo (workspaceData.RoundingPercision));
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

            Assert.That (userJson.ApiToken, Is.EqualTo (userData.ApiToken));
            Assert.That (userJson.DateFormat, Is.EqualTo (userData.DateFormat));
            Assert.That (userJson.DefaultWorkspaceRemoteId, Is.EqualTo (userData.DefaultWorkspaceRemoteId));
            Assert.That (userJson.DurationFormat, Is.EqualTo (userData.DurationFormat));
            Assert.That (userJson.Email, Is.EqualTo (userData.Email));
            Assert.That (userJson.OBM.Included, Is.EqualTo (userData.ExperimentIncluded));
            Assert.That (userJson.OBM.Number, Is.EqualTo (userData.ExperimentNumber));
            Assert.That (userJson.GoogleAccessToken, Is.EqualTo (userData.GoogleAccessToken));
            Assert.That (userJson.ImageUrl, Is.EqualTo (userData.ImageUrl));
            Assert.That (userJson.Locale, Is.EqualTo (userData.Locale));
            Assert.That (userJson.Name, Is.EqualTo (userData.Name));
            Assert.That (userJson.SendProductEmails, Is.EqualTo (userData.SendProductEmails));
            Assert.That (userJson.SendTimerNotifications, Is.EqualTo (userData.SendTimerNotifications));
            Assert.That (userJson.StartOfWeek, Is.EqualTo (userData.StartOfWeek));
            Assert.That (userJson.SendWeeklyReport, Is.EqualTo (userData.SendWeeklyReport));
            Assert.That (userJson.TimeFormat, Is.EqualTo (userData.TimeFormat));
            Assert.That (userJson.Timezone, Is.EqualTo (userData.Timezone));
            Assert.That (userJson.CreatedWith, Is.EqualTo (Platform.DefaultCreatedWith));
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

            Assert.That (teJson.Description, Is.EqualTo (teData.Description));
            Assert.That (teJson.DurationOnly, Is.EqualTo (teData.DurationOnly));
            Assert.That (teJson.IsBillable, Is.EqualTo (teData.IsBillable));
            Assert.That (teJson.ProjectRemoteId, Is.EqualTo (teData.ProjectRemoteId));
            Assert.That (teJson.StartTime, Is.EqualTo (teData.StartTime.ToUtc ()));
            Assert.That (teJson.StopTime, Is.EqualTo (teData.StopTime.ToUtc ()));
            Assert.That (teJson.Duration, Is.EqualTo (Convert.ToInt64 (duration.TotalSeconds)));
            Assert.That (teJson.Duration > 0, Is.True);
            Assert.That (teJson.Tags, Is.EqualTo (tags));
            Assert.That (teJson.TaskRemoteId, Is.EqualTo (teData.TaskRemoteId));
            Assert.That (teJson.UserRemoteId, Is.EqualTo (teData.UserRemoteId));
            Assert.That (teJson.WorkspaceRemoteId, Is.EqualTo (teData.WorkspaceRemoteId));
            Assert.That (teJson.CreatedWith, Is.EqualTo (Platform.DefaultCreatedWith));

            teData.State = TimeEntryState.Running;
            teJson = mapper.Map<TimeEntryJson> (teData);

            //TODO: check startTime.
            Assert.That (teJson.StopTime, Is.EqualTo (stopTime.ToUtc ()));
            Assert.That (teJson.Duration < 0, Is.True);
            Assert.That (teJson.StartTime, Is.EqualTo (startTime.ToUtc ()));
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
