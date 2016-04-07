using System;
using NUnit.Framework;
using SQLite.Net.Interop;
using SQLite.Net.Platform.Generic;
using Toggl.Phoebe.Data;
using Toggl.Phoebe.Data.Json;
using Toggl.Phoebe.Data.Models;
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
            mapper = new JsonMapper();
        }

        [Test]
        public void TestCommonJsonMap()
        {
            var commonData = ClientData.Create(x =>
            {
                x.DeletedAt = DateTime.Now;
                x.Name = "Client";
                x.WorkspaceId = Guid.NewGuid();
                x.WorkspaceRemoteId = 321;
                x.RemoteId = 123;
            });

            var commonJson = mapper.Map<CommonJson> (commonData);

            Assert.That(commonJson.RemoteId, Is.EqualTo(commonData.RemoteId));
            Assert.That(commonJson.ModifiedAt, Is.EqualTo(commonData.ModifiedAt.ToUtc()));
        }

        [Test]
        public void TestMappingInheritance()
        {
            // Check that rules defined for CommonData -> CommonJson
            // works for a descendent object like ClientData.
            var clientData = new ClientData
            {
                Id = Guid.NewGuid(),
                ModifiedAt = DateTime.Now,
                DeletedAt = null,
                Name = "Client",
                WorkspaceId = Guid.NewGuid(),
                WorkspaceRemoteId = 321,
                RemoteId = 123
            };

            var clientJson = mapper.Map<ClientJson> (clientData);

            Assert.That(clientJson.RemoteId, Is.EqualTo(clientJson.RemoteId));
            Assert.That(clientJson.ModifiedAt, Is.EqualTo(clientJson.ModifiedAt.ToUtc()));
            Assert.That(clientJson.DeletedAt, Is.EqualTo(clientJson.DeletedAt.ToUtc()));

            clientData.DeletedAt = DateTime.Now;
            clientJson = mapper.Map<ClientJson> (clientData);
            Assert.That(clientJson.DeletedAt, Is.EqualTo(clientJson.DeletedAt.ToUtc()));
        }

        [Test]
        public void TestClientJsonMap()
        {
            var clientData = new ClientData
            {
                Name = "Client",
                WorkspaceId = Guid.NewGuid(),
                WorkspaceRemoteId = 321,
                RemoteId = 123
            };

            var clientJson = mapper.Map<ClientJson> (clientData);

            Assert.That(clientJson.WorkspaceRemoteId, Is.EqualTo(clientData.WorkspaceRemoteId));
            Assert.That(clientJson.Name, Is.EqualTo(clientData.Name));
        }

        [Test]
        public void TestProjectJsonMap()
        {
            var projectData = ProjectData.Create(x =>
            {
                x.Name = "Project";
                x.ClientId = Guid.NewGuid();
                x.ClientRemoteId = 333;
                x.WorkspaceId = Guid.NewGuid();
                x.WorkspaceRemoteId = 321;
                x.RemoteId = 123;
                x.Color = 3;
                x.IsActive = true;
                x.IsBillable = true;
                x.IsPrivate = true;
                x.IsTemplate = true;
                x.UseTasksEstimate = true;
            });

            var projectJson = mapper.Map<ProjectJson> (projectData);

            Assert.That(projectJson.Name, Is.EqualTo(projectData.Name));
            Assert.That(projectJson.IsActive, Is.EqualTo(projectData.IsActive));
            Assert.That(projectJson.IsPrivate, Is.EqualTo(projectData.IsPrivate));
            Assert.That(projectJson.IsBillable, Is.EqualTo(projectData.IsBillable));
            Assert.That(projectJson.IsTemplate, Is.EqualTo(projectData.IsTemplate));
            Assert.That(projectJson.UseTasksEstimate, Is.EqualTo(projectData.UseTasksEstimate));
            Assert.That(projectJson.Color, Is.EqualTo(projectData.Color.ToString()));
            Assert.That(projectJson.ClientRemoteId, Is.EqualTo(projectData.ClientRemoteId));
            Assert.That(projectJson.WorkspaceRemoteId, Is.EqualTo(projectData.WorkspaceRemoteId));
        }


        [Test]
        public void TestProjectUserJsonMap()
        {
            var projectUserData = ProjectUserData.Create(x =>
            {
                x.RemoteId = 123;
                x.HourlyRate = 1;
                x.ProjectId = Guid.NewGuid();
                x.ProjectRemoteId = 234;
                x.IsManager = true;
                x.UserId = Guid.NewGuid();
                x.UserRemoteId = 456;
            });

            var projectUserJson = mapper.Map<ProjectUserJson> (projectUserData);

            Assert.That(projectUserJson.RemoteId, Is.EqualTo(projectUserData.RemoteId));
            Assert.That(projectUserJson.HourlyRate, Is.EqualTo(projectUserData.HourlyRate));
            Assert.That(projectUserJson.ProjectRemoteId, Is.EqualTo(projectUserData.ProjectRemoteId));
            Assert.That(projectUserJson.IsManager, Is.EqualTo(projectUserData.IsManager));
            Assert.That(projectUserJson.UserRemoteId, Is.EqualTo(projectUserData.UserRemoteId));
        }

        [Test]
        public void TestTagJsonMap()
        {
            var tagData = TagData.Create(x =>
            {
                x.RemoteId = 123;
                x.Name = "Tag";
                x.WorkspaceId = Guid.Empty;
                x.WorkspaceRemoteId = 321;
            });

            var tagJson = mapper.Map<TagJson> (tagData);

            Assert.That(tagJson.Name, Is.EqualTo(tagData.Name));
            Assert.That(tagJson.WorkspaceRemoteId, Is.EqualTo(tagData.WorkspaceRemoteId));
        }

        [Test]
        public void TestTaskJsonMap()
        {
            var taskData = TaskData.Create(x =>
            {
                x.RemoteId = 123;
                x.Name = "Task";
                x.WorkspaceRemoteId = 321;
                x.ProjectRemoteId = 111;
                x.Estimate = 12;
                x.IsActive = true;
            });

            var taskJson = mapper.Map<TaskJson> (taskData);

            Assert.That(taskJson.Name, Is.EqualTo(taskData.Name));
            Assert.That(taskJson.WorkspaceRemoteId, Is.EqualTo(taskData.WorkspaceRemoteId));
            Assert.That(taskJson.ProjectRemoteId, Is.EqualTo(taskData.ProjectRemoteId));
            Assert.That(taskJson.IsActive, Is.EqualTo(taskData.IsActive));
            Assert.That(taskJson.Estimate, Is.EqualTo(taskData.Estimate));
        }

        [Test]
        public void TestWorkspaceUserJsonMap()
        {
            var workspaceUserData = WorkspaceUserData.Create(x =>
            {
                x.WorkspaceId = Guid.Empty;
                x.UserId = Guid.Empty;
                x.RemoteId = 123;
                x.WorkspaceRemoteId = 321;
                x.IsActive = true;
                x.Email = "support@toggl.com";
                x.IsAdmin = true;
                x.UserRemoteId = 123;
            });

            var workspaceUserJson = mapper.Map<WorkspaceUserJson> (workspaceUserData);

            Assert.That(workspaceUserJson.IsActive, Is.EqualTo(workspaceUserData.IsActive));
            Assert.That(workspaceUserJson.IsAdmin, Is.EqualTo(workspaceUserData.IsAdmin));
            Assert.That(workspaceUserJson.UserRemoteId, Is.EqualTo(workspaceUserData.UserRemoteId));
            Assert.That(workspaceUserJson.WorkspaceRemoteId, Is.EqualTo(workspaceUserData.WorkspaceRemoteId));
            Assert.That(workspaceUserJson.Email, Is.EqualTo(workspaceUserData.Email));
            Assert.That(workspaceUserJson.UserRemoteId, Is.EqualTo(workspaceUserData.UserRemoteId));
        }

        [Test]
        public void TestWorkspaceJsonMap()
        {
            var workspaceData = WorkspaceData.Create(x =>
            {
                x.RemoteId = 123;
                x.Name = "WsU";
                x.IsAdmin = true;
                x.DefaultCurrency = "euro";
                x.DefaultRate = 1;
                x.IsPremium = true;
                x.LogoUrl = "http://logout.com";
                x.OnlyAdminsMayCreateProjects = true;
                x.OnlyAdminsSeeBillableRates = true;
                x.RoundingMode = RoundingMode.Up;
                x.RoundingPercision = 1;
                x.BillableRatesVisibility = AccessLevel.Admin;
                x.ProjectCreationPrivileges = AccessLevel.Regular;
            });

            var wData = mapper.Map<WorkspaceData> (workspaceData);

            Assert.That(wData.IsPremium, Is.EqualTo(workspaceData.IsPremium));
            Assert.That(wData.DefaultRate, Is.EqualTo(workspaceData.DefaultRate));
            Assert.That(wData.DefaultCurrency, Is.EqualTo(workspaceData.DefaultCurrency));
            Assert.That(wData.IsAdmin, Is.EqualTo(workspaceData.IsAdmin));
            Assert.That(wData.Name, Is.EqualTo(workspaceData.Name));
            Assert.That(wData.LogoUrl, Is.EqualTo(workspaceData.LogoUrl));
            Assert.That(wData.OnlyAdminsMayCreateProjects, Is.EqualTo(workspaceData.OnlyAdminsMayCreateProjects));
            Assert.That(wData.OnlyAdminsSeeBillableRates, Is.EqualTo(workspaceData.OnlyAdminsSeeBillableRates));
            Assert.That(wData.RoundingMode, Is.EqualTo(workspaceData.RoundingMode));
            Assert.That(wData.RoundingPercision, Is.EqualTo(workspaceData.RoundingPercision));
        }

        [Test]
        public void TestUserJsonMap()
        {
            var userData = UserData.Create(x =>
            {
                x.RemoteId = 123;
                x.Name = "User";
                x.ApiToken = "123";
                x.Email = "support@toggl.com";
                x.DateFormat = "MM-DD-YY";
                x.DurationFormat = DurationFormat.Classic;
                x.GoogleAccessToken = "GoogleToken";
                x.DefaultWorkspaceRemoteId = 111;
                x.ImageUrl = "image.jpg";
                x.Locale = "locale";
                x.ExperimentIncluded = true;
                x.ExperimentNumber = 1;
                x.SendProductEmails = true;
                x.SendTimerNotifications = true;
                x.SendWeeklyReport = true;
                x.StartOfWeek = DayOfWeek.Monday;
                x.TimeFormat = "MM:SS";
                x.Timezone = "TimeZone";
            });

            var userJson = mapper.Map<UserJson> (userData);

            Assert.That(userJson.ApiToken, Is.EqualTo(userData.ApiToken));
            Assert.That(userJson.DateFormat, Is.EqualTo(userData.DateFormat));
            Assert.That(userJson.DefaultWorkspaceRemoteId, Is.EqualTo(userData.DefaultWorkspaceRemoteId));
            Assert.That(userJson.DurationFormat, Is.EqualTo(userData.DurationFormat));
            Assert.That(userJson.Email, Is.EqualTo(userData.Email));
            Assert.That(userJson.OBM.Included, Is.EqualTo(userData.ExperimentIncluded));
            Assert.That(userJson.OBM.Number, Is.EqualTo(userData.ExperimentNumber));
            Assert.That(userJson.GoogleAccessToken, Is.EqualTo(userData.GoogleAccessToken));
            Assert.That(userJson.ImageUrl, Is.EqualTo(userData.ImageUrl));
            Assert.That(userJson.Locale, Is.EqualTo(userData.Locale));
            Assert.That(userJson.Name, Is.EqualTo(userData.Name));
            Assert.That(userJson.SendProductEmails, Is.EqualTo(userData.SendProductEmails));
            Assert.That(userJson.SendTimerNotifications, Is.EqualTo(userData.SendTimerNotifications));
            Assert.That(userJson.StartOfWeek, Is.EqualTo(userData.StartOfWeek));
            Assert.That(userJson.SendWeeklyReport, Is.EqualTo(userData.SendWeeklyReport));
            Assert.That(userJson.TimeFormat, Is.EqualTo(userData.TimeFormat));
            Assert.That(userJson.Timezone, Is.EqualTo(userData.Timezone));
            Assert.That(userJson.CreatedWith, Is.EqualTo(Platform.DefaultCreatedWith));
        }

        [Test]
        public void TestTimeEntryJsonMap()
        {
            var tags = new System.Collections.Generic.List<Guid> ();
            var duration = TimeSpan.FromMinutes(3);
            var startTime = new DateTime(DateTime.Now.Ticks);
            var stopTime = startTime + duration;

            var teData = TimeEntryData.Create(x =>
            {
                x.RemoteId = 123;
                x.Description = "description";
                x.IsBillable = true;
                x.ProjectRemoteId = 123;
                x.DurationOnly = true;
                x.StartTime = startTime;
                x.StopTime = stopTime;
                x.TagIds = tags;
                x.TaskRemoteId = null;
                x.UserRemoteId = 333;
                x.WorkspaceRemoteId = 222;
                x.State = TimeEntryState.Finished;
            });

            var teJson = mapper.Map<TimeEntryJson> (teData);

            Assert.That(teJson.Description, Is.EqualTo(teData.Description));
            Assert.That(teJson.DurationOnly, Is.EqualTo(teData.DurationOnly));
            Assert.That(teJson.IsBillable, Is.EqualTo(teData.IsBillable));
            Assert.That(teJson.ProjectRemoteId, Is.EqualTo(teData.ProjectRemoteId));
            Assert.That(teJson.StartTime, Is.EqualTo(teData.StartTime.ToUtc()));
            Assert.That(teJson.StopTime, Is.EqualTo(teData.StopTime.ToUtc()));
            Assert.That(teJson.Duration, Is.EqualTo(Convert.ToInt64(duration.TotalSeconds)));
            Assert.That(teJson.Duration > 0, Is.True);
            Assert.That(teJson.Tags, Is.EqualTo(tags));
            Assert.That(teJson.TaskRemoteId, Is.EqualTo(teData.TaskRemoteId));
            Assert.That(teJson.UserRemoteId, Is.EqualTo(teData.UserRemoteId));
            Assert.That(teJson.WorkspaceRemoteId, Is.EqualTo(teData.WorkspaceRemoteId));
            Assert.That(teJson.CreatedWith, Is.EqualTo(Platform.DefaultCreatedWith));

            teData = teData.With(x => x.State = TimeEntryState.Running);
            teJson = mapper.Map<TimeEntryJson> (teData);

            //TODO: check startTime.
            Assert.That(teJson.StopTime, Is.EqualTo(stopTime.ToUtc()));
            Assert.That(teJson.Duration < 0, Is.True);
            Assert.That(teJson.StartTime, Is.EqualTo(startTime.ToUtc()));
        }


        private class PlatformUtils : IPlatformUtils
        {
            public string AppIdentifier { get; set; }

            public string AppVersion { get; set; }

            public bool IsWidgetAvailable { get; set; }

            public ISQLitePlatform SQLiteInfo
            {
                get
                {
                    return new SQLitePlatformGeneric();
                }
            }

            public void DispatchOnUIThread(Action action)
            {
                action.Invoke();
            }
        }


    }
}
